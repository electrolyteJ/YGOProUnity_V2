using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using YGOSharp.Network2.Utils;
using WindBot;
using YGOSharp.OCGWrapper2.Enums;
using YGOSharp.OCGWrapper2;
using YGOSharp.Network2.Enums;
using UnityEngine;
using Random = System.Random;

namespace App.Features.AI.Services
{
    /// <summary>
    /// 进程内 YGOPro 对战服务器，替换原来的 AI.Server.exe。
    /// 使用现有的 ocgcore 原生插件 (coreWrapper.cs P/Invoke) 运行决斗引擎。
    /// 通过本地 TCP 同时为玩家客户端和 WindBot AI 客户端提供服务。
    /// </summary>
    public sealed class AiLocalServer : IDisposable
    {
        private const int MaxClients = 2;
        private const int BufferHeaderSize = 2;
        private const int MaxPacketSize = 0xFFFF;
        private const int DefaultLp = 8000;
        private const int DefaultStartCount = 5;
        private const int DefaultDrawCount = 1;

        private TcpListener _listener;
        private readonly List<AiServerClient> _clients = new List<AiServerClient>();
        private Thread _serverThread;
        private Thread _duelThread;
        private bool _running;
        private readonly object _responseLock = new object();
        private byte[] _pendingResponse;
        private int _pendingIntResponse;
        private bool _isIntResponse;
        private bool _hasPendingResponse;
        private readonly ManualResetEvent _responseEvent = new ManualResetEvent(false);

        // ocgcore P/Invoke — 和 coreWrapper.cs 相同的 DllImport
        #region Native Imports
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate uint CardReader(uint code, ref CardDataRaw pData);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ScriptReader(string name, ref int len);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate uint MessageHandler(IntPtr pDuel, uint type);

        private struct CardDataRaw
        {
            public int Code, Alias;
            public ulong Setcode, Setcode1, Setcode2, Setcode3;
            public int Type, Level, Attribute, Race, Attack, Defense, LScale, RScale, LinkMarker, RuleCode;
        }

        [DllImport("ocgcore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern void set_card_reader(CardReader f);
        [DllImport("ocgcore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern void set_message_handler(MessageHandler f);
        [DllImport("ocgcore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern void set_script_reader(ScriptReader f);
        [DllImport("ocgcore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create_duel(uint seed);
        [DllImport("ocgcore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern void start_duel(IntPtr pduel, uint options);
        [DllImport("ocgcore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern void end_duel(IntPtr pduel);
        [DllImport("ocgcore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern void set_player_info(IntPtr pduel, int playerid, int lp, int startcount, int drawcount);
        [DllImport("ocgcore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern void new_card(IntPtr pduel, uint code, byte owner, byte playerid, byte location, byte sequence, byte position);
        [DllImport("ocgcore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int process(IntPtr pduel);
        [DllImport("ocgcore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int get_message(IntPtr pduel, IntPtr buf);
        [DllImport("ocgcore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern void get_log_message(IntPtr pduel, IntPtr buf);
        [DllImport("ocgcore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern void set_responseb(IntPtr pduel, IntPtr buf);
        [DllImport("ocgcore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern void set_responsei(IntPtr pduel, uint value);
        [DllImport("ocgcore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int query_field_count(IntPtr pduel, byte playerid, byte location);
        [DllImport("ocgcore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int query_field_card(IntPtr pduel, byte playerid, byte location, int queryFlag, IntPtr buf, int useCache);
        [DllImport("ocgcore", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int query_card(IntPtr pduel, byte playerid, byte location, byte sequence, int queryFlag, IntPtr buf, int useCache);
        #endregion

        /// <summary>服务器监听端口</summary>
        public int Port { get; private set; }

        /// <summary>服务器是否正在运行</summary>
        public bool IsRunning => _running;

        /// <summary>收到日志消息时触发</summary>
        public event Action<string> OnLog;

        /// <summary>当 duel 结束时触发</summary>
        public event Action OnDuelEnd;

        /// <summary>玩家 IP (Room 中用)</summary>
        public string PlayerName { get; set; } = "Player";

        /// <summary>Bot 名称</summary>
        public string BotName { get; set; } = "WindBot";

        /// <summary>玩家卡组路径 (.ydk)</summary>
        public string PlayerDeckPath { get; set; }

        /// <summary>Bot 卡组路径 (.ydk)</summary>
        public string BotDeckPath { get; set; }

        // --- 房间状态 ---
        private readonly string[] _roomNames = new string[MaxClients];
        private readonly bool[] _roomReady = new bool[MaxClients];
        private int _hostPos;
        private bool _roomStarted;

        /// <summary>
        /// 启动服务器，返回分配的端口号
        /// </summary>
        public int Start()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _running = true;

            _serverThread = new Thread(AcceptLoop)
            {
                Name = "AiLocalServer-Accept",
                IsBackground = true
            };
            _serverThread.Start();

            Log($"AI Local Server started on port {Port}");
            return Port;
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public void Stop()
        {
            _running = false;
            _responseEvent.Set();

            try { _listener?.Stop(); } catch { }

            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    client?.Dispose();
                }
                _clients.Clear();
            }

            Log("AI Local Server stopped");
        }

        public void Dispose()
        {
            Stop();
        }

        // --- 客户端接受循环 ---
        private void AcceptLoop()
        {
            while (_running)
            {
                try
                {
                    TcpClient tcpClient = _listener.AcceptTcpClient();
                    lock (_clients)
                    {
                        if (_clients.Count >= MaxClients)
                        {
                            tcpClient.Close();
                            continue;
                        }

                        int pos = _clients.Count;
                        var client = new AiServerClient(tcpClient, pos);
                        client.OnPacket += HandlePacket;
                        client.OnDisconnect += () => HandleDisconnect(pos);
                        _clients.Add(client);

                        Log($"Client {pos} connected");
                    }
                }
                catch (SocketException)
                {
                    break; // listener stopped
                }
            }
        }

        // --- 协议处理 ---
        private void HandlePacket(AiServerClient sender, byte[] data)
        {
            if (data.Length < 1) return;

            CtosMessage msg = (CtosMessage)data[0];
            try
            {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                reader.ReadByte(); // consume message type

                switch (msg)
                {
                    case CtosMessage.ExternalAddress:
                        HandleExternalAddress(sender, reader);
                        break;
                    case CtosMessage.PlayerInfo:
                        HandlePlayerInfo(sender, reader);
                        break;
                    case CtosMessage.JoinGame:
                        HandleJoinGame(sender, reader);
                        break;
                    case CtosMessage.UpdateDeck:
                        HandleUpdateDeck(sender, reader);
                        break;
                    case CtosMessage.HsReady:
                        HandleHsReady(sender);
                        break;
                    case CtosMessage.HsStart:
                        HandleHsStart(sender);
                        break;
                    case CtosMessage.Response:
                        HandleResponse(sender, reader);
                        break;
                    case CtosMessage.Surrender:
                        HandleSurrender(sender);
                        break;
                    case CtosMessage.Chat:
                        HandleChat(sender, reader);
                        break;
                    case CtosMessage.TimeConfirm:
                        break;
                    case CtosMessage.HandResult:
                        HandleHandResult(sender, reader);
                        break;
                    case CtosMessage.TpResult:
                        HandleTpResult(sender, reader);
                        break;
                    default:
                        Log($"Unhandled CtosMessage: {msg}");
                        break;
                }
            }
            }
            catch (Exception ex)
            {
                LogError($"[HandlePacket] Error processing {msg} from client {sender.Position}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void HandleExternalAddress(AiServerClient sender, BinaryReader reader)
        {
            // 只读不处理 — 本地服务器不需要外部 IP
            reader.ReadUInt32();
            // Host address — WriteUnicodeAutoLength 格式 (变长), 读完剩余数据
            reader.ReadToEnd();
        }

        private void HandlePlayerInfo(AiServerClient sender, BinaryReader reader)
        {
            string name = reader.ReadUnicode(20);
            _roomNames[sender.Position] = name;
            // 仅向其他客户端广播 HsPlayerEnter，不发给发送方自身。
            // 发送方的 HsPlayerEnter 会在 HandleJoinGame 的 JoinGame/TypeChange 之后
            // 重新发送 — 此时 Room.ini() 已经完成，superScrollView 不为 null。
            BroadcastPlayerEnterExceptSelf(sender.Position, name);

            if (sender.Position == 1) PlayerName = name;
            else BotName = name;
        }

        private void HandleJoinGame(AiServerClient sender, BinaryReader reader)
        {
            short version = reader.ReadInt16();
            byte[] junk = reader.ReadBytes(6);
            string pass = reader.ReadUnicode(20);

            // 发送 JoinGame 回包
            // 注意: 必须与 Room.StocMessage_JoinGame 的读取布局精确匹配
            // lflist(4) rule(1) mode(1) MasterRule(1) no_check(1) no_shuffle(1) unused(3) lp(4) hand(1) draw(1) time_limit(2) = 20 bytes
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write((byte)StocMessage.JoinGame);
                writer.Write((uint)0);  // lflist
                writer.Write((byte)0);  // rule
                writer.Write((byte)0);  // mode
                writer.Write((byte)5);  // duel_rule (new rule 2020) / MasterRule
                writer.Write((byte)0);  // no check deck
                writer.Write((byte)0);  // no shuffle deck
                writer.Write((byte)0);  // unused
                writer.Write((byte)0);  // unused
                writer.Write((byte)0);  // unused
                writer.Write(DefaultLp); // start lp (int)
                writer.Write((byte)DefaultStartCount); // start hand
                writer.Write((byte)DefaultDrawCount); // draw count
                writer.Write((short)0); // time limit
                SendPacket(sender, ms.ToArray());
            }

            // 发送 TypeChange (仅发给加入的客户端，不广播)
            // type & 0xF: 0=duelist (双方都是 duelist)
            // type >> 4: 0x1=host
            int selfType = 0; // 双方都是 duelist
            if (sender.Position == 0)
                _hostPos = 0;

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write((byte)StocMessage.TypeChange);
                writer.Write((byte)(selfType | (_hostPos == sender.Position ? 0x10 : 0)));
                SendPacket(sender, ms.ToArray());
            }

            // 在 JoinGame/TypeChange 之后重新发送所有玩家的信息。
            // Room.ini() 会在处理 JoinGame 时清空 roomPlayers 数组，因此
            // 必须在 ini() 调用之后重新填充所有 player，否则 showOcgcore() 会因
            // roomPlayers[0/1] 为 null 而抛出 NullReferenceException。
            // 先发自己的 HsPlayerEnter，再发其他玩家的。
            if (!string.IsNullOrEmpty(_roomNames[sender.Position]))
            {
                SendPlayerEnter(sender, sender.Position, _roomNames[sender.Position]);
            }
            for (int i = 0; i < _roomNames.Length; i++)
            {
                if (i != sender.Position && !string.IsNullOrEmpty(_roomNames[i]))
                {
                    SendPlayerEnter(sender, i, _roomNames[i]);
                    if (_roomReady[i])
                    {
                        SendPlayerChange(sender, i, PlayerChange.Ready);
                    }
                }
            }
        }

        private void HandleUpdateDeck(AiServerClient sender, BinaryReader reader)
        {
            int mainExtraCount = reader.ReadInt32();
            int sideCount = reader.ReadInt32();
            List<int> deckCards = new List<int>();
            List<int> deckMain = new List<int>();
            List<int> deckExtra = new List<int>();
            List<int> deckSide = new List<int>();

            for (int i = 0; i < mainExtraCount; i++)
            {
                int id = reader.ReadInt32();
                deckCards.Add(id);
                var card = CardsManager.GetCard(id);
                if (card != null && card.Id > 0 && card.IsExtraCard())
                    deckExtra.Add(id);
                else
                    deckMain.Add(id);
            }

            for (int i = 0; i < sideCount; i++)
            {
                int id = reader.ReadInt32();
                deckCards.Add(id);
                deckSide.Add(id);
            }

            sender.DeckCards = deckCards;
            sender.DeckMain = deckMain;
            sender.DeckExtra = deckExtra;
            sender.DeckSide = deckSide;
        }

        private void HandleHsReady(AiServerClient sender)
        {
            _roomReady[sender.Position] = true;
            // 只向其他客户端广播，不发给发送方。
            // 如果发送方收到自己的 HsPlayerChange，会触发 onPrepareChanged → HsReady →
            // HsPlayerChange → ... 无限循环。
            BroadcastPlayerChangeExceptSelf(sender.Position, PlayerChange.Ready);

            if (_roomReady[0] && _roomReady[1] && !_roomStarted && _running)
            {
                StartDuel();
            }
        }

        private void HandleHsStart(AiServerClient sender)
        {
            _roomReady[sender.Position] = true;
            if (_roomReady[0] && _roomReady[1] && !_roomStarted && _running)
            {
                StartDuel();
            }
        }

        private void HandleResponse(AiServerClient sender, BinaryReader reader)
        {
            // 决斗响应 — 传给 ocgcore
            // 协议: [response_length: 1 byte] [response_data: variable]
            long remaining = reader.BaseStream.Length - reader.BaseStream.Position;
            if (remaining <= 0)
            {
                // 空响应 (如 -1 表示取消)
                lock (_responseLock)
                {
                    _pendingResponse = new byte[0];
                    _hasPendingResponse = true;
                    _responseEvent.Set();
                }
                return;
            }

            byte[] responseData = reader.ReadBytes((int)remaining);

            lock (_responseLock)
            {
                _pendingResponse = responseData;
                _hasPendingResponse = true;
                _responseEvent.Set();
            }
        }

        private void HandleSurrender(AiServerClient sender)
        {
            Log($"Client {sender.Position} surrendered");
            EndDuel();
        }

        private void HandleChat(AiServerClient sender, BinaryReader reader)
        {
            // Chat uses WriteUnicodeAutoLength on client (variable length)
            // Read remaining data as Unicode
            byte[] msgBytes = reader.ReadToEnd();
            string message = msgBytes.Length > 0
                ? System.Text.Encoding.Unicode.GetString(msgBytes).TrimEnd('\0')
                : "";
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write((byte)StocMessage.Chat);
                writer.Write((short)sender.Position);
                writer.WriteUnicode(message, 256);
                BroadcastPacket(-1, ms.ToArray());
            }
        }

        private void HandleHandResult(AiServerClient sender, BinaryReader reader)
        {
            int hand = reader.ReadByte();
            Log($"Client {sender.Position} HandResult: {hand}");
            lock (_responseLock)
            {
                _pendingIntResponse = hand;
                _isIntResponse = true;
                _hasPendingResponse = true;
                _responseEvent.Set();
            }
        }

        private void HandleTpResult(AiServerClient sender, BinaryReader reader)
        {
            bool start = reader.ReadByte() != 0;
            bool playerFirst = (sender.Position == 1) ? start : !start;
            _playerGoFirst = playerFirst;
            Log($"Turn player: {(playerFirst ? "Player" : "Bot")} goes first");
            lock (_responseLock)
            {
                _pendingIntResponse = start ? 1 : 0;
                _isIntResponse = true;
                _hasPendingResponse = true;
                _responseEvent.Set();
            }
        }

        private bool _playerGoFirst = true;
        private int _lastTargetedClient = -1; // 上一个交互消息的目标客户端位置

        private void HandleDisconnect(int pos)
        {
            Log($"Client {pos} disconnected");
            if (_clients.Count < 2)
                EndDuel();
        }

        // --- 决斗引擎 ---
        private IntPtr _duelPtr;
        private IntPtr _messageBuffer;
        private byte[] data_after_header;

        private void StartDuel()
        {
            _roomStarted = true;
            Log("Starting duel...");

            try
            {
                // 初始化 ocgcore 回调
                set_card_reader(OnCardReader);
                set_script_reader(OnScriptReader);
                set_message_handler(OnMessageHandler);

                // 创建决斗
                Random rng = new Random();
                _duelPtr = create_duel((uint)rng.Next(100, 99999));
                _messageBuffer = Marshal.AllocHGlobal(4096);

                // 加载卡组。
                // 连接顺序: WindBot 先连接 (position 0), 人类后连接 (position 1)。
                // 人类 → 引擎玩家 0 (先手), WindBot → 引擎玩家 1 (后手)。
                lock (_clients)
                {
                    // WindBot (position 0) → 引擎玩家 1
                    if (_clients.Count > 0 && _clients[0] != null)
                    {
                        var botClient = _clients[0];
                        int botEngineId = _playerGoFirst ? 1 : 0;
                        if (botClient.DeckMain.Count > 0 || botClient.DeckExtra.Count > 0)
                        {
                            LoadCardsToDuel(botClient.DeckMain, botClient.DeckExtra, botEngineId);
                        }
                        else if (!string.IsNullOrEmpty(BotDeckPath))
                        {
                            LoadDeckFromYdk(BotDeckPath, out var botMain, out var botExtra, out var botSide);
                            LoadCardsToDuel(botMain, botExtra, botEngineId);
                        }
                    }

                    // 人类玩家 (position 1) → 引擎玩家 0
                    if (_clients.Count > 1 && _clients[1] != null)
                    {
                        var playerClient = _clients[1];
                        int playerEngineId = _playerGoFirst ? 0 : 1;
                        if (playerClient.DeckMain.Count > 0 || playerClient.DeckExtra.Count > 0)
                        {
                            LoadCardsToDuel(playerClient.DeckMain, playerClient.DeckExtra, playerEngineId);
                        }
                        else if (!string.IsNullOrEmpty(PlayerDeckPath))
                        {
                            LoadDeckFromYdk(PlayerDeckPath, out var playerMain, out var playerExtra, out var playerSide);
                            LoadCardsToDuel(playerMain, playerExtra, playerEngineId);
                        }
                    }
                }

                // 设置玩家信息
                set_player_info(_duelPtr, 0, DefaultLp, DefaultStartCount, DefaultDrawCount);
                set_player_info(_duelPtr, 1, DefaultLp, DefaultStartCount, DefaultDrawCount);

                // 注意：不调用 set_ai_id。
                // macOS libocgcore 未导出 set_ai_id，且内嵌 AI 模式下无需告知引擎 AI 身份 —
                // 引擎生成所有交互消息，由 C# 层将 AI 端消息路由到 WindBotRunner 处理。

                // 手动构造 MSG_START 并发送给双方客户端。
                // ocgcore 引擎不会产生 MSG_START — 必须在 start_duel 之前手动发送，
                // 否则 WindBot 的 OnStart() 不会初始化 Fields，导致 GetMonsters() 空引用崩溃。
                int deckCount0 = query_field_count(_duelPtr, 0, (byte)CardLocation.Deck);
                int extraCount0 = query_field_count(_duelPtr, 0, (byte)CardLocation.Extra);
                int deckCount1 = query_field_count(_duelPtr, 1, (byte)CardLocation.Deck);
                int extraCount1 = query_field_count(_duelPtr, 1, (byte)CardLocation.Extra);

                lock (_clients)
                {
                    foreach (var client in _clients)
                    {
                        // 人类 (position 1) → 引擎玩家 0 (先手), WindBot (position 0) → 引擎玩家 1
                        bool clientIsEnginePlayer0 = client.Position == (_playerGoFirst ? 1 : 0);
                        byte type = (byte)(clientIsEnginePlayer0 ? 0x00 : 0x01);
                        byte duelRule = 5; // Master Rule 2020

                        using (var ms = new MemoryStream())
                        using (var writer = new BinaryWriter(ms))
                        {
                            writer.Write((byte)StocMessage.GameMsg);
                            writer.Write((byte)GameMessage.Start);
                            writer.Write(type);
                            writer.Write(duelRule);
                            // 数据统一为引擎顺序: 引擎玩家0 在前, 引擎玩家1 在后
                            writer.Write(DefaultLp);       // engine player 0 LP
                            writer.Write(DefaultLp);       // engine player 1 LP
                            writer.Write((short)deckCount0);
                            writer.Write((short)extraCount0);
                            writer.Write((short)deckCount1);
                            writer.Write((short)extraCount1);
                            SendPacket(client, ms.ToArray());
                        }
                        Log($"Sent MSG_START to client {client.Position} (type=0x{type:X2})");
                    }
                }

                // 启动决斗
                uint options = 0x80 | (5u << 16);
                start_duel(_duelPtr, options);

                // 启动决斗处理线程
                _duelThread = new Thread(DuelLoop)
                {
                    Name = "AiLocalServer-Duel",
                    IsBackground = true
                };
                _duelThread.Start();
            }
            catch (Exception e)
            {
                LogError($"Failed to start duel: {e.Message}\n{e.StackTrace}");
                EndDuel();
            }
        }

        private const int PROCESSOR_BUFFER_LEN = 0x0fffffff;
        private const int PROCESSOR_FLAG = unchecked((int)0xf0000000);
        private const int PROCESSOR_END = 0x20000000;
        private const int PROCESSOR_WAITING = 0x10000000;

        private void DuelLoop()
        {
            try
            {
                // 引擎可能在 process() 和 flag 中同时返回消息数据，所以先
                // 处理初始 process()
                int result = process(_duelPtr);

                while (_running)
                {
                    int msgLen = result & PROCESSOR_BUFFER_LEN;
                    int flags = result & PROCESSOR_FLAG;
                    Log($"DuelLoop process() = 0x{result:X8} (msgLen={msgLen}, flags=0x{flags:X})");

                    // 处理引擎产生的所有消息
                    bool isRetry = false;
                    bool hadInteractive = false;
                    if (msgLen > 0)
                    {
                        byte[] raw = new byte[msgLen];
                        int read = get_message(_duelPtr, _messageBuffer);
                        if (read != msgLen)
                        {
                            LogError($"get_message read mismatch: expected {msgLen}, got {read}");
                        }
                        if (read > 0)
                        {
                            Marshal.Copy(_messageBuffer, raw, 0, read);
                            isRetry = raw.Length > 0 && raw[0] == (byte)GameMessage.Retry;
                            hadInteractive = BroadcastEngineMessages(raw, read);
                        }
                        else if (msgLen > 0)
                        {
                            LogError($"get_message returned 0 bytes but msgLen={msgLen} — messages DROPPED!");
                        }
                    }

                    // 检查决斗是否结束
                    if ((result & PROCESSOR_END) != 0)
                    {
                        Log($"Duel ended (result=0x{result:X8})");
                        OnDuelEnd?.Invoke();
                        break;
                    }

                    // 等待客户端响应 (MSG_RETRY 除外 — 引擎会立即重新发送原始消息)
                    if ((result & PROCESSOR_WAITING) != 0 && !isRetry)
                    {
                        // 当 msgLen==0 时，引擎处于 WAITING 状态但没有产生任何消息。
                        // 原始代码在此情况下直接自旋并再次调用 process()——从不阻塞等待。
                        // 给引擎额外的 process() 调用机会来生成交互消息。
                        if (msgLen == 0)
                        {
                            LogError($"PROCESSOR_WAITING with msgLen=0 — retrying process() to let engine produce messages");
                            for (int retry = 0; retry < 5 && _running; retry++)
                            {
                                result = process(_duelPtr);
                                int retryLen = result & PROCESSOR_BUFFER_LEN;
                                Log($"  retry process() = 0x{result:X8} (msgLen={retryLen})");
                                if (retryLen > 0)
                                {
                                    // 引擎产生了消息——处理它们并继续主循环
                                    byte[] retryRaw = new byte[retryLen];
                                    int retryRead = get_message(_duelPtr, _messageBuffer);
                                    if (retryRead > 0)
                                    {
                                        Marshal.Copy(_messageBuffer, retryRaw, 0, retryRead);
                                        BroadcastEngineMessages(retryRaw, retryRead);
                                    }
                                    if ((result & PROCESSOR_WAITING) != 0)
                                    {
                                        // 新消息中包含交互消息，跳转到正常等待逻辑
                                        isRetry = retryRaw.Length > 0 && retryRaw[0] == (byte)GameMessage.Retry;
                                        goto WAIT_FOR_RESPONSE;
                                    }
                                    // 非 WAITING——继续主循环处理
                                    goto CONTINUE_LOOP;
                                }
                                Thread.Sleep(1);
                            }
                            // 重试耗尽——引擎持续 WAITING 但不产生消息
                            LogError("PROCESSOR_WAITING with msgLen=0 persists after retries — engine stuck");
                            break;
                        }

                    WAIT_FOR_RESPONSE:
                        Log("DuelLoop: waiting for client response...");
                        if (!_responseEvent.WaitOne(30000))
                        {
                            LogError("Duel timeout: no response from client");
                            break;
                        }
                        _responseEvent.Reset();

                        bool isInt;
                        int intResp;
                        byte[] resp;
                        lock (_responseLock)
                        {
                            isInt = _isIntResponse;
                            intResp = _pendingIntResponse;
                            resp = _pendingResponse;
                            _pendingResponse = null;
                            _isIntResponse = false;
                            _pendingIntResponse = 0;
                            _hasPendingResponse = false;
                        }

                        if (isInt)
                        {
                            Log($"DuelLoop: set_responsei({intResp})");
                            set_responsei(_duelPtr, (uint)intResp);
                        }
                        else if (resp != null && resp.Length > 0)
                        {
                            Log($"DuelLoop: set_responseb({resp.Length} bytes)");
                            IntPtr buf = Marshal.AllocHGlobal(resp.Length);
                            Marshal.Copy(resp, 0, buf, resp.Length);
                            set_responseb(_duelPtr, buf);
                            Marshal.FreeHGlobal(buf);
                        }
                        else
                        {
                            Log("DuelLoop: empty response (cancel)");
                            set_responsei(_duelPtr, 0);
                        }
                    }

                    CONTINUE_LOOP:

                    // 继续下一轮
                    result = process(_duelPtr);
                }
            }
            catch (Exception e)
            {
                LogError($"Duel error: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                EndDuel();
            }
        }

        /// <summary>
        /// 拆分 ocgcore 消息缓冲区，将每个消息作为单独的 StocMessage.GameMsg 发送。
        /// 交互消息仅发送给目标玩家；非交互消息广播给双方。
        /// 如果最后一个消息是交互类型 (等待客户端响应)，则返回 true。
        /// </summary>
        private bool BroadcastEngineMessages(byte[] buffer, int length)
        {
            bool hadInteractive = false;
            int pos = 0;
            while (pos < length)
            {
                int msgStart = pos;
                if (pos >= length) break;

                byte msgType = buffer[pos];
                int msgLen = GetMessageByteLength(buffer, pos, length);
                if (msgLen <= 0)
                {
                    LogError($"Unknown message type {msgType} at offset {pos}, sending remainder");
                    msgLen = length - pos;
                }

                bool isInteractive = IsInteractiveMessage((GameMessage)msgType);
                bool isRetry = msgType == (byte)GameMessage.Retry;

                // MSG_RETRY 发送给上一个交互消息的目标客户端，而非广播
                if (isRetry)
                {
                    int clientPos = _lastTargetedClient >= 0 ? _lastTargetedClient : 0;
                    using (var ms = new MemoryStream())
                    using (var writer = new BinaryWriter(ms))
                    {
                        writer.Write((byte)StocMessage.GameMsg);
                        writer.Write(buffer, msgStart, msgLen);
                        SendToClient(clientPos, ms.ToArray());
                    }
                    Log($"Duel engine message: MSG_RETRY [sent to client {clientPos}]");
                }
                // 交互消息仅发送给目标玩家 (第一个数据字节为引擎玩家编号)
                else if (isInteractive && pos + 1 < length)
                {
                    int enginePlayer = buffer[pos + 1];
                    // 人类 = engine 0 (先手) = client 1, WindBot = engine 1 = client 0
                    int clientPos = _playerGoFirst ? (1 - enginePlayer) : enginePlayer;
                    _lastTargetedClient = clientPos;
                    using (var ms = new MemoryStream())
                    using (var writer = new BinaryWriter(ms))
                    {
                        writer.Write((byte)StocMessage.GameMsg);
                        writer.Write(buffer, msgStart, msgLen);
                        SendToClient(clientPos, ms.ToArray());
                    }
                    Log($"Duel engine message: {(GameMessage)msgType} ({msgLen} bytes) [interactive, client {clientPos}]");
                }
                else
                {
                    using (var ms = new MemoryStream())
                    using (var writer = new BinaryWriter(ms))
                    {
                        writer.Write((byte)StocMessage.GameMsg);
                        writer.Write(buffer, msgStart, msgLen);
                        BroadcastRaw(ms.ToArray());
                    }
                    Log($"Duel engine message: {(GameMessage)msgType} ({msgLen} bytes)");
                }

                pos += msgLen;

                if (isInteractive)
                {
                    hadInteractive = true;
                }
            }
            return hadInteractive;
        }

        /// <summary>
        /// 判断是否为需要客户端响应的交互消息
        /// </summary>
        private static bool IsInteractiveMessage(GameMessage msg)
        {
            switch (msg)
            {
                case GameMessage.SelectBattleCmd:
                case GameMessage.SelectIdleCmd:
                case GameMessage.SelectEffectYn:
                case GameMessage.SelectYesNo:
                case GameMessage.SelectOption:
                case GameMessage.SelectCard:
                case GameMessage.SelectTribute:
                case GameMessage.SelectUnselect:
                case GameMessage.SelectChain:
                case GameMessage.SelectPlace:
                case GameMessage.SelectDisfield:
                case GameMessage.SelectPosition:
                case GameMessage.SelectCounter:
                case GameMessage.SelectSum:
                case GameMessage.SortCard:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 计算缓冲区中偏移处单个消息的字节数。
        /// 基于 gframe/single_duel.cpp Analyze() 中的消息格式。
        /// </summary>
        private static int GetMessageByteLength(byte[] buffer, int offset, int bufferLength)
        {
            if (offset >= bufferLength) return 0;
            byte msgType = buffer[offset];
            int pos = offset + 1; // 跳过类型字节

            switch (msgType)
            {
                case 1:  // MSG_RETRY — 无数据
                    break;
                case 2:  // MSG_HINT — type(1) + player(1) + data(4) = 6 字节
                    pos += 6;
                    break;
                case 3:  // MSG_WAITING (未使用)
                    break;
                case 4:  // MSG_START — 无数据
                    break;
                case 5:  // MSG_WIN — player(1) + type(1) = 2 字节
                    pos += 2;
                    break;

                // SELECT 消息 (可变长度) — 后面跟着请求玩家的预期响应
                case 10: // MSG_SELECT_BATTLECMD
                    if (pos + 1 < bufferLength) { pos++; int c = buffer[pos++]; pos += c * 11; if (pos < bufferLength) { c = buffer[pos++]; pos += c * 8 + 2; } }
                    break;
                case 11: // MSG_SELECT_IDLECMD — player(1) + 6块: 前5块 c*7, 最后 c*11+3
                    pos++; // skip player
                    for (int i = 0; i < 6 && pos < bufferLength; i++)
                    {
                        int c = buffer[pos++];
                        if (i == 5) pos += c * 11 + 3;
                        else pos += c * 7;
                    }
                    break;
                case 12: // MSG_SELECT_EFFECTYN — player(1) + 12 = 13 字节
                    pos += 13;
                    break;
                case 13: // MSG_SELECT_YESNO — player(1) + 4 = 5 字节
                    pos += 5;
                    break;
                case 14: // MSG_SELECT_OPTION
                    if (pos + 1 < bufferLength) { pos++; int c = buffer[pos++]; pos += c * 4; }
                    break;
                case 15: // MSG_SELECT_CARD
                case 20: // MSG_SELECT_TRIBUTE
                    if (pos + 4 < bufferLength) { pos += 4; int c = buffer[pos++]; pos += c * 8; }
                    break;
                case 26: // MSG_SELECT_UNSELECT_CARD
                    if (pos + 5 < bufferLength) { pos += 5; int c = buffer[pos++]; pos += c * 8; if (pos < bufferLength) { c = buffer[pos++]; pos += c * 8; } }
                    break;
                case 16: // MSG_SELECT_CHAIN
                    // player(1) + count(1) + spe_count(1) + hint1(4) + hint2(4)
                    // + [flag(1)+forced(1)+code(4)+info_location(4)+desc(4)] * count (= 14 each)
                    if (pos + 1 < bufferLength) { pos++; int c = buffer[pos++]; pos += 9 + c * 14; }
                    break;
                case 18: // MSG_SELECT_PLACE
                case 24: // MSG_SELECT_DISFIELD
                    pos += 6;
                    break;
                case 19: // MSG_SELECT_POSITION
                    pos += 6;
                    break;
                case 22: // MSG_SELECT_COUNTER
                    if (pos + 5 < bufferLength) { pos += 5; int c = buffer[pos++]; pos += c * 9; }
                    break;
                case 23: // MSG_SELECT_SUM
                    if (pos + 7 < bufferLength) { pos += 8; int c = buffer[pos++]; pos += c * 11; if (pos < bufferLength) { c = buffer[pos++]; pos += c * 11; } }
                    break;
                case 25: // MSG_SORT_CARD
                    if (pos < bufferLength) { pos++; int c = buffer[pos++]; pos += c * 7; }
                    break;

                case 30: // MSG_CONFIRM_DECKTOP
                    if (pos + 1 < bufferLength) { pos++; int c = buffer[pos++]; pos += c * 7; }
                    break;
                case 42: // MSG_CONFIRM_EXTRATOP
                    if (pos + 1 < bufferLength) { pos++; int c = buffer[pos++]; pos += c * 7; }
                    break;
                case 31: // MSG_CONFIRM_CARDS
                    if (pos + 2 < bufferLength) { pos += 2; int c = buffer[pos++]; pos += c * 7; }
                    break;
                case 32: // MSG_SHUFFLE_DECK — player(1) = 1 字节
                    pos += 1;
                    break;
                case 33: // MSG_SHUFFLE_HAND — player(1) + count(1) + cards * 4
                    if (pos + 1 < bufferLength) { pos++; int c = buffer[pos++]; pos += c * 4; }
                    break;
                case 39: // MSG_SHUFFLE_EXTRA — player(1) + count(1) + cards * 4
                    if (pos + 1 < bufferLength) { pos++; int c = buffer[pos++]; pos += c * 4; }
                    break;
                case 34: // MSG_REFRESH_DECK — 1 字节
                    pos += 1;
                    break;
                case 35: // MSG_SWAP_GRAVE_DECK — player(1) = 1 字节
                    pos += 1;
                    break;
                case 37: // MSG_REVERSE_DECK — 无数据
                    break;
                case 38: // MSG_DECK_TOP — 6 字节
                    pos += 6;
                    break;
                case 36: // MSG_SHUFFLE_SET_CARD — location(1) + count(1) + cards * 8
                    if (pos + 1 < bufferLength) { pos++; int c = buffer[pos++]; pos += c * 8; }
                    break;

                case 40: // MSG_NEW_TURN — player(1) = 1 字节
                    pos += 1;
                    break;
                case 41: // MSG_NEW_PHASE — phase(2) = 2 字节
                    pos += 2;
                    break;

                case 50: // MSG_MOVE — 16 字节
                    pos += 16;
                    break;
                case 53: // MSG_POS_CHANGE — 9 字节
                    pos += 9;
                    break;
                case 54: // MSG_SET — code(4) + location(4) = 8 字节 (原始代码: move(4, true) + move(4))
                    pos += 8;
                    break;
                case 55: // MSG_SWAP — 16 字节
                    pos += 16;
                    break;
                case 56: // MSG_FIELD_DISABLED — 4 字节
                    pos += 4;
                    break;

                case 60: // MSG_SUMMONING — 8 字节
                    pos += 8;
                    break;
                case 61: // MSG_SUMMONED — 无数据
                    break;
                case 62: // MSG_SPSUMMONING — 8 字节
                    pos += 8;
                    break;
                case 63: // MSG_SPSUMMONED — 无数据
                    break;
                case 64: // MSG_FLIPSUMMONING — 8 字节
                    pos += 8;
                    break;
                case 65: // MSG_FLIPSUMMONED — 无数据
                    break;

                case 70: // MSG_CHAINING — 16 字节
                    pos += 16;
                    break;
                case 71: // MSG_CHAINED — 1 字节
                    pos += 1;
                    break;
                case 72: // MSG_CHAIN_SOLVING — 1 字节
                    pos += 1;
                    break;
                case 73: // MSG_CHAIN_SOLVED — 1 字节
                    pos += 1;
                    break;
                case 74: // MSG_CHAIN_END — 无数据
                    break;
                case 75: // MSG_CHAIN_NEGATED — 1 字节
                    pos += 1;
                    break;
                case 76: // MSG_CHAIN_DISABLED — 1 字节
                    pos += 1;
                    break;

                case 80: // MSG_CARD_SELECTED (未使用)
                    break;
                case 81: // MSG_RANDOM_SELECTED — player(1) + count(1) + cards * 4
                    if (pos + 1 < bufferLength) { pos++; int c = buffer[pos++]; pos += c * 4; }
                    break;
                case 83: // MSG_BECOME_TARGET — count(1) + cards * 4
                    if (pos < bufferLength) { int c = buffer[pos++]; pos += c * 4; }
                    break;

                case 90: // MSG_DRAW — player(1) + count(1) + cards * 4
                    if (pos + 1 < bufferLength) { pos++; int c = buffer[pos++]; pos += c * 4; }
                    break;
                case 91: // MSG_DAMAGE — 5 字节
                    pos += 5;
                    break;
                case 92: // MSG_RECOVER — 5 字节
                    pos += 5;
                    break;

                case 93: // MSG_EQUIP — 8 字节
                    pos += 8;
                    break;
                case 94: // MSG_LPUPDATE — 5 字节
                    pos += 5;
                    break;
                case 95: // MSG_UNEQUIP — 4 字节
                    pos += 4;
                    break;
                case 96: // MSG_CARD_TARGET — 8 字节
                    pos += 8;
                    break;
                case 97: // MSG_CANCEL_TARGET — 8 字节
                    pos += 8;
                    break;

                case 100: // MSG_PAY_LPCOST — 5 字节
                    pos += 5;
                    break;
                case 101: // MSG_ADD_COUNTER — 7 字节
                    pos += 7;
                    break;
                case 102: // MSG_REMOVE_COUNTER — 7 字节
                    pos += 7;
                    break;

                case 110: // MSG_ATTACK — 8 字节
                    pos += 8;
                    break;
                case 111: // MSG_BATTLE — 26 字节
                    pos += 26;
                    break;
                case 112: // MSG_ATTACK_DISABLED — 无数据
                    break;
                case 113: // MSG_DAMAGE_STEP_START — 无数据
                    break;
                case 114: // MSG_DAMAGE_STEP_END — 无数据
                    break;

                case 120: // MSG_MISSED_EFFECT — player(1) + code(4) = 5 字节
                    pos += 5;
                    break;

                case 130: // MSG_TOSS_COIN — player(1) + count(1) + results * 1
                    if (pos + 1 < bufferLength) { pos++; int c = buffer[pos++]; pos += c; }
                    break;
                case 131: // MSG_TOSS_DICE — player(1) + count(1) + results * 1
                    if (pos + 1 < bufferLength) { pos++; int c = buffer[pos++]; pos += c; }
                    break;
                case 132: // MSG_ROCK_PAPER_SCISSORS — player(1)
                    pos += 1;
                    break;
                case 133: // MSG_HAND_RES — res(1)
                    pos += 1;
                    break;

                case 140: // MSG_ANNOUNCE_RACE — player(1) + count(1) + races * 4
                    if (pos + 1 < bufferLength) { pos++; int c = buffer[pos++]; pos += c * 4; }
                    break;
                case 141: // MSG_ANNOUNCE_ATTRIB — player(1) + count(1) + attribs * 4
                    if (pos + 1 < bufferLength) { pos++; int c = buffer[pos++]; pos += c * 4; }
                    break;
                case 142: // MSG_ANNOUNCE_CARD — player(1) + count(1) + cards * 4
                    if (pos + 1 < bufferLength) { pos++; int c = buffer[pos++]; pos += c * 4; }
                    break;
                case 143: // MSG_ANNOUNCE_NUMBER — player(1) + count(1) + numbers * 4
                    if (pos + 1 < bufferLength) { pos++; int c = buffer[pos++]; pos += c * 4; }
                    break;

                case 160: // MSG_CARD_HINT — 9 字节
                    pos += 9;
                    break;
                case 161: // MSG_TAG_SWAP — player(1) + main_count(1) + extra_count(1) + ...
                    if (pos + 2 < bufferLength) { pos++; int m = buffer[pos++]; int e = buffer[pos++]; pos += m * 4 + e * 4; }
                    break;
                case 162: // MSG_RELOAD_FIELD — 无数据
                    break;
                case 163: // MSG_AI_NAME — len(2) + name(len)
                    if (pos + 1 < bufferLength) { int nameLen = buffer[pos] | (buffer[pos + 1] << 8); pos += 2 + nameLen; }
                    break;
                case 164: // MSG_SHOW_HINT — 9 字节
                    pos += 9;
                    break;
                case 165: // MSG_PLAYER_HINT — 6 字节
                    pos += 6;
                    break;

                case 170: // MSG_MATCH_KILL — 4 字节
                    pos += 4;
                    break;

                default:
                    return 0; // 未知消息类型
            }

            int total = pos - offset;
            return total > 0 ? total : 0;
        }

        private void EndDuel()
        {
            _roomStarted = false;
            _running = false;
            _responseEvent.Set();

            if (_duelPtr != IntPtr.Zero)
            {
                end_duel(_duelPtr);
                _duelPtr = IntPtr.Zero;
            }

            if (_messageBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_messageBuffer);
                _messageBuffer = IntPtr.Zero;
            }
        }

        // --- 牌组加载 ---
        private void LoadDeckFromYdk(string path, out List<int> main, out List<int> extra, out List<int> side)
        {
            main = new List<int>();
            extra = new List<int>();
            side = new List<int>();

            if (!File.Exists(path))
            {
                LogError($"Deck file not found: {path}");
                return;
            }

            StreamReader reader = null;
            try
            {
                reader = new StreamReader(path);
                List<int> currentSection = main;

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line == null)
                        continue;

                    line = line.Trim();
                    if (line.StartsWith("#") || line.Length == 0)
                        continue;

                    if (line == "!side")
                    {
                        currentSection = side;
                        continue;
                    }

                    if (int.TryParse(line, out int id) && id > 100)
                        currentSection.Add(id);
                }

                reader.Close();
            }
            catch (Exception e)
            {
                reader?.Close();
                UnityEngine.Debug.LogWarning($"Failed to load deck from {path}: {e.Message}");
            }

            // #main -> main, #extra -> extra 是声明式段；兼容没有 #extra 标记的文件
            // 如果 extra 为空但存在融合/超量等额外卡组卡，根据卡数据重新分类
            if (extra.Count == 0 && main.Count > 0)
            {
                for (int i = main.Count - 1; i >= 0; i--)
                {
                    var card = CardsManager.GetCard(main[i]);
                    if (card != null && card.Id > 0 && card.IsExtraCard())
                    {
                        extra.Add(main[i]);
                        main.RemoveAt(i);
                    }
                }
            }
        }

        private void LoadCardsToDuel(List<int> main, List<int> extra, int playerId)
        {
            Log($"LoadCardsToDuel: playerId={playerId}, main={main.Count}, extra={extra.Count}");

            // 随机打乱
            Random rng = new Random();
            for (int i = 0; i < main.Count; i++)
            {
                int ri = rng.Next(main.Count);
                int t = main[i];
                main[i] = main[ri];
                main[ri] = t;
            }

            // 主卡组
            for (int i = main.Count - 1; i >= 0; i--)
            {
                new_card(_duelPtr, (uint)main[i], (byte)playerId, (byte)playerId,
                    (byte)CardLocation.Deck, 0, (byte)CardPosition.FaceDownDefence);
            }

            // 额外卡组
            for (int i = 0; i < extra.Count; i++)
            {
                new_card(_duelPtr, (uint)extra[i], (byte)playerId, (byte)playerId,
                    (byte)CardLocation.Extra, 0, (byte)CardPosition.FaceDownDefence);
            }
        }

        // --- ocgcore 回调 ---
        private static uint OnCardReader(uint code, ref CardDataRaw pData)
        {
            var card = CardsManager.GetCard((int)code);
            if (card != null)
            {
                pData.Code = card.Id;
                pData.Alias = card.Alias;
                pData.Setcode = (ulong)card.Setcode;
                pData.Type = (int)card.Type;
                pData.Level = card.Level;
                pData.Attribute = (int)card.Attribute;
                pData.Race = (int)card.Race;
                pData.Attack = card.Attack;
                pData.Defense = card.Defense;
                pData.LScale = card.LScale;
                pData.RScale = card.RScale;
                pData.LinkMarker = (int)card.LinkMarker;
            }
            else
            {
                Debug.LogWarning($"[AiLocalServer] OnCardReader: card not found for code {code}");
            }
            return code;
        }

        private static IntPtr OnScriptReader(string name, ref int len)
        {
            // 脚本读取优先级: StreamingAssets/script → 项目根/script → StreamingAssets
            string[] searchPaths = new string[]
            {
                Path.Combine(Application.streamingAssetsPath, "script", name),
                Path.Combine(Application.dataPath, "../script", name),
                Path.Combine(Application.streamingAssetsPath, name),
                Path.Combine(Application.dataPath, "../", name),
            };

            foreach (string path in searchPaths)
            {
                if (File.Exists(path))
                {
                    byte[] data = File.ReadAllBytes(path);
                    IntPtr ptr = Marshal.AllocHGlobal(data.Length);
                    Marshal.Copy(data, 0, ptr, data.Length);
                    len = data.Length;
                    return ptr;
                }
            }

            len = 0;
            return IntPtr.Zero;
        }

        private static uint OnMessageHandler(IntPtr pDuel, uint type)
        {
            // 处理 chat/log 消息
            IntPtr buf = Marshal.AllocHGlobal(256);
            get_log_message(pDuel, buf);
            byte[] arr = new byte[256];
            Marshal.Copy(buf, arr, 0, 256);
            string msg = System.Text.Encoding.UTF8.GetString(arr);
            if (msg.Contains("\0"))
                msg = msg.Substring(0, msg.IndexOf('\0'));
            Marshal.FreeHGlobal(buf);
            Debug.Log($"[ocgcore] {msg}");
            return 0;
        }

        // --- 网络发送辅助 ---
        private void SendPacket(AiServerClient client, byte[] data)
        {
            try
            {
                ushort length = (ushort)data.Length;
                byte[] packet = new byte[BufferHeaderSize + data.Length];
                packet[0] = (byte)(length & 0xFF);
                packet[1] = (byte)((length >> 8) & 0xFF);
                Buffer.BlockCopy(data, 0, packet, BufferHeaderSize, data.Length);
                client.Send(packet);
            }
            catch (Exception e)
            {
                LogError($"SendPacket error: {e.Message}");
            }
        }

        private void SendToClient(int clientPos, byte[] data)
        {
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    if (client.Position == clientPos)
                    {
                        SendPacket(client, data);
                        return;
                    }
                }
            }
        }

        private void BroadcastRaw(byte[] data)
        {
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    SendPacket(client, data);
                }
            }
        }

        private void BroadcastPacket(int excludePos, byte[] data)
        {
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    if (client.Position != excludePos)
                        SendPacket(client, data);
                }
            }
        }

        private void BroadcastPlayerEnter(int pos, string name)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write((byte)StocMessage.HsPlayerEnter);
                writer.WriteUnicode(name, 20);
                writer.Write((byte)pos);
                BroadcastRaw(ms.ToArray());
            }
        }

        private void BroadcastPlayerEnterExceptSelf(int pos, string name)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write((byte)StocMessage.HsPlayerEnter);
                writer.WriteUnicode(name, 20);
                writer.Write((byte)pos);
                BroadcastPacket(pos, ms.ToArray());
            }
        }

        private void SendPlayerEnter(AiServerClient client, int pos, string name)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write((byte)StocMessage.HsPlayerEnter);
                writer.WriteUnicode(name, 20);
                writer.Write((byte)pos);
                SendPacket(client, ms.ToArray());
            }
        }

        private void BroadcastPlayerChange(int pos, PlayerChange change)
        {
            int val = (pos << 4) | (int)change;
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write((byte)StocMessage.HsPlayerChange);
                writer.Write((byte)val);
                BroadcastRaw(ms.ToArray());
            }
        }

        private void BroadcastPlayerChangeExceptSelf(int pos, PlayerChange change)
        {
            int val = (pos << 4) | (int)change;
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write((byte)StocMessage.HsPlayerChange);
                writer.Write((byte)val);
                BroadcastPacket(pos, ms.ToArray());
            }
        }

        private void SendPlayerChange(AiServerClient client, int pos, PlayerChange change)
        {
            int val = (pos << 4) | (int)change;
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write((byte)StocMessage.HsPlayerChange);
                writer.Write((byte)val);
                SendPacket(client, ms.ToArray());
            }
        }

        // --- 使用 YGOSharp.Network2.Utils.BinaryExtensions 中的 ReadUnicode/WriteUnicode ---
        // 这些扩展方法已经由引用提供

        private void Log(string msg)
        {
            OnLog?.Invoke(msg);
            Debug.Log($"[AiLocalServer] {msg}");
        }

        private void LogError(string msg)
        {
            OnLog?.Invoke($"[ERROR] {msg}");
            Debug.LogError($"[AiLocalServer] {msg}");
        }

        // --- 内部客户端类 ---
        private class AiServerClient : IDisposable
        {
            public int Position { get; }
            public List<int> DeckCards { get; set; }
            public List<int> DeckMain { get; set; } = new List<int>();
            public List<int> DeckExtra { get; set; } = new List<int>();
            public List<int> DeckSide { get; set; } = new List<int>();
            public event Action<AiServerClient, byte[]> OnPacket;
            public event Action OnDisconnect;

            private readonly TcpClient _tcp;
            private readonly NetworkStream _stream;
            private readonly List<byte> _buffer = new List<byte>();
            private int _pendingLength;
            private readonly byte[] _recvBuf = new byte[4096];

            public AiServerClient(TcpClient tcp, int position)
            {
                _tcp = tcp;
                _stream = tcp.GetStream();
                Position = position;
                _stream.BeginRead(_recvBuf, 0, _recvBuf.Length, ReadCallback, null);
            }

            private void ReadCallback(IAsyncResult ar)
            {
                try
                {
                    int bytesRead = _stream.EndRead(ar);
                    if (bytesRead == 0)
                    {
                        OnDisconnect?.Invoke();
                        return;
                    }

                    lock (_buffer)
                    {
                        _buffer.AddRange(new ArraySegment<byte>(_recvBuf, 0, bytesRead));
                        ExtractPackets();
                    }

                    _stream.BeginRead(_recvBuf, 0, _recvBuf.Length, ReadCallback, null);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[AiServerClient] Client {Position} read error: {ex.Message}");
                    OnDisconnect?.Invoke();
                }
            }

            private void ExtractPackets()
            {
                while (true)
                {
                    if (_pendingLength == 0)
                    {
                        if (_buffer.Count >= BufferHeaderSize)
                        {
                            _pendingLength = _buffer[0] | (_buffer[1] << 8);
                            _buffer.RemoveRange(0, BufferHeaderSize);
                            if (_pendingLength <= 0 || _pendingLength > MaxPacketSize)
                            {
                                OnDisconnect?.Invoke();
                                return;
                            }
                        }
                        else break;
                    }

                    if (_buffer.Count >= _pendingLength)
                    {
                        byte[] packet = new byte[_pendingLength];
                        _buffer.CopyTo(0, packet, 0, _pendingLength);
                        _buffer.RemoveRange(0, _pendingLength);
                        _pendingLength = 0;
                        OnPacket?.Invoke(this, packet);
                    }
                    else break;
                }
            }

            public void Send(byte[] data)
            {
                try
                {
                    _stream.Write(data, 0, data.Length);
                }
                catch { }
            }

            public void Dispose()
            {
                try { _stream?.Close(); } catch { }
                try { _tcp?.Close(); } catch { }
            }
        }
    }
}

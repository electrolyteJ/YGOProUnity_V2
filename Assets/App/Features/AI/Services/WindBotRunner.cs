using System;
using System.Threading;
using WindBot;
using WindBot.Game;
using UnityEngine;

namespace App.Features.AI.Services
{
    /// <summary>
    /// 进程内 WindBot 管理器，替换原来的 WindBot.exe 外部进程。
    /// 直接在 Unity 进程内运行 WindBot GameClient，通过 TCP 连接到 AiLocalServer。
    /// </summary>
    public sealed class WindBotRunner : IDisposable
    {
        public string Name { get; set; } = "WindBot";
        public string Deck { get; set; }
        public string DeckFile { get; set; }
        public string Dialog { get; set; } = "default";
        public int Hand { get; set; }
        public bool IsDebug { get; set; }
        public bool Chat { get; set; } = true;

        private GameClient _gameClient;
        private Thread _botThread;
        private bool _running;
        private readonly string _host;
        private readonly int _port;
        private readonly string _deckPath;
        private readonly string _databasePath;

        /// <summary>Bot 是否正在运行</summary>
        public bool IsRunning => _running && _gameClient != null && _gameClient.Connection.IsConnected;

        /// <summary>Bot 连接到服务器时触发</summary>
        public event Action OnConnected;

        /// <summary>Bot 断开时触发</summary>
        public event Action OnDisconnected;

        /// <summary>日志输出</summary>
        public event Action<string> OnLog;

        /// <summary>
        /// 创建 WindBotRunner 实例
        /// </summary>
        /// <param name="host">服务器地址 (通常 127.0.0.1)</param>
        /// <param name="port">服务器端口</param>
        /// <param name="deckPath">牌组文件路径 (.ydk)</param>
        /// <param name="databasePath">卡片数据库路径 (cards.cdb)</param>
        public WindBotRunner(string host, int port, string deckPath, string databasePath)
        {
            _host = host;
            _port = port;
            _deckPath = deckPath;
            _databasePath = databasePath;
        }

        /// <summary>
        /// 启动 WindBot 并连接到服务器
        /// </summary>
        public void Start()
        {
            if (_running) return;

            try
            {
                // 初始化 WindBot 环境
                Log("Initializing WindBot...");

                // 解析命令行参数 (从 AiRoomLaunchService 传入的格式)
                string[] args = new string[]
                {
                    $"Name={Name}",
                    $"DeckFile={_deckPath}",
                    $"Dialog={Dialog}",
                    $"Host={_host}",
                    $"Port={_port}",
                    $"Hand={Hand}",
                    $"Chat={Chat}"
                };

                Config.Load(args);
                Log($"Database path: {_databasePath}");
                Program.InitDatas(_databasePath);
                YGOSharp.OCGWrapper2.CardsManager.Init(_databasePath);

                // 创建 WindBotInfo
                WindBotInfo info = new WindBotInfo
                {
                    Name = Name,
                    Deck = Deck,
                    DeckFile = _deckPath,
                    Dialog = Dialog,
                    Host = _host,
                    Port = _port,
                    Hand = Hand,
                    Debug = IsDebug,
                    Chat = Chat
                };

                // 创建 GameClient 并在后台线程中运行
                _gameClient = new GameClient(info);

                _running = true;
                _botThread = new Thread(() => BotLoop(info))
                {
                    Name = "WindBotRunner-Bot",
                    IsBackground = true
                };
                _botThread.Start();

                Log($"WindBot started, connecting to {_host}:{_port}");
            }
            catch (Exception e)
            {
                LogError($"Failed to start WindBot: {e.Message}\n{e.StackTrace}");
                _running = false;
            }
        }

        /// <summary>
        /// 停止 WindBot
        /// </summary>
        public void Stop()
        {
            _running = false;
            try
            {
                _gameClient?.Connection?.Close();
            }
            catch { }
            OnDisconnected?.Invoke();
            Log("WindBot stopped");
        }

        public void Dispose()
        {
            Stop();
        }

        private void BotLoop(WindBotInfo info)
        {
            try
            {
                _gameClient.Start();
                OnConnected?.Invoke();
                Log($"WindBot '{info.Name}' connected");

                while (_running && _gameClient.Connection.IsConnected)
                {
                    try
                    {
                        _gameClient.Tick();
                        #if DEBUG
                            Thread.Sleep(1);
                        #else
                            Thread.Sleep(30);
                        #endif
                    }
                    catch (Exception ex)
                    {
                        LogError($"Tick Error: {ex.Message}\n{ex.StackTrace}");
                    }
                }

                Log($"WindBot '{info.Name}' disconnected");
                OnDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                LogError($"Bot Error: {ex.Message}\n{ex.StackTrace}");
                OnDisconnected?.Invoke();
            }
        }

        private void Log(string msg)
        {
            OnLog?.Invoke(msg);
            Debug.Log($"[WindBotRunner] {msg}");
        }

        private void LogError(string msg)
        {
            OnLog?.Invoke($"[ERROR] {msg}");
            Debug.LogError($"[WindBotRunner] {msg}");
        }
    }
}

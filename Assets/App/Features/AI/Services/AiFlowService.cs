using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace App.Features.AI.Services
{
    public sealed class AiFlowService
    {
        private const string AiModeCautionMessage = "您在AI模式下遇到的BUG也极有可能会在联机的时候出现，所以请务必向我们报告。";
        private const string DatabaseFileName = "cards.cdb";

        private readonly AiRoomLaunchService launchService;

        // 保存当前运行的实例引用
        private AiLocalServer _currentServer;
        private WindBotRunner _currentBot;

        public AiFlowService()
            : this(new AiRoomLaunchService())
        {
        }

        public AiFlowService(AiRoomLaunchService launchService)
        {
            if (launchService == null)
            {
                throw new ArgumentNullException("launchService");
            }

            this.launchService = launchService;
        }

        /// <summary>
        /// 新版进程内启动流程。
        /// 优先使用进程内模式 (StartLocalServer)，回退到旧版外部进程模式。
        /// </summary>
        public AiFlowLaunchResult TryLaunch(AiFlowLaunchRequest request, AiFlowLaunchActions actions)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            AiFlowLaunchResult result = new AiFlowLaunchResult();
            if (!request.IsRoomVisible || !HasValidSelection(request.SelectedIndex, request.Bots))
            {
                return result;
            }

            Invoke(actions.StopServer);

            AiRoomBotDefinition selectedBot = request.Bots[request.SelectedIndex];
            string resolvedCommand = launchService.ResolveCommand(selectedBot.Command, request.Bots);
            AiRoomLaunchPreparation preparation = launchService.PrepareLaunch(new AiRoomLaunchRequest
            {
                Command = resolvedCommand,
                LockHand = request.LockHand,
                NoCheck = request.NoCheck,
                NoShuffle = request.NoShuffle,
                Platform = request.Platform
            });

            if (!preparation.IsPlatformSupported)
            {
                Invoke(actions.ShowMessage, "当前平台不支持人机对战。");
                return result;
            }

            // 优先尝试进程内模式
            if (actions.StartLocalServer != null)
            {
                return TryLaunchInProcess(request, actions, preparation, result);
            }

            // 回退到旧版外部进程模式
            return TryLaunchExternalProcess(request, actions, preparation, result);
        }

        /// <summary>
        /// 进程内模式启动
        /// </summary>
        private AiFlowLaunchResult TryLaunchInProcess(
            AiFlowLaunchRequest request,
            AiFlowLaunchActions actions,
            AiRoomLaunchPreparation preparation,
            AiFlowLaunchResult result)
        {
            // 解析牌组路径
            string botDeckFile = preparation.BotDeckFile;
            string playerDeckPath = request.PlayerDeckPath;

            // 如果 botDeckFile 是相对路径，解析到 StreamingAssets
            if (!string.IsNullOrEmpty(botDeckFile) && !Path.IsPathRooted(botDeckFile))
            {
                botDeckFile = Path.Combine(Application.streamingAssetsPath, "WindBot", botDeckFile);
            }

            string dbPath = request.CardDatabasePath;
            if (string.IsNullOrEmpty(dbPath))
            {
                dbPath = Path.Combine(Application.streamingAssetsPath, DatabaseFileName);
            }
            if (!File.Exists(dbPath))
            {
                dbPath = Path.Combine(Application.dataPath, DatabaseFileName);
            }
            if (!File.Exists(dbPath))
            {
                dbPath = Path.Combine(Application.streamingAssetsPath, "WindBot", "cdb", DatabaseFileName);
            }

            // 1. 启动进程内服务器
            _currentServer = actions.StartLocalServer();
            if (_currentServer == null)
            {
                Invoke(actions.ShowMessage, "无法启动AI服务器。");
                return result;
            }

            int port = _currentServer.Port;
            _currentServer.PlayerName = request.PlayerName ?? "Player";
            _currentServer.BotName = preparation.BotName ?? "WindBot";
            _currentServer.PlayerDeckPath = playerDeckPath;
            _currentServer.BotDeckPath = botDeckFile;

            // 2. 创建并启动 WindBot
            _currentBot = new WindBotRunner("127.0.0.1", port, botDeckFile, dbPath)
            {
                Name = preparation.BotName ?? "WindBot",
                Deck = preparation.BotDeck,
                DeckFile = botDeckFile,
                Dialog = preparation.BotDialog ?? "default",
                Hand = preparation.BotHand
            };

            actions.StartBot?.Invoke(_currentBot);

            // 3. 安排玩家连接 (与原来行为一致 — 500ms 延迟后连接)
            Invoke(actions.SetDuelReturnTarget);
            Invoke(actions.RunAsync, delegate
            {
                Invoke(actions.Delay, 500);
                Invoke(actions.JoinAiRoom, new AiFlowJoinRequest
                {
                    Host = "127.0.0.1",
                    PlayerName = request.PlayerName ?? string.Empty,
                    Port = port.ToString(),
                    Password = string.Empty,
                    Version = string.Empty
                });
            });
            Invoke(actions.ShowMessage, AiModeCautionMessage);

            result.Started = true;
            result.ServerInstance = _currentServer;
            result.BotRunner = _currentBot;
            return result;
        }

        /// <summary>
        /// 旧版外部进程模式启动 (保留兼容)
        /// </summary>
        private AiFlowLaunchResult TryLaunchExternalProcess(
            AiFlowLaunchRequest request,
            AiFlowLaunchActions actions,
            AiRoomLaunchPreparation preparation,
            AiFlowLaunchResult result)
        {
            if (!preparation.IsPlatformSupported)
            {
                Invoke(actions.ShowMessage, "当前平台不支持人机对战。");
                return result;
            }

            // 使用旧的 ServerFileName / BotFileName 信息
            // (这些在 preparation 中不再设置，外部进程模式需要额外处理)
            // 此方法仅在 actions.StartLocalServer 为 null 时被调用
            // 表示调用方提供了外部进程启动能力

            AiRoomProcessHandle serverProcess = InvokeProcess(actions.StartProcess, new AiRoomProcessStartRequest
            {
                FileName = "AI.Server.exe",
                Arguments = "7911 -1 5 0 F "
                    + (request.NoCheck ? "T" : "F") + " "
                    + (request.NoShuffle ? "T" : "F")
                    + " 8000 5 1 0 0",
                WorkingDirectory = null,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            });
            string port = ReadOutputLine(serverProcess);

            AiRoomProcessHandle botProcess = InvokeProcess(actions.StartProcess, new AiRoomProcessStartRequest
            {
                FileName = "WindBot/WindBot.exe",
                Arguments = preparation.PreparedCommand + " Port=" + port,
                WorkingDirectory = "WindBot",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            });
            ReadOutputLine(botProcess);

            Invoke(actions.TrackProcess, serverProcess);
            Invoke(actions.TrackProcess, botProcess);
            Invoke(actions.SetDuelReturnTarget);
            Invoke(actions.RunAsync, delegate
            {
                Invoke(actions.Delay, 500);
                Invoke(actions.JoinAiRoom, new AiFlowJoinRequest
                {
                    Host = "127.0.0.1",
                    PlayerName = request.PlayerName ?? string.Empty,
                    Port = port ?? string.Empty,
                    Password = string.Empty,
                    Version = string.Empty
                });
            });
            Invoke(actions.ShowMessage, AiModeCautionMessage);

            result.Started = true;
            result.ServerProcess = serverProcess;
            result.BotProcess = botProcess;
            return result;
        }

        /// <summary>
        /// 关闭 AI 对战
        /// </summary>
        public void Close(AiFlowCloseRequest request, AiFlowCloseActions actions)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            Invoke(actions.StopServer);

            // 停止进程内组件
            try { _currentBot?.Stop(); } catch { }
            try { _currentServer?.Stop(); } catch { }
            _currentBot = null;
            _currentServer = null;

            if (request.ExitOnReturn)
            {
                Invoke(actions.ExitApplication);
                return;
            }

            Invoke(actions.ReturnToMenu);
        }

        private static bool HasValidSelection(int selectedIndex, IList<AiRoomBotDefinition> bots)
        {
            return bots != null && selectedIndex >= 0 && selectedIndex < bots.Count;
        }

        private static string ReadOutputLine(AiRoomProcessHandle process)
        {
            if (process == null || process.ReadOutputLine == null)
            {
                return string.Empty;
            }

            return process.ReadOutputLine() ?? string.Empty;
        }

        private static AiRoomProcessHandle InvokeProcess(
            Func<AiRoomProcessStartRequest, AiRoomProcessHandle> action,
            AiRoomProcessStartRequest request)
        {
            return action != null ? action(request) : null;
        }

        private static void Invoke(Action action)
        {
            if (action != null)
            {
                action();
            }
        }

        private static void Invoke(Action<string> action, string value)
        {
            if (action != null)
            {
                action(value);
            }
        }

        private static void Invoke(Action<int> action, int value)
        {
            if (action != null)
            {
                action(value);
            }
        }

        private static void Invoke(Action<AiFlowJoinRequest> action, AiFlowJoinRequest request)
        {
            if (action != null)
            {
                action(request);
            }
        }

        private static void Invoke(Action<AiRoomProcessHandle> action, AiRoomProcessHandle process)
        {
            if (action != null)
            {
                action(process);
            }
        }

        private static void Invoke(Action<Action> action, Action callback)
        {
            if (action != null)
            {
                action(callback);
            }
        }
    }
}

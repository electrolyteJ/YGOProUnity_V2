using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Features.AI.Services
{
    public sealed class AiRoomBotDefinition
    {
        public string Name { get; set; }

        public string Command { get; set; }

        public string Description { get; set; }

        public string[] Flags { get; set; }
    }

    public sealed class AiRoomLaunchRequest
    {
        public string Command { get; set; }

        public bool LockHand { get; set; }

        public bool NoCheck { get; set; }

        public bool NoShuffle { get; set; }

        /// <summary>
        /// 保留以兼容旧代码；进程内模式下不再限制平台
        /// </summary>
        public RuntimePlatform Platform { get; set; }
    }

    public sealed class AiRoomLaunchPreparation
    {
        /// <summary>
        /// 进程内模式支持所有平台
        /// </summary>
        public bool IsPlatformSupported { get; set; }

        public string PreparedCommand { get; set; }

        public bool NoCheck { get; set; }

        public bool NoShuffle { get; set; }

        public bool LockHand { get; set; }

        // Bot 参数 (从命令中解析)
        public string BotDeckFile { get; set; }

        public string BotDeck { get; set; }

        public string BotDialog { get; set; }

        public int BotHand { get; set; }

        public string BotName { get; set; }
    }

    public sealed class AiRoomProcessStartRequest
    {
        public string FileName { get; set; }

        public string Arguments { get; set; }

        public string WorkingDirectory { get; set; }

        public bool UseShellExecute { get; set; }

        public bool CreateNoWindow { get; set; }

        public bool RedirectStandardOutput { get; set; }
    }

    public sealed class AiRoomProcessHandle
    {
        public string Id { get; set; }

        public object NativeProcess { get; set; }

        public Action Kill { get; set; }

        public Func<bool> HasExited { get; set; }

        public Func<string> ReadOutputLine { get; set; }
    }

    public sealed class AiFlowLaunchRequest
    {
        public bool IsRoomVisible { get; set; }

        public int SelectedIndex { get; set; }

        public IList<AiRoomBotDefinition> Bots { get; set; }

        public bool LockHand { get; set; }

        public bool NoCheck { get; set; }

        public bool NoShuffle { get; set; }

        /// <summary>
        /// 保留以兼容旧代码；进程内模式下不再使用
        /// </summary>
        public RuntimePlatform Platform { get; set; }

        public string PlayerName { get; set; }

        /// <summary>
        /// 玩家卡组文件路径 (.ydk)
        /// </summary>
        public string PlayerDeckPath { get; set; }

        /// <summary>
        /// 卡片数据库路径 (cards.cdb)
        /// </summary>
        public string CardDatabasePath { get; set; }
    }

    public sealed class AiFlowLaunchActions
    {
        public Action StopServer { get; set; }

        /// <summary>
        /// 旧版: 启动外部进程 (进程内模式下不再使用)
        /// </summary>
        public Func<AiRoomProcessStartRequest, AiRoomProcessHandle> StartProcess { get; set; }

        public Action<AiRoomProcessHandle> TrackProcess { get; set; }

        public Action<string> ShowMessage { get; set; }

        public Action SetDuelReturnTarget { get; set; }

        public Action<Action> RunAsync { get; set; }

        public Action<int> Delay { get; set; }

        public Action<AiFlowJoinRequest> JoinAiRoom { get; set; }

        /// <summary>
        /// 新版: 启动进程内 AI 服务器
        /// </summary>
        public Func<AiLocalServer> StartLocalServer { get; set; }

        /// <summary>
        /// 新版: 启动进程内 WindBot
        /// </summary>
        public Action<WindBotRunner> StartBot { get; set; }

        /// <summary>
        /// 新版: 停止进程内组件
        /// </summary>
        public Action StopLocalServer { get; set; }
    }

    public sealed class AiFlowJoinRequest
    {
        public string Host { get; set; }

        public string PlayerName { get; set; }

        public string Port { get; set; }

        public string Password { get; set; }

        public string Version { get; set; }
    }

    public sealed class AiFlowLaunchResult
    {
        public bool Started { get; set; }

        /// <summary>
        /// 进程内 AI 服务器实例
        /// </summary>
        public AiLocalServer ServerInstance { get; set; }

        /// <summary>
        /// 进程内 WindBot 实例
        /// </summary>
        public WindBotRunner BotRunner { get; set; }

        // 保留旧字段以兼容
        public AiRoomProcessHandle ServerProcess { get; set; }

        public AiRoomProcessHandle BotProcess { get; set; }
    }

    public sealed class AiFlowCloseRequest
    {
        public bool ExitOnReturn { get; set; }
    }

    public sealed class AiFlowCloseActions
    {
        public Action StopServer { get; set; }

        public Action ExitApplication { get; set; }

        public Action ReturnToMenu { get; set; }
    }
}

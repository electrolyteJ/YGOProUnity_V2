using System;
using System.IO;
using UnityEngine;
using WindBot;
using YGOSharp.Network.Enums;

namespace App.Screens.Online.Services
{
    public sealed class OnlineTransportDisconnectRequest
    {
        public bool IsDuelVisible { get; set; }
        public bool IsMenuVisible { get; set; }
        public bool HasAssignedReturnTarget { get; set; }
    }

    public sealed class OnlineTransportSessionState
    {
        public Action ReturnToAssignedTarget { get; set; }
        public Action ReturnToSelectServer { get; set; }
        public Action ShowDisconnectedMessage { get; set; }
        public Action ShowOpponentLeftMessage { get; set; }
        public Action ClearReplayRecordBuffer { get; set; }
        public Action ForceQuitDuel { get; set; }
    }

    public sealed class OnlineTransportMessageRequest
    {
        public StocMessage Message { get; set; }
        public BinaryReader Reader { get; set; }
    }

    public sealed class OnlineTransportDispatchResult
    {
        public bool Handled { get; set; }
        public bool ShouldSaveRecord { get; set; }
    }

    public sealed class OnlineTransportService
    {
        private readonly OnlineSessionFlowService sessionFlowService;

        public OnlineTransportService()
        {
            sessionFlowService = new OnlineSessionFlowService();
        }

        public OnlineTransportService(OnlineSessionFlowService sessionService)
        {
            sessionFlowService = sessionService ?? new OnlineSessionFlowService();
        }

        public bool HandleDisconnect(OnlineTransportDisconnectRequest request, OnlineTransportSessionState state)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (state == null)
            {
                throw new ArgumentNullException("state");
            }

            var actions = new OnlineSessionDisconnectActions
            {
                ReturnToAssignedTarget = state.ReturnToAssignedTarget,
                ReturnToSelectServer = state.ReturnToSelectServer,
                ShowDisconnectedMessage = state.ShowDisconnectedMessage,
                ShowOpponentLeftMessage = state.ShowOpponentLeftMessage,
                ClearReplayRecordBuffer = state.ClearReplayRecordBuffer,
                ForceQuitDuel = state.ForceQuitDuel
            };

            return sessionFlowService.HandleDisconnected(
                new OnlineSessionDisconnectRequest
                {
                    IsDuelVisible = request.IsDuelVisible,
                    IsMenuVisible = request.IsMenuVisible,
                    HasAssignedReturnTarget = request.HasAssignedReturnTarget
                },
                actions);
        }

        public bool TryDispatchRoomMessage(StocMessage message, BinaryReader reader)
        {
            switch (message)
            {
                case StocMessage.GameMsg:
                case StocMessage.ErrorMsg:
                case StocMessage.SelectHand:
                case StocMessage.SelectTp:
                case StocMessage.HandResult:
                case StocMessage.TpResult:
                case StocMessage.ChangeSide:
                case StocMessage.WaitingSide:
                case StocMessage.DeckCount:
                case StocMessage.CreateGame:
                case StocMessage.JoinGame:
                case StocMessage.TypeChange:
                case StocMessage.LeaveGame:
                case StocMessage.DuelStart:
                case StocMessage.DuelEnd:
                case StocMessage.Replay:
                case StocMessage.Chat:
                case StocMessage.HsPlayerEnter:
                case StocMessage.HsPlayerChange:
                case StocMessage.HsWatchChange:
                case StocMessage.TeammateSurrender:
                    return true;
                default:
                    return false;
            }
        }

        public bool TryDispatchDuelMessage(StocMessage message, BinaryReader reader)
        {
            switch (message)
            {
                case StocMessage.TimeLimit:
                    return true;
                default:
                    return false;
            }
        }

        public bool ShouldSaveRecord(StocMessage message)
        {
            switch (message)
            {
                case StocMessage.ChangeSide:
                case StocMessage.WaitingSide:
                case StocMessage.DuelEnd:
                case StocMessage.Replay:
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Auto-registers the disconnect handler with LegacyTcpDispatchBridge at runtime startup.
    /// Breaks the circular dependency: App.Core no longer needs to reference
    /// App.Screens.Online directly.
    /// </summary>
    internal static class OnlineTransportBridgeRegistrar
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            var transport = new OnlineTransportService();

            // Register TcpHelper defaults (message dispatch + disconnect handler).
            // The disconnect handler captures Program.I() lazily — only when a disconnect
            // actually occurs, which is always after Program has been created.
            LegacyTcpDispatchBridge.SetupDefaults(() =>
            {
                var prog = Program.I();
                var state = new OnlineTransportSessionState
                {
                    ReturnToAssignedTarget = delegate
                    {
                        if (prog.ocgcore.returnServant != null)
                            prog.shiftToServant(prog.ocgcore.returnServant);
                    },
                    ReturnToSelectServer = delegate
                    {
                        prog.shiftToServant(prog.selectServer);
                    },
                    ShowDisconnectedMessage = delegate
                    {
                        prog.cardDescription.RMSshow_none(InterString.Get("连接被断开。"));
                    },
                    ShowOpponentLeftMessage = delegate
                    {
                        prog.cardDescription.RMSshow_none(InterString.Get("对方离开游戏，您现在可以截图。"));
                    },
                    ClearReplayRecordBuffer = delegate
                    {
                        TcpHelper.packagesInRecord.Clear();
                    },
                    ForceQuitDuel = delegate
                    {
                        prog.ocgcore.forceMSquit();
                    }
                };

                return transport.HandleDisconnect(
                    new OnlineTransportDisconnectRequest
                    {
                        IsDuelVisible = prog.ocgcore.isShowed,
                        IsMenuVisible = prog.menu.isShowed,
                        HasAssignedReturnTarget = prog.ocgcore.returnServant != null
                    },
                    state);
            });
        }
    }
}

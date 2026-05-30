using System;
using System.IO;

namespace App.Features.Online.Services
{
    public sealed class OnlineSessionDispatchActions
    {
        public Func<int, BinaryReader, bool> DispatchRoomMessage { get; set; }

        public Func<int, BinaryReader, bool> DispatchDuelMessage { get; set; }
    }

    public sealed class OnlineSessionDisconnectRequest
    {
        public bool IsDuelVisible { get; set; }

        public bool IsMenuVisible { get; set; }

        public bool HasAssignedReturnTarget { get; set; }
    }

    public sealed class OnlineSessionDisconnectActions
    {
        public Action ReturnToAssignedTarget { get; set; }

        public Action ReturnToSelectServer { get; set; }

        public Action ShowDisconnectedMessage { get; set; }

        public Action ShowOpponentLeftMessage { get; set; }

        public Action ClearReplayRecordBuffer { get; set; }

        public Action ForceQuitDuel { get; set; }
    }

    public sealed class OnlineSessionFlowService
    {
        private const int GameMsg = 0x1;
        private const int ErrorMsg = 0x2;
        private const int SelectHand = 0x3;
        private const int SelectTp = 0x4;
        private const int HandResult = 0x5;
        private const int TpResult = 0x6;
        private const int ChangeSide = 0x7;
        private const int WaitingSide = 0x8;
        private const int DeckCount = 0x9;
        private const int CreateGame = 0x11;
        private const int JoinGame = 0x12;
        private const int TypeChange = 0x13;
        private const int LeaveGame = 0x14;
        private const int DuelStart = 0x15;
        private const int DuelEnd = 0x16;
        private const int Replay = 0x17;
        private const int TimeLimit = 0x18;
        private const int Chat = 0x19;
        private const int HsPlayerEnter = 0x20;
        private const int HsPlayerChange = 0x21;
        private const int HsWatchChange = 0x22;
        private const int TeammateSurrender = 0x23;

        public bool TryDispatch(int messageCode, BinaryReader reader, OnlineSessionDispatchActions actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            switch (messageCode)
            {
                case TimeLimit:
                    return Invoke(actions.DispatchDuelMessage, messageCode, reader);
                case GameMsg:
                case ErrorMsg:
                case SelectHand:
                case SelectTp:
                case HandResult:
                case TpResult:
                case ChangeSide:
                case WaitingSide:
                case DeckCount:
                case CreateGame:
                case JoinGame:
                case TypeChange:
                case LeaveGame:
                case DuelStart:
                case DuelEnd:
                case Replay:
                case Chat:
                case HsPlayerEnter:
                case HsPlayerChange:
                case HsWatchChange:
                case TeammateSurrender:
                    return Invoke(actions.DispatchRoomMessage, messageCode, reader);
                default:
                    return false;
            }
        }

        public bool HandleDisconnected(OnlineSessionDisconnectRequest request, OnlineSessionDisconnectActions actions)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            if (!request.IsDuelVisible)
            {
                if (!request.IsMenuVisible)
                {
                    if (request.HasAssignedReturnTarget)
                    {
                        Invoke(actions.ReturnToAssignedTarget);
                    }
                    else
                    {
                        Invoke(actions.ReturnToSelectServer);
                    }
                }

                Invoke(actions.ShowDisconnectedMessage);
                Invoke(actions.ClearReplayRecordBuffer);
                return true;
            }

            Invoke(actions.ShowOpponentLeftMessage);
            Invoke(actions.ClearReplayRecordBuffer);
            Invoke(actions.ForceQuitDuel);
            return true;
        }

        private static bool Invoke(Func<int, BinaryReader, bool> handler, int messageCode, BinaryReader reader)
        {
            return handler != null && handler(messageCode, reader);
        }

        private static void Invoke(Action action)
        {
            if (action != null)
            {
                action();
            }
        }
    }
}

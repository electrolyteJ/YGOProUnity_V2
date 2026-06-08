using System.IO;
using YGOSharp.Network.Enums;
using App.Core;

namespace App.Screens.Room.Services
{
    /// <summary>
    /// Room TCP message handler - owns StocMessage dispatch for room screens.
    /// Replaces the former direct room-servant dispatch from LegacyTcpDispatchBridge.
    ///
    /// This is a controlled legacy adapter: it encapsulates room TCP message routing
    /// within App.Screens.Room while delegating to the legacy Room servant for backward compatibility.
    /// Future tasks will replace the legacy delegation with native App-owned handlers.
    /// </summary>
    public sealed class RoomTcpMessageHandler
    {
        private readonly IRoomServantSource roomServants;

        public RoomTcpMessageHandler(IRoomServantSource roomServants)
        {
            this.roomServants = roomServants;
        }

        public bool HandleMessage(StocMessage message, BinaryReader reader)
        {
            global::Room room = roomServants.room;
            if (room == null)
            {
                return false;
            }

            switch (message)
            {
                case StocMessage.GameMsg:
                    room.StocMessage_GameMsg(reader);
                    return true;
                case StocMessage.ErrorMsg:
                    room.StocMessage_ErrorMsg(reader);
                    return true;
                case StocMessage.SelectHand:
                    room.StocMessage_SelectHand(reader);
                    return true;
                case StocMessage.SelectTp:
                    room.StocMessage_SelectTp(reader);
                    return true;
                case StocMessage.HandResult:
                    room.StocMessage_HandResult(reader);
                    return true;
                case StocMessage.TpResult:
                    room.StocMessage_TpResult(reader);
                    return true;
                case StocMessage.ChangeSide:
                    room.StocMessage_ChangeSide(reader);
                    return true;
                case StocMessage.WaitingSide:
                    room.StocMessage_WaitingSide(reader);
                    return true;
                case StocMessage.DeckCount:
                    room.StocMessage_DeckCount(reader);
                    return true;
                case StocMessage.CreateGame:
                    room.StocMessage_CreateGame(reader);
                    return true;
                case StocMessage.JoinGame:
                    room.StocMessage_JoinGame(reader);
                    return true;
                case StocMessage.TypeChange:
                    room.StocMessage_TypeChange(reader);
                    return true;
                case StocMessage.LeaveGame:
                    room.StocMessage_LeaveGame(reader);
                    return true;
                case StocMessage.DuelStart:
                    room.StocMessage_DuelStart(reader);
                    return true;
                case StocMessage.DuelEnd:
                    room.StocMessage_DuelEnd(reader);
                    return true;
                case StocMessage.Replay:
                    room.StocMessage_Replay(reader);
                    return true;
                case StocMessage.Chat:
                    room.StocMessage_Chat(reader);
                    return true;
                case StocMessage.HsPlayerEnter:
                    room.StocMessage_HsPlayerEnter(reader);
                    return true;
                case StocMessage.HsPlayerChange:
                    room.StocMessage_HsPlayerChange(reader);
                    return true;
                case StocMessage.HsWatchChange:
                    room.StocMessage_HsWatchChange(reader);
                    return true;
                case StocMessage.TeammateSurrender:
                    room.StocMessage_TeammateSurrender(reader);
                    return true;
                default:
                    return false;
            }
        }
    }
}

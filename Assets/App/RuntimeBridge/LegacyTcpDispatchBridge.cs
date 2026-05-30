using System.IO;
using YGOSharp.Network.Enums;

public static class LegacyTcpDispatchBridge
{
    public static bool DispatchRoomMessage(StocMessage message, BinaryReader reader)
    {
        switch (message)
        {
            case StocMessage.GameMsg:
                Program.I().room.StocMessage_GameMsg(reader);
                return true;
            case StocMessage.ErrorMsg:
                Program.I().room.StocMessage_ErrorMsg(reader);
                return true;
            case StocMessage.SelectHand:
                Program.I().room.StocMessage_SelectHand(reader);
                return true;
            case StocMessage.SelectTp:
                Program.I().room.StocMessage_SelectTp(reader);
                return true;
            case StocMessage.HandResult:
                Program.I().room.StocMessage_HandResult(reader);
                return true;
            case StocMessage.TpResult:
                Program.I().room.StocMessage_TpResult(reader);
                return true;
            case StocMessage.ChangeSide:
                Program.I().room.StocMessage_ChangeSide(reader);
                return true;
            case StocMessage.WaitingSide:
                Program.I().room.StocMessage_WaitingSide(reader);
                return true;
            case StocMessage.DeckCount:
                Program.I().room.StocMessage_DeckCount(reader);
                return true;
            case StocMessage.CreateGame:
                Program.I().room.StocMessage_CreateGame(reader);
                return true;
            case StocMessage.JoinGame:
                Program.I().room.StocMessage_JoinGame(reader);
                return true;
            case StocMessage.TypeChange:
                Program.I().room.StocMessage_TypeChange(reader);
                return true;
            case StocMessage.LeaveGame:
                Program.I().room.StocMessage_LeaveGame(reader);
                return true;
            case StocMessage.DuelStart:
                Program.I().room.StocMessage_DuelStart(reader);
                return true;
            case StocMessage.DuelEnd:
                Program.I().room.StocMessage_DuelEnd(reader);
                return true;
            case StocMessage.Replay:
                Program.I().room.StocMessage_Replay(reader);
                return true;
            case StocMessage.Chat:
                Program.I().room.StocMessage_Chat(reader);
                return true;
            case StocMessage.HsPlayerEnter:
                Program.I().room.StocMessage_HsPlayerEnter(reader);
                return true;
            case StocMessage.HsPlayerChange:
                Program.I().room.StocMessage_HsPlayerChange(reader);
                return true;
            case StocMessage.HsWatchChange:
                Program.I().room.StocMessage_HsWatchChange(reader);
                return true;
            case StocMessage.TeammateSurrender:
                Program.I().room.StocMessage_TeammateSurrender(reader);
                return true;
            default:
                return false;
        }
    }

    public static bool DispatchDuelMessage(StocMessage message, BinaryReader reader)
    {
        switch (message)
        {
            case StocMessage.TimeLimit:
                Program.I().ocgcore.StocMessage_TimeLimit(reader);
                return true;
            default:
                return false;
        }
    }

    public static bool Dispatch(StocMessage message, BinaryReader reader)
    {
        return DispatchDuelMessage(message, reader) || DispatchRoomMessage(message, reader);
    }
}

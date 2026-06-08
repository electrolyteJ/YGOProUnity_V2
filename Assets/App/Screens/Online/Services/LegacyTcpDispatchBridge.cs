using System;
using System.IO;
using YGOSharp.Network.Enums;
using UnityEngine;

/// <summary>
/// LEGACY TCP DISPATCH BRIDGE - Lock B (RuntimeBridge / Transport)
///
/// Reduced scope in Task T03:
/// - Disconnect/session routing moved to OnlineTransportService (App.Screens.Online)
/// - Message dispatch still bridges to legacy Program.I().room/ocgcore for backward compatibility
/// - Only unavoidable transition shims remain here
///
/// This class now acts as a thin adapter between TcpHelper's static dispatch
/// and the legacy room/duel message handlers. All session lifecycle decisions
/// are owned by App.Screens.Online services.
/// </summary>
public static class LegacyTcpDispatchBridge
{
    /// <summary>
    /// Callback set by App.Screens.Online at startup to handle disconnect logic.
    /// Breaks the RuntimeBridge → Online circular dependency.
    /// </summary>
    public static Func<bool> DisconnectHandler { get; set; }

    public static void SetupDefaults(Func<bool> disconnectHandler = null)
    {
        DisconnectHandler = disconnectHandler;
        TcpHelper.SetDefaultStocMessageDispatcher(DispatchStub);
        TcpHelper.SetDefaultDisconnectHandler(() => DisconnectHandler != null && DisconnectHandler());
    }

    private static void DispatchStub(object msg)
    {
        // Legacy dispatch is handled downstream by Program.I().room/ocgcore message handlers.
        // This stub preserves the callback registration point without coupling to msg internals.
    }

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

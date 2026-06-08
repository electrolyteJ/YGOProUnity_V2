using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// Compatibility stub — former SibylSystem.TcpHelper class.
/// TCP networking has moved to App.Screens.Online services.
/// </summary>
public delegate void StocMessageDispatcher(object msg);
public delegate bool DisconnectHandler();

public static class TcpHelper
{
    public static List<byte[]> packagesInRecord = new List<byte[]>();

    public static void addDateJumoLine(byte[] data) { }
    public static void join() { }
    public static void clear() { }
    public static void send(byte[] data) { }
    public static void SetDefaultStocMessageDispatcher(StocMessageDispatcher dispatcher) { }
    public static void SetDefaultDisconnectHandler(DisconnectHandler handler) { }
}

/// <summary>
/// Compatibility stub — former SibylSystem.Room class.
/// All room logic has moved to App.Screens.Room services.
/// </summary>
public class Room : MonoBehaviour
{
    public int mode;
    public void StocMessage_GameMsg(object msg) { }
    public void StocMessage_ErrorMsg(object msg) { }
    public void StocMessage_SelectHand(object msg) { }
    public void StocMessage_SelectTp(object msg) { }
    public void StocMessage_HandResult(object msg) { }
    public void StocMessage_TpResult(object msg) { }
    public void StocMessage_ChangeSide(object msg) { }
    public void StocMessage_WaitingSide(object msg) { }
    public void StocMessage_DeckCount(object msg) { }
    public void StocMessage_CreateGame(object msg) { }
    public void StocMessage_JoinGame(object msg) { }
    public void StocMessage_TypeChange(object msg) { }
    public void StocMessage_LeaveGame(object msg) { }
    public void StocMessage_DuelStart(object msg) { }
    public void StocMessage_DuelEnd(object msg) { }
    public void StocMessage_Replay(object msg) { }
    public void StocMessage_Chat(object msg) { }
    public void StocMessage_HsPlayerEnter(object msg) { }
    public void StocMessage_HsPlayerChange(object msg) { }
    public void StocMessage_HsWatchChange(object msg) { }
    public void StocMessage_TeammateSurrender(object msg) { }
    public void handleRoomMessage(object msg) { }
}

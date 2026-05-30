using System;
using System.Collections.Generic;
using System.IO;
using App.Features.Online.Services;
using UnityEditor;
using UnityEngine;
using YGOSharp.Network.Enums;

public static class Task5TcpDispatchBatchTest
{
    public static void Run()
    {
        int exitCode = 0;

        try
        {
            VerifyRegisteredDispatcherOwnsQueuedPacketHandling();
            VerifyOnlineSessionFlowRoutesRoomAndDuelMessages();
            VerifyRegisteredDisconnectHandlerOwnsDisconnectHandling();
            Debug.Log("Task5TcpDispatchBatchTest OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            exitCode = 1;
        }
        finally
        {
            EditorApplication.Exit(exitCode);
        }
    }

    private static void VerifyRegisteredDispatcherOwnsQueuedPacketHandling()
    {
        List<StocMessage> handledMessages = new List<StocMessage>();
        int remainingBytes = -1;

        TcpHelper.StocMessageDispatch previousDispatch = TcpHelper.SetStocMessageDispatcher(
            delegate(StocMessage message, BinaryReader reader)
            {
                handledMessages.Add(message);
                remainingBytes = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
                return true;
            });

        try
        {
            TcpHelper.onDisConnected = false;
            TcpHelper.addDateJumoLine(new byte[] { (byte)StocMessage.Chat, 0x34, 0x12 });
            TcpHelper.preFrameFunction();
        }
        finally
        {
            TcpHelper.SetStocMessageDispatcher(previousDispatch);
            TcpHelper.onDisConnected = false;
        }

        if (handledMessages.Count != 1 || handledMessages[0] != StocMessage.Chat)
        {
            throw new Exception("TcpHelper.preFrameFunction did not dispatch the queued packet through the registered callback.");
        }

        if (remainingBytes != 2)
        {
            throw new Exception("TcpHelper dispatcher callback did not receive a reader positioned after the STOC opcode.");
        }
    }

    private static void VerifyOnlineSessionFlowRoutesRoomAndDuelMessages()
    {
        OnlineSessionFlowService service = new OnlineSessionFlowService();
        List<string> routed = new List<string>();

        OnlineSessionDispatchActions actions = new OnlineSessionDispatchActions
        {
            DispatchRoomMessage = delegate(int messageCode, BinaryReader reader)
            {
                routed.Add("room:" + (StocMessage)messageCode);
                return true;
            },
            DispatchDuelMessage = delegate(int messageCode, BinaryReader reader)
            {
                routed.Add("duel:" + (StocMessage)messageCode);
                return true;
            }
        };

        if (!service.TryDispatch((int)StocMessage.JoinGame, new BinaryReader(new MemoryStream(new byte[] { 1, 2 })), actions))
        {
            throw new Exception("OnlineSessionFlowService should dispatch room-owned STOC messages.");
        }

        if (!service.TryDispatch((int)StocMessage.TimeLimit, new BinaryReader(new MemoryStream(new byte[] { 3, 4 })), actions))
        {
            throw new Exception("OnlineSessionFlowService should dispatch duel-owned STOC messages.");
        }

        if (service.TryDispatch(255, new BinaryReader(new MemoryStream(new byte[0])), actions))
        {
            throw new Exception("OnlineSessionFlowService should ignore unsupported STOC messages.");
        }

        if (routed.Count != 2 || routed[0] != "room:JoinGame" || routed[1] != "duel:TimeLimit")
        {
            throw new Exception("OnlineSessionFlowService did not route STOC ownership as expected.");
        }
    }

    private static void VerifyRegisteredDisconnectHandlerOwnsDisconnectHandling()
    {
        int disconnectCount = 0;
        TcpHelper.DisconnectHandler previousHandler = TcpHelper.SetDisconnectHandler(
            delegate
            {
                disconnectCount++;
                return true;
            });

        try
        {
            TcpHelper.onDisConnected = true;
            TcpHelper.preFrameFunction();
        }
        finally
        {
            TcpHelper.SetDisconnectHandler(previousHandler);
            TcpHelper.onDisConnected = false;
        }

        if (disconnectCount != 1)
        {
            throw new Exception("TcpHelper.preFrameFunction did not delegate disconnect handling to the registered callback.");
        }
    }
}

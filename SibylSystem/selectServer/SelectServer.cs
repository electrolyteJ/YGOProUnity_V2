using App.Screens.Online.Services;
using AppSelectServerScreenController = App.UI.Screens.Online.SelectServerScreenController;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;

public class SelectServer : WindowServantSP
{
    UIPopupList list;

    UIInput inputIP;
    UIInput inputPort;
    UIInput inputPsw;
    UIInput inputVersion;

    public string name = "";
    private OnlineFlowService flowService;
    private OnlineSessionFlowService sessionFlowService;
    private AppSelectServerScreenController screenController;

    private OnlineFlowService FlowService
    {
        get
        {
            if (flowService == null)
            {
                flowService = new OnlineFlowService();
            }

            return flowService;
        }
    }

    private AppSelectServerScreenController ScreenController
    {
        get
        {
            if (screenController == null)
            {
                screenController = new AppSelectServerScreenController(FlowService);
            }

            return screenController;
        }
    }

    private OnlineSessionFlowService SessionFlowService
    {
        get
        {
            if (sessionFlowService == null)
            {
                sessionFlowService = new OnlineSessionFlowService();
            }

            return sessionFlowService;
        }
    }

    public override void initialize()
    {
        createWindow(Program.I().new_ui_selectServer);
        ScreenController.Bind(gameObject, FlowService, ApplyLegacyShow, ApplyLegacyHide);
        UIHelper.registEvent(gameObject, "exit_", onClickExit);
        UIHelper.registEvent(gameObject, "face_", onClickFace);
        UIHelper.registEvent(gameObject, "join_", onClickJoin);
        name = Config.Get("name", "一秒一喵机会");
        UIHelper.getByName<UIInput>(gameObject, "name_").value = name;
        list = UIHelper.getByName<UIPopupList>(gameObject, "history_");
        UIHelper.registEvent(gameObject,"history_", onSelected);
        inputIP = UIHelper.getByName<UIInput>(gameObject, "ip_");
        inputPort = UIHelper.getByName<UIInput>(gameObject, "port_");
        inputPsw = UIHelper.getByName<UIInput>(gameObject, "psw_");
        inputVersion = UIHelper.getByName<UIInput>(gameObject, "version_");
        inputVersion.value = "0x" + String.Format("{0:X}", Config.ClientVersion);
        SetActiveFalse();
    }

    void onSelected()
    {
        if (list != null)
        {
            readString(list.value);
        }
    }

    private void readString(string str)
    {
        OnlineHistorySelection selection = ScreenController.ParseHistoryEntry(str);
        inputIP.value = selection.Host ?? string.Empty;
        inputPort.value = selection.Port ?? string.Empty;
        inputPsw.value = selection.Password ?? string.Empty;
    }

    public override void show()
    {
        ApplyLegacyShow();
        ScreenController.SynchronizeLegacyShown();
    }

    public override void hide()
    {
        ApplyLegacyHide();
        ScreenController.SynchronizeLegacyHidden();
    }

    private void ApplyLegacyShow()
    {
        base.show();
        Program.I().room.RMSshow_clear();
        printFile();
        Program.charge();
        Program.I().ocgcore.returnServant = Program.I().selectServer;
    }

    private void ApplyLegacyHide()
    {
        base.hide();
    }

    public override void preFrameFunction()
    {
        base.preFrameFunction();
        Menu.checkCommend();
    }

    void printFile()
    {
        list.Clear();
        OnlineHistoryState historyState = ScreenController.LoadHistory();
        for (int i = 0; i < historyState.Entries.Count; i++)
        {
            list.AddItem(historyState.Entries[i]);
        }

        if (historyState.Entries.Count > 0)
        {
            inputIP.value = historyState.SelectedHost ?? string.Empty;
            inputPort.value = historyState.SelectedPort ?? string.Empty;
            inputPsw.value = historyState.SelectedPassword ?? string.Empty;
        }
    }

    void onClickExit()
    {
        UninstallSessionHandlers();
        ScreenController.Close(new OnlineCloseActions
        {
            ExitOnReturn = Program.exitOnReturn,
            ExitApplication = delegate { Program.I().menu.onClickExit(); },
            ShowMenu = delegate { Program.I().shiftToServant(Program.I().menu); },
            CloseConnection = CloseTcpConnection
        });
    }

    void onClickJoin()
    {
        string Name = UIHelper.getByName<UIInput>(gameObject, "name_").value;
        string ipString = UIHelper.getByName<UIInput>(gameObject, "ip_").value;
        string portString = UIHelper.getByName<UIInput>(gameObject, "port_").value;
        string pswString = UIHelper.getByName<UIInput>(gameObject, "psw_").value;
        string versionString = UIHelper.getByName<UIInput>(gameObject, "version_").value;
        KF_onlineGame(Name, ipString, portString, versionString, pswString);
    }

    public void KF_onlineGame(string Name,string ipString, string portString, string versionString, string pswString="")
    {
        name = Name;
        Config.Set("name", name);
        OnlineJoinResult result = ScreenController.PrepareJoin(new OnlineJoinRequest
        {
            IsVisible = isShowed,
            PlayerName = name,
            Host = ipString,
            Port = portString,
            Version = versionString,
            Password = pswString,
            ExistingHistory = list != null ? list.items : new List<string>()
        });

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            RMSshow_onlyYes("", InterString.Get(result.ErrorMessage), null);
            return;
        }

        if (!result.ShouldConnect || result.Connection == null)
        {
            return;
        }

        InstallSessionHandlers();
        list.items.Clear();
        for (int index = 0; index < result.UpdatedHistory.Count; index++)
        {
            list.items.Add(result.UpdatedHistory[index]);
        }

        string selectedHistory = result.UpdatedHistory.Count > 0 ? result.UpdatedHistory[0] : string.Empty;
        if (!string.IsNullOrEmpty(selectedHistory))
        {
            list.value = selectedHistory;
        }

        printFile();
        OnlineConnectRequest connection = result.Connection;
        (new Thread(() => { TcpHelper.join(connection.Host, connection.PlayerName, connection.Port, connection.Password, connection.Version); })).Start();
    }

    GameObject faceShow = null;

    void onClickFace()
    {
        name = UIHelper.getByName<UIInput>(gameObject, "name_").value;
        RMSshow_face("showFace", name);
        Config.Set("name", name);
    }

    private static void CloseTcpConnection()
    {
        if (TcpHelper.tcpClient != null && TcpHelper.tcpClient.Connected)
        {
            TcpHelper.tcpClient.Close();
        }
    }

    private void InstallSessionHandlers()
    {
        TcpHelper.SetStocMessageDispatcher(DispatchSessionMessage);
        TcpHelper.SetDisconnectHandler(HandleSessionDisconnect);
    }

    private void UninstallSessionHandlers()
    {
        TcpHelper.SetStocMessageDispatcher(null);
        TcpHelper.SetDisconnectHandler(null);
    }

    private bool DispatchSessionMessage(YGOSharp.Network.Enums.StocMessage message, System.IO.BinaryReader reader)
    {
        return SessionFlowService.TryDispatch((int)message, reader, new OnlineSessionDispatchActions
        {
            DispatchRoomMessage = delegate(int messageCode, System.IO.BinaryReader packetReader)
            {
                return LegacyTcpDispatchBridge.DispatchRoomMessage((YGOSharp.Network.Enums.StocMessage)messageCode, packetReader);
            },
            DispatchDuelMessage = delegate(int messageCode, System.IO.BinaryReader packetReader)
            {
                return LegacyTcpDispatchBridge.DispatchDuelMessage((YGOSharp.Network.Enums.StocMessage)messageCode, packetReader);
            }
        });
    }

    private bool HandleSessionDisconnect()
    {
        try
        {
            return SessionFlowService.HandleDisconnected(new OnlineSessionDisconnectRequest
            {
                IsDuelVisible = Program.I().ocgcore.isShowed,
                IsMenuVisible = Program.I().menu.isShowed,
                HasAssignedReturnTarget = Program.I().ocgcore.returnServant != null
            }, new OnlineSessionDisconnectActions
            {
                ReturnToAssignedTarget = delegate { Program.I().shiftToServant(Program.I().ocgcore.returnServant); },
                ReturnToSelectServer = delegate { Program.I().shiftToServant(Program.I().selectServer); },
                ShowDisconnectedMessage = delegate { Program.I().cardDescription.RMSshow_none(InterString.Get("连接被断开。")); },
                ShowOpponentLeftMessage = delegate { Program.I().cardDescription.RMSshow_none(InterString.Get("对方离开游戏，您现在可以截图。")); },
                ClearReplayRecordBuffer = delegate { TcpHelper.packagesInRecord.Clear(); },
                ForceQuitDuel = delegate { Program.I().ocgcore.forceMSquit(); }
            });
        }
        finally
        {
            UninstallSessionHandlers();
        }
    }

}

using App.Features.AI.Services;
using AppAiScreenController = App.UI.Screens.AI.AiScreenController;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

public class AIRoom : WindowServantSP
{
    #region ui
    UIselectableList superScrollView = null;
    string sort = "sortByTimeDeck";
    System.Diagnostics.Process serverProcess;
    System.Diagnostics.Process botProcess;
    private AiLocalServer _localServer;
    private WindBotRunner _botRunner;
    private AiFlowService aiFlowService;
    private AiRoomLaunchService aiRoomLaunchService;
    private AppAiScreenController screenController;
    private IList<AiRoomBotDefinition> Bots = new List<AiRoomBotDefinition>();

    private AiFlowService FlowService
    {
        get
        {
            if (aiFlowService == null)
            {
                aiFlowService = new AiFlowService(LaunchService);
            }

            return aiFlowService;
        }
    }

    private AiRoomLaunchService LaunchService
    {
        get
        {
            if (aiRoomLaunchService == null)
            {
                aiRoomLaunchService = new AiRoomLaunchService();
            }

            return aiRoomLaunchService;
        }
    }

    private AppAiScreenController ScreenController
    {
        get
        {
            if (screenController == null)
            {
                screenController = new AppAiScreenController(FlowService);
            }

            return screenController;
        }
    }

    private static string[] ReadConfigLines(string path)
    {
        List<string> lines = new List<string>();
        using (StringReader reader = new StringReader(RuntimeTextFile.ReadAllText(path)))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }
        }

        return lines.ToArray();
    }

    private void ReadBots(string confPath)
    {
        AiRoomBotDefinition[] parsedBots = LaunchService.ParseBots(ReadConfigLines(confPath));
        for (int index = 0; index < parsedBots.Length; index++)
        {
            Bots.Add(parsedBots[index]);
        }
    }

    public override void initialize()
    {
        createWindow(Program.I().new_ui_aiRoom);
        ScreenController.Bind(gameObject, FlowService, ApplyLegacyShow, ApplyLegacyHide);
        superScrollView = gameObject.GetComponentInChildren<UIselectableList>();
        superScrollView.selectedAction = onSelected;
        UIHelper.registEvent(gameObject, "start_", onStart);
        UIHelper.registEvent(gameObject, "exit_", onClickExit);
        UIHelper.trySetLableText(gameObject, "percyHint", InterString.Get("人机模式"));
        UIHelper.trySetLableText(gameObject, "botdesc_", InterString.Get("请选择对手。"));
        superScrollView.install();
        ReadBots(RuntimePaths.GetFilePath(RuntimeDirectory.Config, "bot.conf"));
        SetActiveFalse();
    }

    void onSelected()
    {
        int sel = superScrollView.selectedIndex;
        if (sel >= 0 && sel < Bots.Count)
            UIHelper.trySetLableText(gameObject, "botdesc_", Bots[sel].Description);
        else
            UIHelper.trySetLableText(gameObject, "botdesc_", InterString.Get("请选择对手。"));
    }

    void onSave()
    {
        //Config.Set("list_aideck", list_aideck.value);
        //Config.Set("list_airank", list_airank.value);
    }

    void onClickExit()
    {
        ScreenController.Close(new AiFlowCloseRequest
        {
            ExitOnReturn = Program.exitOnReturn
        }, new AiFlowCloseActions
        {
            StopServer = killServerProcess,
            ExitApplication = delegate
            {
                Program.I().menu.onClickExit();
            },
            ReturnToMenu = delegate
            {
                Program.I().shiftToServant(Program.I().menu);
            }
        });
    }

    public void killServerProcess()
    {
        if (serverProcess != null && !serverProcess.HasExited)
        {
            serverProcess.Kill();
        }
        serverProcess = null;

        _botRunner?.Dispose();
        _botRunner = null;
        _localServer?.Stop();
        _localServer = null;
    }

    void onStart()
    {
        launch(UIHelper.getByName<UIToggle>(gameObject, "lockhand_").value, UIHelper.getByName<UIToggle>(gameObject, "nocheck_").value, UIHelper.getByName<UIToggle>(gameObject, "noshuffle_").value);
    }

    void printFile()
    {
        superScrollView.clear();
        foreach (var bot in Bots)
        {
            superScrollView.add(bot.Name);
        }
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
        printFile();
        onSelected();
        Program.charge();
    }

    private void ApplyLegacyHide()
    {
        base.hide();
    }

    #endregion

    PrecyOcg precy;

    public void launch(bool lockhand, bool nocheck, bool noshuffle)
    {
        AiFlowLaunchResult result = ScreenController.TryLaunch(new AiFlowLaunchRequest
        {
            IsRoomVisible = isShowed,
            SelectedIndex = superScrollView != null ? superScrollView.selectedIndex : -1,
            Bots = Bots,
            LockHand = lockhand,
            NoCheck = nocheck,
            NoShuffle = noshuffle,
            Platform = Application.platform,
            PlayerName = Config.Get("name", "一秒一咕机会")
        }, new AiFlowLaunchActions
        {
            StopServer = killServerProcess,
            StartProcess = StartProcess,
            TrackProcess = delegate(AiRoomProcessHandle process)
            {
                if (process == null || process.NativeProcess == null)
                {
                    return;
                }

                System.Diagnostics.Process nativeProcess = process.NativeProcess as System.Diagnostics.Process;
                if (nativeProcess == null)
                {
                    return;
                }

                if (process.Id == "server")
                {
                    serverProcess = nativeProcess;
                }
                else if (process.Id == "bot")
                {
                    botProcess = nativeProcess;
                }

                ChildProcessTracker.AddProcess(nativeProcess);
            },
            ShowMessage = RMSshow_none,
            SetDuelReturnTarget = delegate
            {
                Program.I().ocgcore.returnServant = Program.I().aiRoom;
            },
            RunAsync = delegate(Action action)
            {
                new Thread(delegate()
                {
                    if (action != null)
                    {
                        action();
                    }
                }).Start();
            },
            Delay = delegate(int milliseconds)
            {
                Thread.Sleep(milliseconds);
            },
            JoinAiRoom = delegate(AiFlowJoinRequest request)
            {
                TcpHelper.join(request.Host, request.PlayerName, request.Port, request.Password, request.Version);
            },
            StartLocalServer = delegate
            {
                _localServer = new AiLocalServer();
                _localServer.Start();
                return _localServer;
            },
            StopLocalServer = delegate
            {
                _localServer?.Stop();
                _localServer = null;
            },
            StartBot = delegate(WindBotRunner runner)
            {
                _botRunner = runner;
                _botRunner.Start();
            }
        });

        if (!result.Started)
        {
            botProcess = null;
        }
    }

    private static AiRoomProcessHandle StartProcess(AiRoomProcessStartRequest request)
    {
        System.Diagnostics.Process process = new System.Diagnostics.Process();
        process.StartInfo.UseShellExecute = request.UseShellExecute;
        process.StartInfo.FileName = request.FileName;
        process.StartInfo.Arguments = request.Arguments;
        process.StartInfo.WorkingDirectory = request.WorkingDirectory;
        process.StartInfo.CreateNoWindow = request.CreateNoWindow;
        process.StartInfo.RedirectStandardOutput = request.RedirectStandardOutput;
        process.Start();

        return new AiRoomProcessHandle
        {
            Id = InferProcessId(request.FileName),
            NativeProcess = process,
            Kill = delegate
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            },
            HasExited = delegate { return process.HasExited; },
            ReadOutputLine = delegate { return process.StandardOutput.ReadLine(); }
        };
    }

    private static string InferProcessId(string fileName)
    {
        return string.Equals(fileName, "AI.Server.exe", StringComparison.Ordinal) ? "server" : "bot";
    }

    public override void preFrameFunction()
    {
        base.preFrameFunction();
        Menu.checkCommend();
    }
}

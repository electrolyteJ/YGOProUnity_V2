using App.Features.Menu.Services;
using AppMenuScreenController = App.UI.Screens.Menu.MenuScreenController;
using UnityEngine;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;

public class Menu : WindowServantSP 
{
    private static readonly MenuFlowService FlowService = new MenuFlowService();
    private static readonly AppMenuScreenController ScreenController = new AppMenuScreenController(FlowService);

    private static string GetVersionConfigPath()
    {
        return RuntimePaths.GetFilePath(RuntimeDirectory.Config, "ver.txt");
    }

    private static bool TryReadUpdateServerConfig(out string version, out string url)
    {
        string verPath = GetVersionConfigPath();
        RuntimeTextFile.EnsureFileExists(verPath);
        string[] lines = ReadUtf8Lines(verPath);
        if (lines.Length == 2 && Uri.IsWellFormedUriString(lines[1], UriKind.Absolute))
        {
            version = lines[0];
            url = lines[1];
            return true;
        }

        version = "";
        url = "";
        return false;
    }

    private static string[] ReadUtf8Lines(string path)
    {
        List<string> lines = new List<string>();
        using (StreamReader reader = new StreamReader(path, Encoding.UTF8))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }
        }

        return lines.ToArray();
    }

    //GameObject screen;
    public override void initialize()
    {
        createWindow(Program.I().new_ui_menu);
        ScreenController.Bind(gameObject, FlowService, ApplyLegacyShow, ApplyLegacyHide);
        UIHelper.registEvent(gameObject, "setting_", onClickSetting);
        UIHelper.registEvent(gameObject, "deck_", onClickSelectDeck);
        UIHelper.registEvent(gameObject, "online_", onClickOnline);
        UIHelper.registEvent(gameObject, "replay_", onClickReplay);
        UIHelper.registEvent(gameObject, "single_", onClickPizzle);
        UIHelper.registEvent(gameObject, "ai_", onClickAI);
        UIHelper.registEvent(gameObject, "exit_", onClickExit);
        Program.I().StartCoroutine(checkUpdate());
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

    string upurl = "";
    string uptxt = "";
    IEnumerator checkUpdate()
    {
        yield return new WaitForSeconds(1);
        string ver;
        string url;
        if (!TryReadUpdateServerConfig(out ver, out url))
        {
            Program.PrintToChat(InterString.Get("YGOPro2 自动更新：[ff5555]未设置更新服务器，无法检查更新。[-]@n请从官网重新下载安装完整版以获得更新。"));
            yield break;
        }
        UnityWebRequest www = UnityWebRequest.Get(url);
        www.SetRequestHeader("Cache-Control", "max-age=0, no-cache, no-store");
        www.SetRequestHeader("Pragma", "no-cache");
        yield return www.Send();
        try
        {
            string result = www.downloadHandler.text;
            string[] lines = RuntimeTextFile.SplitNormalizedLines(result);
            string[] mats = lines[0].Split(":.:");
            if (ver != mats[0])
            {
                upurl = mats[1];
                for(int i = 1; i < lines.Length; i++)
                {
                    uptxt += lines[i] + "\n";
                }
            }
            else
            {
                Program.PrintToChat(InterString.Get("YGOPro2 自动更新：[55ff55]当前已是最新版本。[-]"));
            }
        }
        catch (System.Exception e)
        {
            Program.PrintToChat(InterString.Get("YGOPro2 自动更新：[ff5555]检查更新失败！[-]"));
        }
    }

    public override void ES_RMS(string hashCode, List<messageSystemValue> result)
    {
        base.ES_RMS(hashCode, result);
        if (hashCode == "update" && result[0].value == "1")
        {
            Application.OpenURL(upurl);
        }
    }

    bool msgUpdateShowed = false;
    bool msgPermissionShowed = false;
    public override void preFrameFunction()
    {
        base.preFrameFunction();
        Menu.checkCommend();
        if (Program.noAccess && !msgPermissionShowed)
        {
            msgPermissionShowed = true;
            Program.PrintToChat(InterString.Get("[b][FF0000]NO ACCESS!! NO ACCESS!! NO ACCESS!![-][/b]") + "\n" + InterString.Get("访问程序目录出错，软件大部分功能将无法使用。@n请将 YGOPro2 安装到其他文件夹，或以管理员身份运行。"));
        }
        else if (upurl != "" && !msgUpdateShowed)
        {
            msgUpdateShowed = true;
            RMSshow_yesOrNo("update", InterString.Get("[b]发现更新！[/b]") + "\n" + uptxt + "\n" + InterString.Get("是否打开下载页面？"),
                new messageSystemValue { value = "1", hint = "yes" }, new messageSystemValue { value = "0", hint = "no" });
        }
    }

    public void onClickExit()
    {
        ScreenController.Navigate(MenuDestination.Exit, CreateNavigationActions());
    }

    void onClickOnline()
    {
        ScreenController.Navigate(MenuDestination.Online, CreateNavigationActions());
    }

    void onClickAI()
    {
        ScreenController.Navigate(MenuDestination.AI, CreateNavigationActions());
    }

    void onClickPizzle()
    {
        ScreenController.Navigate(MenuDestination.Puzzle, CreateNavigationActions());
    }

    void onClickReplay()
    {
        ScreenController.Navigate(MenuDestination.Replay, CreateNavigationActions());
    }

    void onClickSetting()
    {
        ScreenController.Navigate(MenuDestination.Settings, CreateNavigationActions());
    }

    void onClickSelectDeck()
    {
        ScreenController.Navigate(MenuDestination.Deck, CreateNavigationActions());
    }

    public void RestoreAsActiveRoute()
    {
        if (isShowed)
        {
            ScreenController.RestoreAsCurrentRoute();
        }
    }

    private void ApplyLegacyShow()
    {
        base.show();
        Program.charge();
    }

    private void ApplyLegacyHide()
    {
        base.hide();
    }

    public static void deleteShell()
    {
        try
        {
            Program.DeleteCommandShell();
        }
        catch (Exception)
        {
        }
    }

    static int lastTime = 0;
    public static void checkCommend()
    {
        if (Program.TimePassed() - lastTime > 1000)
        {
            lastTime = Program.TimePassed();
            if (Program.I().selectDeck == null)
            {
                return;
            }
            if (Program.I().selectReplay == null)
            {
                return;
            }
            if (Program.I().puzzleMode == null)
            {
                return;
            }
            if (Program.I().selectServer == null)
            {
                return;
            }
            try
            {
                Program.EnsureCommandShellExists();
            }
            catch (System.Exception e)
            {
                Program.noAccess = true;
                UnityEngine.Debug.Log(e);
            }
            string all = "";
            try
            {
                all = Program.ReadCommandShell();
                ScreenController.TryHandleShellCommand(all, CreateShellExecutionActions());
            }
            catch (System.Exception e)
            {
                Program.noAccess = true;
                UnityEngine.Debug.Log(e);
            }
            try
            {
                if (all != "")
                {
                    Program.ClearCommandShell();
                }
            }
            catch (System.Exception e)
            {
                Program.noAccess = true;
                UnityEngine.Debug.Log(e);
            }
        }
    }

    private static MenuNavigationActions CreateNavigationActions()
    {
        return MenuLegacyBindings.CreateNavigationActions(new MenuLegacyBindings.NavigationTargets
        {
            ShowSettings = delegate { Program.I().setting.show(); },
            ShowDeck = delegate { Program.I().shiftToServant(Program.I().selectDeck); },
            ShowOnline = delegate { Program.I().shiftToServant(Program.I().selectServer); },
            ShowReplay = delegate { Program.I().shiftToServant(Program.I().selectReplay); },
            ShowPuzzle = delegate { Program.I().shiftToServant(Program.I().puzzleMode); },
            ShowAI = delegate { Program.I().shiftToServant(Program.I().aiRoom); },
            ExitApplication = delegate
            {
                Program.I().quit();
                Program.Running = false;
                TcpHelper.SaveRecord();
                Process.GetCurrentProcess().Kill();
            },
        });
    }

    private static MenuShellExecutionActions CreateShellExecutionActions()
    {
        return MenuLegacyBindings.CreateShellExecutionActions(new MenuLegacyBindings.ShellTargets
        {
            InitializeFaces = delegate { UIHelper.iniFaces(); },
            OpenOnline = delegate(string host, string roomId, string userName, string password)
            {
                Program.I().selectServer.KF_onlineGame(host, roomId, userName, password);
            },
            OpenOnlineWithVersion = delegate(string host, string roomId, string userName, string password, string version)
            {
                Program.I().selectServer.KF_onlineGame(host, roomId, userName, password, version);
            },
            EditDeck = delegate(string deckName)
            {
                Program.I().selectDeck.KF_editDeck(deckName);
            },
            Replay = delegate(string replayName)
            {
                Program.I().selectReplay.KF_replay(replayName);
            },
            Puzzle = delegate(string puzzleName)
            {
                Program.I().puzzleMode.KF_puzzle(puzzleName);
            },
        });
    }
}

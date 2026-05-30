using System;
using System.Collections.Generic;
using App.Core;
using App.Features.Menu.Services;
using App.Features.Online.Services;
using App.UI.Common;
using App.UI.Screens.Menu;
using App.UI.Screens.AI;
using App.UI.Screens.Deck;
using App.UI.Screens.Online;
using App.UI.Screens.Puzzle;
using App.UI.Screens.Replay;
using UnityEditor;
using UnityEngine;

public static class Task3MenuSettingsBatchTest
{
    public static void Run()
    {
        int exitCode = 0;

        try
        {
            VerifyMenuLegacyBindings();
            VerifySettingsLegacyBindings();
            VerifyShellCommandDispatch();
            VerifyResolutionParsing();
            VerifyCloseUpPreset();
            VerifySharedUiRuntimeOwnership();
            VerifySharedMenuNavigationAcrossRegisteredRoutes();
            VerifySelectServerFlowHistoryJoinAndClose();
            VerifySelectServerControllerCloseAndRebindSynchronization();
            VerifyDuplicateRouteOwnerReplacement();
            Debug.Log("Task3MenuSettingsBatchTest OK");
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

    private static void VerifyMenuLegacyBindings()
    {
        string navigationState = string.Empty;
        MenuNavigationActions navigationActions = MenuLegacyBindings.CreateNavigationActions(new MenuLegacyBindings.NavigationTargets
        {
            ShowSettings = delegate { navigationState = "settings"; },
            ShowDeck = delegate { navigationState = "deck"; },
            ShowOnline = delegate { navigationState = "online"; },
            ShowReplay = delegate { navigationState = "replay"; },
            ShowPuzzle = delegate { navigationState = "puzzle"; },
            ShowAI = delegate { navigationState = "ai"; },
            ExitApplication = delegate { navigationState = "exit"; }
        });

        navigationActions.ShowSettings();
        if (navigationState != "settings")
        {
            throw new Exception("MenuLegacyBindings did not preserve navigation callback wiring.");
        }

        string shellState = string.Empty;
        MenuShellExecutionActions shellActions = MenuLegacyBindings.CreateShellExecutionActions(new MenuLegacyBindings.ShellTargets
        {
            InitializeFaces = delegate { shellState += "faces|"; },
            OpenOnline = delegate(string a, string b, string c, string d) { shellState += "online:" + a + "|" + b + "|" + c + "|" + d; },
            EditDeck = delegate(string deck) { shellState = "edit:" + deck; }
        });

        shellActions.InitializeFaces();
        shellActions.OpenOnline("h", "p", "u", "n");
        if (shellState != "faces|online:h|p|u|n")
        {
            throw new Exception("MenuLegacyBindings did not preserve shell callback wiring.");
        }

        shellActions.EditDeck("deck1");
        if (shellState != "edit:deck1")
        {
            throw new Exception("MenuLegacyBindings deck callback was not wired.");
        }
    }

    private static void VerifySettingsLegacyBindings()
    {
        GameObject root = new GameObject("Task3.SettingsRoot");
        LAZYsetting lazySetting = root.AddComponent<LAZYsetting>();

        UIToggle full = CreateChildComponent<UIToggle>(root, "full_");
        UIToggle ignoreWatcher = CreateChildComponent<UIToggle>(root, "ignoreWatcher_");
        UIToggle ignoreOpponent = CreateChildComponent<UIToggle>(root, "ignoreOP_");
        UIToggle smartSelect = CreateChildComponent<UIToggle>(root, "smartSelect_");
        UIToggle autoChain = CreateChildComponent<UIToggle>(root, "autoChain_");
        UIToggle handPosition = CreateChildComponent<UIToggle>(root, "handPosition_");
        UIToggle handMirrorPosition = CreateChildComponent<UIToggle>(root, "handmPosition_");
        UIToggle spyer = CreateChildComponent<UIToggle>(root, "spyer_");
        UIToggle resize = CreateChildComponent<UIToggle>(root, "resize_");
        UIToggle longField = CreateChildComponent<UIToggle>(root, "longField_");
        UIToggle high = CreateChildComponent<UIToggle>(root, "high_");
        UIToggle additional = CreateChildComponent<UIToggle>(root, "*mouseParticle");
        UISlider volume = CreateChildComponent<UISlider>(root, "vol_");
        UISlider size = CreateChildComponent<UISlider>(root, "size_");
        UISlider verticalSize = CreateChildComponent<UISlider>(root, "vSize_");
        UIPopupList showoffAtk = CreateChildComponent<UIPopupList>(root, "showoffATK");
        UIPopupList showoffStar = CreateChildComponent<UIPopupList>(root, "showoffStar");

        lazySetting.showoffATK = showoffAtk;
        lazySetting.showoffStar = showoffStar;

        SettingsViewState state = new SettingsViewState();
        state.FullScreen = true;
        state.IgnoreWatcher = true;
        state.IgnoreOpponent = true;
        state.SmartSelect = true;
        state.AutoChain = false;
        state.HandPosition = true;
        state.HandMirrorPosition = false;
        state.Spyer = true;
        state.Resize = true;
        state.LongField = false;
        state.HighQuality = true;
        state.Volume = 0.75f;
        state.Size = 0.5f;
        state.VerticalSize = 0.25f;
        state.ShowoffAttack = "1900";
        state.ShowoffStar = "6";
        state.AdditionalToggles["*mouseParticle"] = true;

        SettingsLegacyBindings.ApplyLoadedState(root, lazySetting, state);
        volume.value = state.Volume;
        size.value = state.Size;
        verticalSize.value = state.VerticalSize;

        if (!full.value || !ignoreWatcher.value || !ignoreOpponent.value || !smartSelect.value || autoChain.value || !additional.value)
        {
            throw new Exception("SettingsLegacyBindings did not apply toggle state correctly.");
        }

        SettingsViewState captured = SettingsLegacyBindings.CaptureCurrentState(root, lazySetting, delegate { return true; });
        if (!captured.FullScreen || captured.ShowoffAttack != "1900" || captured.ShowoffStar != "6" || Math.Abs(captured.Volume - 0.75f) > 0.0001f)
        {
            throw new Exception("SettingsLegacyBindings did not capture current state correctly.");
        }

        bool monsterCloud = false;
        bool mouseVisible = false;
        bool longFieldEnabled = false;
        float fieldSize = 0f;
        float verticalScale = 0f;
        bool refreshed = false;
        int qualityLevel = -1;

        SettingsRuntimeActions runtimeActions = SettingsLegacyBindings.CreateRuntimeActions(
            delegate(bool value) { monsterCloud = value; },
            delegate(bool value) { mouseVisible = value; },
            delegate(bool value) { longFieldEnabled = value; },
            delegate(float value) { fieldSize = value; },
            delegate(float value) { verticalScale = value; },
            delegate { refreshed = true; },
            delegate(int value) { qualityLevel = value; });

        runtimeActions.SetMonsterCloudEnabled(true);
        runtimeActions.SetMouseParticleVisible(true);
        runtimeActions.SetLongFieldEnabled(true);
        runtimeActions.SetFieldSize(1.5f);
        runtimeActions.SetVerticalScale(5f);
        runtimeActions.RefreshFieldLayout();
        runtimeActions.SetQualityLevel(5);

        if (!monsterCloud || !mouseVisible || !longFieldEnabled || Math.Abs(fieldSize - 1.5f) > 0.0001f || Math.Abs(verticalScale - 5f) > 0.0001f || !refreshed || qualityLevel != 5)
        {
            throw new Exception("SettingsLegacyBindings runtime actions were not wired correctly.");
        }

        longFieldEnabled = false;
        verticalScale = 0f;
        qualityLevel = -1;
        SettingsLegacyBindings.ApplyInitialRuntimeState(
            new SettingsScreenController(new SettingsFlowService()),
            state,
            runtimeActions);

        if (qualityLevel != 5
            || longFieldEnabled
            || Math.Abs(fieldSize - (1f + state.Size * 0.21f)) > 0.0001f
            || Math.Abs(verticalScale - (4f + 2f * state.VerticalSize)) > 0.0001f)
        {
            throw new Exception("SettingsLegacyBindings initial runtime replay was not wired correctly.");
        }

        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void VerifyShellCommandDispatch()
    {
        MenuScreenController controller = new MenuScreenController(new MenuFlowService());
        string handledCommand = string.Empty;

        MenuShellExecutionActions handlers = new MenuShellExecutionActions
        {
            InitializeFaces = delegate { handledCommand += "faces|"; },
            OpenOnline = delegate(string a, string b, string c, string d)
            {
                handledCommand += "online:" + a + "|" + b + "|" + c + "|" + d;
            },
            EditDeck = delegate(string deckName)
            {
                handledCommand = "edit:" + deckName;
            }
        };

        if (!controller.TryHandleShellCommand("online h p u n", handlers))
        {
            throw new Exception("Expected online shell command to be handled.");
        }

        if (handledCommand != "faces|online:h|p|u|n")
        {
            throw new Exception("Unexpected online command payload: " + handledCommand);
        }

        if (!controller.TryHandleShellCommand("edit deck1", handlers))
        {
            throw new Exception("Expected edit shell command to be handled.");
        }

        if (handledCommand != "edit:deck1")
        {
            throw new Exception("Unexpected edit command payload: " + handledCommand);
        }

        if (controller.TryHandleShellCommand("unknown", handlers))
        {
            throw new Exception("Unknown shell command should not be handled.");
        }
    }

    private static void VerifyResolutionParsing()
    {
        SettingsFlowService flowService = new SettingsFlowService();
        ResolutionRequest request;

        if (!flowService.TryParseResolution("1280*720", true, out request))
        {
            throw new Exception("Expected valid resolution to parse.");
        }

        if (request.Width != 1280 || request.Height != 720 || !request.FullScreen)
        {
            throw new Exception("Parsed resolution request did not preserve values.");
        }

        if (flowService.TryParseResolution("broken", false, out request))
        {
            throw new Exception("Invalid resolution text should not parse.");
        }
    }

    private static void VerifyCloseUpPreset()
    {
        SettingsScreenController controller = new SettingsScreenController(new SettingsFlowService());
        float alpha = -1f;
        float size = -1f;

        controller.ApplyCloseUpPreset(
            true,
            delegate(float value) { alpha = value; },
            delegate(float value) { size = value; });

        if (Math.Abs(alpha - 0.6666f) > 0.0001f || Math.Abs(size - 1f) > 0.0001f)
        {
            throw new Exception("Enabled close-up preset did not apply expected values.");
        }

        controller.ApplyCloseUpPreset(
            false,
            delegate(float value) { alpha = value; },
            delegate(float value) { size = value; });

        if (Math.Abs(alpha) > 0.0001f || Math.Abs(size) > 0.0001f)
        {
            throw new Exception("Disabled close-up preset did not clear expected values.");
        }
    }

    private static void VerifySharedUiRuntimeOwnership()
    {
        if (AppUiRoot.HasCurrent)
        {
            UnityEngine.Object.DestroyImmediate(AppUiRoot.Current.gameObject);
        }

        GameObject menuObject = new GameObject("Task3.Menu");
        GameObject settingsObject = new GameObject("Task3.Settings");
        AppUiRoot uiRoot = AppUiRoot.EnsureInstance();
        GameObject rootObject = uiRoot.gameObject;

        bool menuShown = false;
        bool menuHidden = false;
        bool settingsShown = false;
        bool settingsHidden = false;

        MenuScreenController menuController = new MenuScreenController(new MenuFlowService());
        menuController.Bind(
            menuObject,
            new MenuFlowService(),
            delegate
            {
                menuShown = true;
                menuObject.SetActive(true);
            },
            delegate
            {
                menuHidden = true;
                menuObject.SetActive(false);
            });

        SettingsScreenController settingsController = new SettingsScreenController(new SettingsFlowService());
        settingsController.Bind(
            settingsObject,
            new SettingsFlowService(),
            delegate
            {
                settingsShown = true;
                settingsObject.SetActive(true);
            },
            delegate
            {
                settingsHidden = true;
                settingsObject.SetActive(false);
            });

        menuController.SynchronizeLegacyShown();
        if (uiRoot.Router.CurrentRouteKey != MenuScreenController.Route)
        {
            throw new Exception("Menu route was not registered as the current route.");
        }

        bool settingsFallbackCalled = false;
        menuController.Navigate(
            MenuDestination.Settings,
            new MenuNavigationActions
            {
                ShowSettings = delegate { settingsFallbackCalled = true; }
            });

        if (settingsFallbackCalled)
        {
            throw new Exception("Menu navigation fell back instead of using the shared host route.");
        }

        if (uiRoot.Router.CurrentRouteKey != SettingsScreenController.Route)
        {
            throw new Exception("Shared host did not switch to the settings route.");
        }

        if (!menuHidden || !settingsShown)
        {
            throw new Exception("Expected shared host route change to hide menu and show settings.");
        }

        bool closeFallbackCalled = false;
        settingsController.Close(delegate { closeFallbackCalled = true; });
        if (closeFallbackCalled)
        {
            throw new Exception("Settings close fell back instead of navigating back through the shared host.");
        }

        if (uiRoot.Router.CurrentRouteKey != MenuScreenController.Route)
        {
            throw new Exception("Closing settings did not return to the menu route.");
        }

        if (!settingsHidden || !menuShown)
        {
            throw new Exception("Returning from settings did not restore the menu route lifecycle.");
        }

        bool messageDismissed = false;
        bool confirmationCompleted = false;
        bool confirmationValue = false;

        uiRoot.ShowMessage("m", "body", delegate { messageDismissed = true; });
        uiRoot.ShowConfirmation("c", "body", delegate(bool value)
        {
            confirmationCompleted = true;
            confirmationValue = value;
        });

        if (!uiRoot.Dialogs.HasActiveDialog || uiRoot.Dialogs.PendingCount != 1)
        {
            throw new Exception("DialogHost did not keep one active dialog plus one queued dialog.");
        }

        uiRoot.Dialogs.Dismiss();
        if (!messageDismissed)
        {
            throw new Exception("Dismissing the first dialog did not invoke its callback.");
        }

        if (!uiRoot.Dialogs.HasActiveDialog || !uiRoot.Dialogs.CurrentDialog.RequiresDecision)
        {
            throw new Exception("Queued confirmation dialog was not promoted after dismiss.");
        }

        uiRoot.Dialogs.Confirm();
        if (!confirmationCompleted || !confirmationValue)
        {
            throw new Exception("Confirmation dialog did not complete with the expected value.");
        }

        IDisposable outerBusy = uiRoot.ShowBusy("outer");
        IDisposable innerBusy = uiRoot.ShowBusy("inner");
        if (!uiRoot.BusyOverlay.IsBusy || uiRoot.BusyOverlay.BusyDepth != 2 || uiRoot.BusyOverlay.BusyMessage != "inner")
        {
            throw new Exception("Busy overlay did not track nested busy scopes.");
        }

        innerBusy.Dispose();
        if (!uiRoot.BusyOverlay.IsBusy || uiRoot.BusyOverlay.BusyDepth != 1 || uiRoot.BusyOverlay.BusyMessage != "outer")
        {
            throw new Exception("Busy overlay did not restore the previous busy scope message.");
        }

        outerBusy.Dispose();
        if (uiRoot.BusyOverlay.IsBusy)
        {
            throw new Exception("Busy overlay remained busy after all scopes were disposed.");
        }

        uiRoot.ShowError("err");
        if (!uiRoot.BusyOverlay.HasError || uiRoot.BusyOverlay.ErrorMessage != "err")
        {
            throw new Exception("Busy overlay did not expose the global error surface.");
        }

        uiRoot.ClearError();
        if (uiRoot.BusyOverlay.HasError)
        {
            throw new Exception("Global error surface did not clear.");
        }

        UnityEngine.Object.DestroyImmediate(menuObject);
        UnityEngine.Object.DestroyImmediate(settingsObject);
        UnityEngine.Object.DestroyImmediate(rootObject);
    }

    private static void VerifyDuplicateRouteOwnerReplacement()
    {
        if (AppUiRoot.HasCurrent)
        {
            UnityEngine.Object.DestroyImmediate(AppUiRoot.Current.gameObject);
        }

        AppUiRoot uiRoot = AppUiRoot.EnsureInstance();
        GameObject firstObject = new GameObject("Task3.Menu.First");
        GameObject secondObject = new GameObject("Task3.Menu.Second");
        bool firstShown = false;
        bool secondShown = false;

        MenuScreenController firstController = new MenuScreenController(new MenuFlowService());
        firstController.Bind(
            firstObject,
            new MenuFlowService(),
            delegate
            {
                firstShown = true;
                firstObject.SetActive(true);
            },
            delegate { firstObject.SetActive(false); });
        firstController.SynchronizeLegacyShown();

        MenuScreenController replacementController = new MenuScreenController(new MenuFlowService());
        replacementController.Bind(
            secondObject,
            new MenuFlowService(),
            delegate
            {
                secondShown = true;
                secondObject.SetActive(true);
            },
            delegate { secondObject.SetActive(false); });

        uiRoot.Navigate(MenuScreenController.Route);

        if (!secondShown || firstShown && secondObject == firstObject)
        {
            throw new Exception("Duplicate route owner replacement did not promote the newest controller.");
        }

        if (!ReferenceEquals(uiRoot.Router.CurrentController, replacementController))
        {
            throw new Exception("ScreenRouter did not retain the replacement controller as current.");
        }

        UnityEngine.Object.DestroyImmediate(firstObject);
        UnityEngine.Object.DestroyImmediate(secondObject);
        UnityEngine.Object.DestroyImmediate(uiRoot.gameObject);
    }

    private static void VerifySharedMenuNavigationAcrossRegisteredRoutes()
    {
        ResetUiRoot();
        AppUiRoot uiRoot = AppUiRoot.EnsureInstance();
        GameObject menuRoot = new GameObject("Task3.Menu.Navigation");
        GameObject onlineRoot = new GameObject("Task3.Online.Navigation");
        GameObject replayRoot = new GameObject("Task3.Replay.Navigation");
        GameObject puzzleRoot = new GameObject("Task3.Puzzle.Navigation");
        GameObject aiRoot = new GameObject("Task3.Ai.Navigation");
        GameObject deckRoot = new GameObject("Task3.Deck.Navigation");
        GameObject settingsRoot = new GameObject("Task3.Settings.Navigation");

        try
        {
            MenuScreenController menuController = new MenuScreenController(new MenuFlowService());
            menuController.Bind(menuRoot, new MenuFlowService(), delegate { menuRoot.SetActive(true); }, delegate { menuRoot.SetActive(false); });
            RegisterTestRoute(onlineRoot, SelectServerScreenController.Route);
            RegisterTestRoute(replayRoot, ReplayScreenController.Route);
            RegisterTestRoute(puzzleRoot, PuzzleScreenController.Route);
            RegisterTestRoute(aiRoot, AiScreenController.Route);
            RegisterTestRoute(deckRoot, DeckScreenController.Route);
            RegisterTestRoute(settingsRoot, SettingsScreenController.Route);

            menuController.SynchronizeLegacyShown();

            VerifySharedMenuNavigation(menuController, uiRoot, MenuDestination.Online, SelectServerScreenController.Route);
            VerifySharedMenuNavigation(menuController, uiRoot, MenuDestination.Replay, ReplayScreenController.Route);
            VerifySharedMenuNavigation(menuController, uiRoot, MenuDestination.Puzzle, PuzzleScreenController.Route);
            VerifySharedMenuNavigation(menuController, uiRoot, MenuDestination.AI, AiScreenController.Route);
            VerifySharedMenuNavigation(menuController, uiRoot, MenuDestination.Deck, DeckScreenController.Route);
            VerifySharedMenuNavigation(menuController, uiRoot, MenuDestination.Settings, SettingsScreenController.Route);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(menuRoot);
            UnityEngine.Object.DestroyImmediate(onlineRoot);
            UnityEngine.Object.DestroyImmediate(replayRoot);
            UnityEngine.Object.DestroyImmediate(puzzleRoot);
            UnityEngine.Object.DestroyImmediate(aiRoot);
            UnityEngine.Object.DestroyImmediate(deckRoot);
            UnityEngine.Object.DestroyImmediate(settingsRoot);
            ResetUiRoot();
        }
    }

    private static void VerifySelectServerFlowHistoryJoinAndClose()
    {
        List<string> writtenHistory = new List<string>();
        OnlineFlowService service = new OnlineFlowService(
            new TestPaths("/task3-online"),
            new TestTextStorage(),
            new OnlineFlowCallbacks
            {
                ReadHistoryLines = delegate
                {
                    return new[]
                    {
                        "(0x233)127.0.0.1:7911 secret",
                        "remote.host:1234 pass"
                    };
                },
                WriteHistoryLines = delegate(IList<string> entries)
                {
                    writtenHistory.Clear();
                    writtenHistory.AddRange(entries);
                }
            });

        OnlineHistoryState history = service.LoadHistory();
        if (history.Entries.Count != 2
            || history.Entries[0] != "127.0.0.1:7911 secret"
            || history.SelectedHost != "127.0.0.1"
            || history.SelectedPort != "7911"
            || history.SelectedPassword != "secret")
        {
            throw new Exception("OnlineFlowService should normalize host history entries and parse the first selection.");
        }

        OnlineHistorySelection selection = service.ParseHistoryEntry("alpha.host:8888 token");
        if (selection.Host != "alpha.host" || selection.Port != "8888" || selection.Password != "token")
        {
            throw new Exception("OnlineFlowService should parse host, port, and password from history entries.");
        }

        OnlineJoinResult hiddenJoin = service.PrepareJoin(new OnlineJoinRequest
        {
            IsVisible = false,
            PlayerName = "Task3",
            Host = "alpha.host",
            Port = "7911",
            Version = "0x233",
            Password = "pw",
            ExistingHistory = history.Entries
        });
        if (hiddenJoin.ShouldConnect || hiddenJoin.Connection != null || writtenHistory.Count != 0)
        {
            throw new Exception("OnlineFlowService should ignore join requests while the legacy screen is hidden.");
        }

        OnlineJoinResult invalidHost = service.PrepareJoin(new OnlineJoinRequest
        {
            IsVisible = true,
            PlayerName = "Task3",
            Host = string.Empty,
            Port = "7911",
            Version = "0x233",
            Password = "pw",
            ExistingHistory = history.Entries
        });
        if (invalidHost.ErrorMessage != "非法输入！请检查输入的主机名。")
        {
            throw new Exception("OnlineFlowService should preserve the invalid host validation message.");
        }

        OnlineJoinResult invalidName = service.PrepareJoin(new OnlineJoinRequest
        {
            IsVisible = true,
            PlayerName = string.Empty,
            Host = "alpha.host",
            Port = "7911",
            Version = "0x233",
            Password = "pw",
            ExistingHistory = history.Entries
        });
        if (invalidName.ErrorMessage != "昵称不能为空。")
        {
            throw new Exception("OnlineFlowService should preserve the empty nickname validation message.");
        }

        OnlineJoinResult join = service.PrepareJoin(new OnlineJoinRequest
        {
            IsVisible = true,
            PlayerName = "Task3",
            Host = "remote.host",
            Port = "1234",
            Version = "0x233",
            Password = "pass",
            ExistingHistory = new List<string>
            {
                "old.1:1000 one",
                "remote.host:1234 pass",
                "old.2:2000 two",
                "old.3:3000 three",
                "old.4:4000 four",
                "old.5:5000 five"
            }
        });

        if (!join.ShouldConnect
            || join.Connection == null
            || join.Connection.PlayerName != "Task3"
            || join.Connection.Host != "remote.host"
            || join.Connection.Port != "1234"
            || join.Connection.Version != "0x233"
            || join.Connection.Password != "pass")
        {
            throw new Exception("OnlineFlowService should preserve the online join payload when launching.");
        }

        if (join.UpdatedHistory.Count != 5
            || join.UpdatedHistory[0] != "remote.host:1234 pass"
            || join.UpdatedHistory[1] != "old.1:1000 one"
            || join.UpdatedHistory[4] != "old.4:4000 four")
        {
            throw new Exception("OnlineFlowService should move the newest host to the front, de-duplicate it, and keep only five entries.");
        }

        if (writtenHistory.Count != 5 || writtenHistory[0] != "remote.host:1234 pass")
        {
            throw new Exception("OnlineFlowService should persist the updated host history.");
        }

        bool closedConnection = false;
        bool showedMenu = false;
        bool exited = false;
        service.Close(new OnlineCloseActions
        {
            ExitOnReturn = false,
            CloseConnection = delegate { closedConnection = true; },
            ShowMenu = delegate { showedMenu = true; },
            ExitApplication = delegate { exited = true; }
        });
        if (!closedConnection || !showedMenu || exited)
        {
            throw new Exception("OnlineFlowService should close the connection and return to menu when exit-on-return is disabled.");
        }

        closedConnection = false;
        showedMenu = false;
        exited = false;
        service.Close(new OnlineCloseActions
        {
            ExitOnReturn = true,
            CloseConnection = delegate { closedConnection = true; },
            ShowMenu = delegate { showedMenu = true; },
            ExitApplication = delegate { exited = true; }
        });
        if (!closedConnection || showedMenu || !exited)
        {
            throw new Exception("OnlineFlowService should close the connection and exit when exit-on-return is enabled.");
        }
    }

    private static void VerifySelectServerControllerCloseAndRebindSynchronization()
    {
        ResetUiRoot();
        AppUiRoot uiRoot = AppUiRoot.EnsureInstance();
        GameObject menuRoot = new GameObject("Task3.Menu.Online.Close");
        GameObject firstRoot = new GameObject("Task3.Online.First");
        GameObject secondRoot = new GameObject("Task3.Online.Second");
        int secondShowCount = 0;
        int secondHideCount = 0;

        try
        {
            uiRoot.RegisterScreen(new TestScreenController(MenuScreenController.Route, menuRoot));

            SelectServerScreenController controller = new SelectServerScreenController(
                new OnlineFlowService(new TestPaths("/task3-online-ui"), new TestTextStorage(), new OnlineFlowCallbacks()));
            controller.Bind(firstRoot, null, delegate { firstRoot.SetActive(true); }, delegate { firstRoot.SetActive(false); });

            controller.SynchronizeLegacyShown();
            if (uiRoot.Router.CurrentRouteKey != SelectServerScreenController.Route || !firstRoot.activeSelf)
            {
                throw new Exception("SelectServerScreenController should register and activate the shared online route during legacy show synchronization.");
            }

            uiRoot.Navigate(MenuScreenController.Route);
            uiRoot.Navigate(controller.RouteKey);

            bool fallbackMenu = false;
            bool fallbackExit = false;
            bool connectionClosed = false;
            controller.Close(new OnlineCloseActions
            {
                ExitOnReturn = false,
                ShowMenu = delegate { fallbackMenu = true; },
                ExitApplication = delegate { fallbackExit = true; },
                CloseConnection = delegate { connectionClosed = true; }
            });
            if (fallbackMenu || fallbackExit || connectionClosed || uiRoot.Router.CurrentRouteKey != MenuScreenController.Route)
            {
                throw new Exception("SelectServerScreenController should navigate back through the shared router before using legacy close actions.");
            }

            uiRoot.Router.HideCurrent();
            controller.Close(new OnlineCloseActions
            {
                ExitOnReturn = false,
                ShowMenu = delegate { fallbackMenu = true; },
                ExitApplication = delegate { fallbackExit = true; },
                CloseConnection = delegate { connectionClosed = true; }
            });
            if (!fallbackMenu || fallbackExit || !connectionClosed)
            {
                throw new Exception("SelectServerScreenController should delegate close behavior to OnlineFlowService when no shared route history exists.");
            }

            controller.Bind(
                secondRoot,
                null,
                delegate
                {
                    secondShowCount++;
                    secondRoot.SetActive(true);
                },
                delegate
                {
                    secondHideCount++;
                    secondRoot.SetActive(false);
                });

            controller.SynchronizeLegacyShown();
            if (uiRoot.Router.CurrentRouteKey != SelectServerScreenController.Route || !secondRoot.activeSelf)
            {
                throw new Exception("SelectServerScreenController should move legacy shown state onto the rebound root.");
            }

            secondRoot.SetActive(false);
            controller.SynchronizeLegacyHidden();
            if (uiRoot.Router.CurrentRouteKey == SelectServerScreenController.Route || secondRoot.activeSelf)
            {
                throw new Exception("SelectServerScreenController should clear the shared online route when the rebound legacy screen hides.");
            }

            uiRoot.Navigate(controller.RouteKey);
            if (secondShowCount != 1 || !secondRoot.activeSelf)
            {
                throw new Exception("SelectServerScreenController navigation should target the rebound root.");
            }

            uiRoot.Router.HideCurrent();
            if (secondHideCount != 1 || secondRoot.activeSelf)
            {
                throw new Exception("SelectServerScreenController hide should target the rebound root.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(menuRoot);
            UnityEngine.Object.DestroyImmediate(firstRoot);
            UnityEngine.Object.DestroyImmediate(secondRoot);
            ResetUiRoot();
        }
    }

    private static void VerifySharedMenuNavigation(MenuScreenController menuController, AppUiRoot uiRoot, MenuDestination destination, string expectedRoute)
    {
        bool fallbackInvoked = false;
        menuController.Navigate(destination, new MenuNavigationActions
        {
            ShowSettings = delegate { fallbackInvoked = true; },
            ShowDeck = delegate { fallbackInvoked = true; },
            ShowOnline = delegate { fallbackInvoked = true; },
            ShowReplay = delegate { fallbackInvoked = true; },
            ShowPuzzle = delegate { fallbackInvoked = true; },
            ShowAI = delegate { fallbackInvoked = true; },
        });

        if (fallbackInvoked)
        {
            throw new Exception("MenuScreenController should prefer registered shared routes before legacy navigation actions for " + destination + ".");
        }

        if (uiRoot.Router.CurrentRouteKey != expectedRoute)
        {
            throw new Exception("MenuScreenController did not navigate to the expected shared route for " + destination + ".");
        }

        uiRoot.Navigate(MenuScreenController.Route);
    }

    private static void RegisterTestRoute(GameObject root, string routeKey)
    {
        AppUiRoot.EnsureInstance().RegisterScreen(new TestScreenController(routeKey, root));
    }

    private static void ResetUiRoot()
    {
        if (AppUiRoot.HasCurrent)
        {
            UnityEngine.Object.DestroyImmediate(AppUiRoot.Current.gameObject);
        }
    }

    private sealed class TestPaths : IPlatformPaths
    {
        private readonly string projectRoot;

        public TestPaths(string projectRoot)
        {
            this.projectRoot = projectRoot;
        }

        public string ProjectRoot
        {
            get { return projectRoot; }
        }

        public string GetDirectoryPath(string directoryName)
        {
            return Combine(projectRoot, directoryName);
        }

        public string GetProjectRootFilePath(params string[] segments)
        {
            string path = projectRoot;
            for (int index = 0; index < segments.Length; index++)
            {
                path = Combine(path, segments[index]);
            }

            return path;
        }

        public string GetFilePath(string directoryName, params string[] segments)
        {
            string path = GetDirectoryPath(directoryName);
            for (int index = 0; index < segments.Length; index++)
            {
                path = Combine(path, segments[index]);
            }

            return path;
        }

        private static string Combine(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
            {
                return right ?? string.Empty;
            }

            if (string.IsNullOrEmpty(right))
            {
                return left;
            }

            return left.TrimEnd('/') + "/" + right.TrimStart('/');
        }
    }

    private sealed class TestTextStorage : IFileStorage
    {
        public bool FileExists(string path)
        {
            return false;
        }

        public string ReadAllText(string path)
        {
            return string.Empty;
        }

        public void WriteAllText(string path, string contents)
        {
        }
    }

    private static T CreateChildComponent<T>(GameObject root, string name) where T : Component
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(root.transform, false);
        return child.AddComponent<T>();
    }

    private sealed class TestScreenController : App.UI.Common.Navigation.IUiScreenController
    {
        private readonly string routeKey;
        private readonly GameObject root;

        public TestScreenController(string routeKey, GameObject root)
        {
            this.routeKey = routeKey;
            this.root = root;
        }

        public string RouteKey
        {
            get { return routeKey; }
        }

        public bool IsVisible
        {
            get { return root != null && root.activeSelf; }
        }

        public void Show(object parameter)
        {
            if (root != null)
            {
                root.SetActive(true);
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }
    }
}

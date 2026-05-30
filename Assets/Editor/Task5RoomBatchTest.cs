using System;
using App.Core;
using App.Features.AI.Services;
using App.Features.Room.Services;
using App.UI.Common;
using App.UI.Common.Navigation;
using App.UI.Screens.Room;
using UnityEditor;
using UnityEngine;

public static class Task5RoomBatchTest
{
    public static void Run()
    {
        int exitCode = 0;

        try
        {
            VerifyRoomFlowActions();
            VerifyAiEntryActions();
            VerifyAiRoomLaunchService();
            VerifyAiFlowService();
            VerifyRoomControllerRebindSynchronization();
            VerifyRoomControllerCloseBehavior();
            Debug.Log("Task5RoomBatchTest OK");
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

    private static void VerifyRoomFlowActions()
    {
        RoomFlowService service = new RoomFlowService(new TestPaths("/task5"));

        RecordingRoomActions chatActions = new RecordingRoomActions();
        if (service.SubmitChat(string.Empty, chatActions.CreateInteractionActions()))
        {
            throw new Exception("RoomFlowService should ignore empty chat submissions.");
        }

        if (!service.SubmitChat("hello room", chatActions.CreateInteractionActions()) || chatActions.ChatMessage != "hello room")
        {
            throw new Exception("RoomFlowService did not route chat submission through the injected callback.");
        }

        RecordingRoomActions readyActions = new RecordingRoomActions();
        service.HandleReadyToggle(
            new RoomSeatInteractionRequest
            {
                SelfType = 1,
                SeatCount = 4,
                IsPrepared = false,
                SelectedDeckName = "alpha"
            },
            readyActions.CreateInteractionActions());
        if (readyActions.UpdatedDeckPath != "/task5/deck/alpha.ydk" || readyActions.ReadyCount != 1 || readyActions.NotReadyCount != 0)
        {
            throw new Exception("RoomFlowService should update the selected deck and ready the local player.");
        }

        RecordingRoomActions unreadyActions = new RecordingRoomActions();
        service.HandleReadyToggle(
            new RoomSeatInteractionRequest
            {
                SelfType = 1,
                SeatCount = 4,
                IsPrepared = true,
                SelectedDeckName = "alpha"
            },
            unreadyActions.CreateInteractionActions());
        if (unreadyActions.NotReadyCount != 1 || unreadyActions.ReadyCount != 0 || unreadyActions.UpdatedDeckPath != null)
        {
            throw new Exception("RoomFlowService should unready an already prepared local player without touching the deck.");
        }

        RecordingRoomActions selectActions = new RecordingRoomActions();
        service.SelectDeck(
            "beta",
            new RoomSeatInteractionRequest
            {
                SelfType = 0,
                SeatCount = 4,
                IsPrepared = true,
                SelectedDeckName = "alpha"
            },
            selectActions.CreateInteractionActions());
        if (selectActions.StoredDeckName != "beta"
            || selectActions.NotReadyCount != 1
            || selectActions.ReadyCount != 1
            || selectActions.UpdatedDeckPath != "/task5/deck/beta.ydk")
        {
            throw new Exception("RoomFlowService should refresh ready state when the prepared player changes decks.");
        }

        RecordingRoomActions prepareActions = new RecordingRoomActions();
        service.HandlePrepareChanged(true, "gamma", prepareActions.CreateInteractionActions());
        if (prepareActions.ReadyCount != 1 || prepareActions.UpdatedDeckPath != "/task5/deck/gamma.ydk" || prepareActions.NotReadyCount != 0)
        {
            throw new Exception("RoomFlowService should send the current deck before marking the room player ready.");
        }

        RecordingRoomActions prepareOffActions = new RecordingRoomActions();
        service.HandlePrepareChanged(false, "gamma", prepareOffActions.CreateInteractionActions());
        if (prepareOffActions.NotReadyCount != 1 || prepareOffActions.ReadyCount != 0)
        {
            throw new Exception("RoomFlowService should unready the player when prepare state is cleared.");
        }

        RecordingRoomActions miscActions = new RecordingRoomActions();
        service.MoveToDuelist(miscActions.CreateInteractionActions());
        service.MoveToObserver(miscActions.CreateInteractionActions());
        service.StartDuel(miscActions.CreateInteractionActions());
        service.KickPlayer(3, miscActions.CreateInteractionActions());
        service.Close(new RoomCloseActions
        {
            LeaveRoom = delegate { miscActions.CloseCount++; }
        });
        if (miscActions.MoveToDuelistCount != 1
            || miscActions.MoveToObserverCount != 1
            || miscActions.StartCount != 1
            || miscActions.KickedSeat != 3
            || miscActions.CloseCount != 1)
        {
            throw new Exception("RoomFlowService did not route the expected lobby actions.");
        }
    }

    private static void VerifyRoomControllerCloseBehavior()
    {
        AppUiRoot uiRoot = AppUiRoot.EnsureInstance();
        GameObject menuRoot = new GameObject("Task5.MenuRoot");
        GameObject roomRoot = new GameObject("Task5.RoomRoot");

        try
        {
            TestScreenController menuController = new TestScreenController("menu.main", menuRoot);
            uiRoot.RegisterScreen(menuController);

            RoomScreenController roomController = new RoomScreenController(new RoomFlowService(new TestPaths("/task5-ui")));
            roomController.Bind(roomRoot, null, delegate { roomRoot.SetActive(true); }, delegate { roomRoot.SetActive(false); });

            roomController.SynchronizeLegacyShown();
            if (uiRoot.Router.CurrentRouteKey != RoomScreenController.Route || !roomRoot.activeSelf)
            {
                throw new Exception("RoomScreenController did not register the room route with the shared UI root.");
            }

            uiRoot.Navigate(menuController.RouteKey);
            uiRoot.Navigate(roomController.RouteKey);

            bool fallbackInvoked = false;
            roomController.Close(new RoomCloseActions
            {
                LeaveRoom = delegate { fallbackInvoked = true; }
            });
            if (fallbackInvoked || uiRoot.Router.CurrentRouteKey != menuController.RouteKey)
            {
                throw new Exception("RoomScreenController should navigate back through the shared router before using legacy close actions.");
            }

            uiRoot.Router.HideCurrent();
            fallbackInvoked = false;
            roomController.Close(new RoomCloseActions
            {
                LeaveRoom = delegate { fallbackInvoked = true; }
            });
            if (!fallbackInvoked)
            {
                throw new Exception("RoomScreenController should fall back to legacy close actions when no shared route history exists.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(menuRoot);
            UnityEngine.Object.DestroyImmediate(roomRoot);
            if (AppUiRoot.Current != null)
            {
                UnityEngine.Object.DestroyImmediate(AppUiRoot.Current.gameObject);
            }
        }
    }

    private static void VerifyRoomControllerRebindSynchronization()
    {
        if (AppUiRoot.HasCurrent)
        {
            UnityEngine.Object.DestroyImmediate(AppUiRoot.Current.gameObject);
        }

        AppUiRoot uiRoot = AppUiRoot.EnsureInstance();
        GameObject firstRoot = new GameObject("Task5.RoomRoot.First");
        GameObject secondRoot = new GameObject("Task5.RoomRoot.Second");
        int secondShowCount = 0;
        int secondHideCount = 0;

        try
        {
            RoomScreenController roomController = new RoomScreenController(new RoomFlowService(new TestPaths("/task5-rebind")));
            roomController.Bind(firstRoot, null, delegate { firstRoot.SetActive(true); }, delegate { firstRoot.SetActive(false); });
            roomController.SynchronizeLegacyShown();
            if (uiRoot.Router.CurrentRouteKey != RoomScreenController.Route || !firstRoot.activeSelf)
            {
                throw new Exception("Initial room route synchronization did not activate the bound root.");
            }

            roomController.Bind(
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

            roomController.SynchronizeLegacyShown();
            if (uiRoot.Router.CurrentRouteKey != RoomScreenController.Route || !secondRoot.activeSelf)
            {
                throw new Exception("Rebinding the room controller did not move legacy shown state to the replacement root.");
            }

            secondRoot.SetActive(false);
            roomController.SynchronizeLegacyHidden();
            if (uiRoot.Router.CurrentRouteKey == RoomScreenController.Route || secondRoot.activeSelf)
            {
                throw new Exception("RoomScreenController did not clear the replacement route during legacy hide synchronization.");
            }

            uiRoot.Navigate(roomController.RouteKey);
            if (secondShowCount != 1 || !secondRoot.activeSelf)
            {
                throw new Exception("RoomScreenController navigation did not target the replacement root after rebinding.");
            }

            uiRoot.Router.HideCurrent();
            if (secondHideCount != 1 || secondRoot.activeSelf)
            {
                throw new Exception("RoomScreenController hide did not target the replacement root after rebinding.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(firstRoot);
            UnityEngine.Object.DestroyImmediate(secondRoot);
            if (AppUiRoot.Current != null)
            {
                UnityEngine.Object.DestroyImmediate(AppUiRoot.Current.gameObject);
            }
        }
    }

    private static void VerifyAiEntryActions()
    {
        RoomFlowService service = new RoomFlowService(new TestPaths("/task5-ai"));
        bool returnTargetSet = false;
        bool connected = false;

        service.EnterAiRoom(new RoomAiEntryActions
        {
            SetDuelReturnTarget = delegate { returnTargetSet = true; },
            ConnectToAiRoom = delegate { connected = true; }
        });

        if (!returnTargetSet || !connected)
        {
            throw new Exception("RoomFlowService should route AI entry decisions through the injected actions.");
        }
    }

    private static void VerifyAiRoomLaunchService()
    {
        AiRoomLaunchService service = new AiRoomLaunchService();
        AiRoomBotDefinition[] bots = service.ParseBots(new[]
        {
            "!Random Picker",
            "Deck='Alpha' Random=FIRE",
            "Uses a random FIRE bot.",
            "CONTROL",
            "!Fire Only",
            "Deck='Beta'",
            "A concrete FIRE bot.",
            "FIRE AGGRO",
            "!Needs Deck File",
            "Deck='Gamma'",
            "Should be filtered.",
            "SELECT_DECKFILE"
        });

        if (bots.Length != 2)
        {
            throw new Exception("AiRoomLaunchService should ignore SELECT_DECKFILE bots when parsing bot.conf.");
        }

        if (bots[0].Name != "Random Picker" || bots[0].Description != "Uses a random FIRE bot." || bots[0].Flags.Length != 1 || bots[0].Flags[0] != "CONTROL")
        {
            throw new Exception("AiRoomLaunchService did not preserve parsed bot metadata.");
        }

        string resolvedCommand = service.ResolveCommand(bots[0].Command, bots);
        if (resolvedCommand != "Deck='Beta'")
        {
            throw new Exception("AiRoomLaunchService should replace Random=FLAG commands with a matching bot command.");
        }

        string unresolvedCommand = service.ResolveCommand("Deck='Delta' Random=WATER", bots);
        if (unresolvedCommand != "Deck='Delta' Random=WATER")
        {
            throw new Exception("AiRoomLaunchService should keep the original command when Random=FLAG finds no matches.");
        }

        AiRoomLaunchPreparation unsupported = service.PrepareLaunch(
            new AiRoomLaunchRequest
            {
                Command = "Deck='Alpha'",
                LockHand = true,
                NoCheck = true,
                NoShuffle = false,
                Platform = RuntimePlatform.OSXPlayer
            });
        if (unsupported.IsPlatformSupported)
        {
            throw new Exception("AiRoomLaunchService should reject non-Windows AI launch platforms.");
        }

        AiRoomLaunchPreparation preparation = service.PrepareLaunch(
            new AiRoomLaunchRequest
            {
                Command = "Deck='Alpha'",
                LockHand = true,
                NoCheck = true,
                NoShuffle = false,
                Platform = RuntimePlatform.WindowsPlayer
            });
        if (!preparation.IsPlatformSupported)
        {
            throw new Exception("AiRoomLaunchService should allow Windows AI launch platforms.");
        }

        if (preparation.PreparedCommand != "Deck=\"Alpha\" Hand=1")
        {
            throw new Exception("AiRoomLaunchService did not preserve command quote normalization and Hand=1 composition.");
        }

        // if (preparation.ServerFileName != "AI.Server.exe"
        //     || preparation.ServerArguments != "7911 -1 5 0 F T F 8000 5 1 0 0"
        //     || preparation.BotFileName != "WindBot/WindBot.exe"
        //     || preparation.BotWorkingDirectory != "WindBot")
        // {
        //     throw new Exception("AiRoomLaunchService did not preserve AI server and bot process launch metadata.");
        // }

        // if (service.ComposeBotArguments(preparation.PreparedCommand, "7777") != "Deck=\"Alpha\" Hand=1 Port=7777")
        // {
        //     throw new Exception("AiRoomLaunchService did not preserve Port wiring in bot launch arguments.");
        // }
    }

    private static void VerifyAiFlowService()
    {
        VerifyAiFlowServiceIgnoresInvalidSelection();
        VerifyAiFlowServiceHandlesUnsupportedPlatforms();
        VerifyAiFlowServiceLaunchesProcessesAndSchedulesJoin();
        VerifyAiFlowServiceCloseBehavior();
    }

    private static void VerifyAiFlowServiceIgnoresInvalidSelection()
    {
        AiFlowService service = new AiFlowService(new AiRoomLaunchService());
        RecordingAiFlowActions actions = new RecordingAiFlowActions();
        AiRoomBotDefinition[] bots = new[]
        {
            CreateBot("Alpha", "Deck='Alpha'", "Alpha bot", "CONTROL")
        };

        AiFlowLaunchResult hiddenRoomResult = service.TryLaunch(
            new AiFlowLaunchRequest
            {
                IsRoomVisible = false,
                SelectedIndex = 0,
                Bots = bots,
                LockHand = false,
                NoCheck = false,
                NoShuffle = false,
                Platform = RuntimePlatform.WindowsPlayer,
                PlayerName = "Tester"
            },
            actions.CreateLaunchActions());
        if (hiddenRoomResult.Started || actions.StartedProcesses.Count != 0 || actions.Messages.Count != 0)
        {
            throw new Exception("AiFlowService should ignore launch requests when the AI room is hidden.");
        }

        AiFlowLaunchResult invalidSelectionResult = service.TryLaunch(
            new AiFlowLaunchRequest
            {
                IsRoomVisible = true,
                SelectedIndex = 4,
                Bots = bots,
                LockHand = false,
                NoCheck = false,
                NoShuffle = false,
                Platform = RuntimePlatform.WindowsPlayer,
                PlayerName = "Tester"
            },
            actions.CreateLaunchActions());
        if (invalidSelectionResult.Started || actions.StartedProcesses.Count != 0 || actions.StopServerCount != 0)
        {
            throw new Exception("AiFlowService should ignore launch requests when the selected bot index is invalid.");
        }
    }

    private static void VerifyAiFlowServiceHandlesUnsupportedPlatforms()
    {
        AiFlowService service = new AiFlowService(new AiRoomLaunchService());
        RecordingAiFlowActions actions = new RecordingAiFlowActions();

        AiFlowLaunchResult result = service.TryLaunch(
            new AiFlowLaunchRequest
            {
                IsRoomVisible = true,
                SelectedIndex = 0,
                Bots = new[]
                {
                    CreateBot("Alpha", "Deck='Alpha'", "Alpha bot", "CONTROL")
                },
                LockHand = true,
                NoCheck = false,
                NoShuffle = false,
                Platform = RuntimePlatform.OSXPlayer,
                PlayerName = "Tester"
            },
            actions.CreateLaunchActions());

        if (result.Started)
        {
            throw new Exception("AiFlowService should not report a started AI launch on unsupported platforms.");
        }

        if (actions.StopServerCount != 1)
        {
            throw new Exception("AiFlowService should stop the previous AI server before evaluating the new launch.");
        }

        if (actions.StartedProcesses.Count != 0)
        {
            throw new Exception("AiFlowService should not start child processes on unsupported platforms.");
        }

        if (actions.Messages.Count != 1 || actions.Messages[0] != "当前平台不支持人机对战。")
        {
            throw new Exception("AiFlowService should surface the unsupported platform message through injected callbacks.");
        }
    }

    private static void VerifyAiFlowServiceLaunchesProcessesAndSchedulesJoin()
    {
        AiFlowService service = new AiFlowService(new AiRoomLaunchService());
        RecordingAiFlowActions actions = new RecordingAiFlowActions();
        actions.QueueProcess(
            "server",
            new[]
            {
                "7911"
            });
        actions.QueueProcess(
            "bot",
            new[]
            {
                "started"
            });

        AiFlowLaunchResult result = service.TryLaunch(
            new AiFlowLaunchRequest
            {
                IsRoomVisible = true,
                SelectedIndex = 0,
                Bots = new[]
                {
                    CreateBot("Random", "Deck='Random' Random=FIRE", "Random bot", "CONTROL"),
                    CreateBot("Fire", "Deck='Fire'", "Fire bot", "FIRE")
                },
                LockHand = true,
                NoCheck = true,
                NoShuffle = false,
                Platform = RuntimePlatform.WindowsPlayer,
                PlayerName = "FlowTester"
            },
            actions.CreateLaunchActions());

        if (!result.Started || result.ServerProcess == null || result.BotProcess == null)
        {
            throw new Exception("AiFlowService should return the launched child process handles after a successful AI start.");
        }

        if (actions.StopServerCount != 1)
        {
            throw new Exception("AiFlowService should stop the prior server before starting a new AI session.");
        }

        if (actions.StartedProcesses.Count != 2)
        {
            throw new Exception("AiFlowService should start both the AI server and the bot process.");
        }

        if (actions.StartedProcesses[0].FileName != "AI.Server.exe"
            || actions.StartedProcesses[0].Arguments != "7911 -1 5 0 F T F 8000 5 1 0 0"
            || actions.StartedProcesses[0].WorkingDirectory != null)
        {
            throw new Exception("AiFlowService should preserve AI server startup metadata when orchestrating the launch.");
        }

        if (actions.StartedProcesses[1].FileName != "WindBot/WindBot.exe"
            || actions.StartedProcesses[1].Arguments != "Deck=\"Fire\" Hand=1 Port=7911"
            || actions.StartedProcesses[1].WorkingDirectory != "WindBot")
        {
            throw new Exception("AiFlowService should preserve bot startup metadata and resolved launch arguments.");
        }

        if (actions.TrackedProcesses.Count != 2 || actions.TrackedProcesses[0] != result.ServerProcess || actions.TrackedProcesses[1] != result.BotProcess)
        {
            throw new Exception("AiFlowService should track both launched child processes after startup.");
        }

        if (actions.SetReturnTargetCount != 1)
        {
            throw new Exception("AiFlowService should set the duel return target through the injected room entry actions.");
        }

        if (actions.RunAsyncCount != 1 || actions.Delays.Count != 1 || actions.Delays[0] != 500)
        {
            throw new Exception("AiFlowService should preserve the delayed AI join timing.");
        }

        if (actions.JoinRequests.Count != 1
            || actions.JoinRequests[0].Host != "127.0.0.1"
            || actions.JoinRequests[0].PlayerName != "FlowTester"
            || actions.JoinRequests[0].Port != "7911"
            || actions.JoinRequests[0].Password != string.Empty
            || actions.JoinRequests[0].Version != string.Empty)
        {
            throw new Exception("AiFlowService should preserve the TCP join request that enters the AI room.");
        }

        if (actions.Messages.Count != 1 || actions.Messages[0] != InterString.Get("您在AI模式下遇到的BUG也极有可能会在联机的时候出现，所以请务必向我们报告。"))
        {
            throw new Exception("AiFlowService should surface the existing AI mode caution message after launch.");
        }
    }

    private static void VerifyAiFlowServiceCloseBehavior()
    {
        AiFlowService service = new AiFlowService(new AiRoomLaunchService());

        RecordingAiCloseActions exitActions = new RecordingAiCloseActions();
        service.Close(
            new AiFlowCloseRequest
            {
                ExitOnReturn = true
            },
            exitActions.CreateCloseActions());
        if (exitActions.StopServerCount != 1 || exitActions.ExitCount != 1 || exitActions.ReturnToMenuCount != 0)
        {
            throw new Exception("AiFlowService should stop the server and exit the app when exit-on-return is enabled.");
        }

        RecordingAiCloseActions returnActions = new RecordingAiCloseActions();
        service.Close(
            new AiFlowCloseRequest
            {
                ExitOnReturn = false
            },
            returnActions.CreateCloseActions());
        if (returnActions.StopServerCount != 1 || returnActions.ExitCount != 0 || returnActions.ReturnToMenuCount != 1)
        {
            throw new Exception("AiFlowService should stop the server and return to the menu when exit-on-return is disabled.");
        }
    }

    private static AiRoomBotDefinition CreateBot(string name, string command, string description, params string[] flags)
    {
        return new AiRoomBotDefinition
        {
            Name = name,
            Command = command,
            Description = description,
            Flags = flags
        };
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

    private sealed class RecordingRoomActions
    {
        public string ChatMessage;
        public string StoredDeckName;
        public string UpdatedDeckPath;
        public int ReadyCount;
        public int NotReadyCount;
        public int MoveToDuelistCount;
        public int MoveToObserverCount;
        public int StartCount;
        public int KickedSeat = -1;
        public int CloseCount;

        public RoomInteractionActions CreateInteractionActions()
        {
            return new RoomInteractionActions
            {
                SendChat = delegate(string message) { ChatMessage = message; },
                SetDeckInUse = delegate(string deckName) { StoredDeckName = deckName; },
                UpdateDeck = delegate(string deckPath) { UpdatedDeckPath = deckPath; },
                SendReady = delegate { ReadyCount++; },
                SendNotReady = delegate { NotReadyCount++; },
                MoveToDuelist = delegate { MoveToDuelistCount++; },
                MoveToObserver = delegate { MoveToObserverCount++; },
                StartDuel = delegate { StartCount++; },
                KickPlayer = delegate(int seat) { KickedSeat = seat; }
            };
        }
    }

    private sealed class RecordingAiFlowActions
    {
        private readonly System.Collections.Generic.Queue<AiRoomProcessHandle> queuedProcesses = new System.Collections.Generic.Queue<AiRoomProcessHandle>();

        public readonly System.Collections.Generic.List<AiRoomProcessStartRequest> StartedProcesses = new System.Collections.Generic.List<AiRoomProcessStartRequest>();
        public readonly System.Collections.Generic.List<AiRoomProcessHandle> TrackedProcesses = new System.Collections.Generic.List<AiRoomProcessHandle>();
        public readonly System.Collections.Generic.List<string> Messages = new System.Collections.Generic.List<string>();
        public readonly System.Collections.Generic.List<int> Delays = new System.Collections.Generic.List<int>();
        public readonly System.Collections.Generic.List<AiFlowJoinRequest> JoinRequests = new System.Collections.Generic.List<AiFlowJoinRequest>();
        public int StopServerCount;
        public int SetReturnTargetCount;
        public int RunAsyncCount;

        public void QueueProcess(string id, string[] outputLines)
        {
            int lineIndex = 0;
            queuedProcesses.Enqueue(new AiRoomProcessHandle
            {
                Id = id,
                HasExited = delegate { return false; },
                Kill = delegate { },
                ReadOutputLine = delegate
                {
                    if (outputLines == null || lineIndex >= outputLines.Length)
                    {
                        return null;
                    }

                    return outputLines[lineIndex++];
                }
            });
        }

        public AiFlowLaunchActions CreateLaunchActions()
        {
            return new AiFlowLaunchActions
            {
                StopServer = delegate { StopServerCount++; },
                StartProcess = delegate(AiRoomProcessStartRequest request)
                {
                    StartedProcesses.Add(request);
                    return queuedProcesses.Dequeue();
                },
                TrackProcess = delegate(AiRoomProcessHandle process) { TrackedProcesses.Add(process); },
                ShowMessage = delegate(string message) { Messages.Add(message); },
                SetDuelReturnTarget = delegate { SetReturnTargetCount++; },
                RunAsync = delegate(Action action)
                {
                    RunAsyncCount++;
                    if (action != null)
                    {
                        action();
                    }
                },
                Delay = delegate(int milliseconds) { Delays.Add(milliseconds); },
                JoinAiRoom = delegate(AiFlowJoinRequest request) { JoinRequests.Add(request); }
            };
        }
    }

    private sealed class RecordingAiCloseActions
    {
        public int StopServerCount;
        public int ExitCount;
        public int ReturnToMenuCount;

        public AiFlowCloseActions CreateCloseActions()
        {
            return new AiFlowCloseActions
            {
                StopServer = delegate { StopServerCount++; },
                ExitApplication = delegate { ExitCount++; },
                ReturnToMenu = delegate { ReturnToMenuCount++; }
            };
        }
    }

    private sealed class TestScreenController : IUiScreenController
    {
        private readonly GameObject root;

        public TestScreenController(string routeKey, GameObject root)
        {
            RouteKey = routeKey;
            this.root = root;
        }

        public string RouteKey { get; private set; }

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

using System;
using System.Collections.Generic;
using System.IO;
using App.Core;
using App.Features.AI.Services;
using App.Features.Puzzle.Services;
using App.UI.Common;
using App.UI.Common.Navigation;
using App.UI.Screens.AI;
using App.UI.Screens.Puzzle;
using UnityEditor;
using UnityEngine;

public static class Task6PuzzleAiBatchTest
{
    public static void Run()
    {
        int exitCode = 0;

        try
        {
            VerifyPuzzleFlowListingSelectionAndClose();
            VerifyPuzzleControllerCloseAndRebindSynchronization();
            VerifyAiControllerFlowDelegation();
            VerifyAiControllerRebindSynchronization();
            Debug.Log("Task6PuzzleAiBatchTest OK");
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

    private static void VerifyPuzzleFlowListingSelectionAndClose()
    {
        PuzzleFlowService service = new PuzzleFlowService(
            new TestPaths("/task6"),
            new PuzzleFlowCallbacks
            {
                GetPuzzleFiles = delegate
                {
                    return new[]
                    {
                        new FileInfo("/task6/puzzle/zeta.lua"),
                        new FileInfo("/task6/puzzle/notes.txt"),
                        new FileInfo("/task6/puzzle/alpha.lua")
                    };
                }
            });

        PuzzleListState listState = service.LoadPuzzleList(UIHelper.CompareName);
        if (listState.DisplayNames.Count != 2 || listState.DisplayNames[0] != "alpha" || listState.DisplayNames[1] != "zeta")
        {
            throw new Exception("PuzzleFlowService should list only .lua puzzle names and preserve the existing name sort behavior.");
        }

        PuzzleSelectionResult firstSelection = service.HandleSelection(true, "previous", "alpha");
        if (firstSelection.ShouldLaunch || firstSelection.SelectedName != "alpha")
        {
            throw new Exception("PuzzleFlowService should update selection without launching on the first click.");
        }

        PuzzleSelectionResult secondSelection = service.HandleSelection(true, "alpha", "alpha");
        if (!secondSelection.ShouldLaunch || secondSelection.PuzzlePath != "/task6/puzzle/alpha.lua")
        {
            throw new Exception("PuzzleFlowService should request launch only when the same visible puzzle is selected twice.");
        }

        PuzzleSelectionResult hiddenSelection = service.HandleSelection(false, "alpha", "alpha");
        if (hiddenSelection.ShouldLaunch)
        {
            throw new Exception("PuzzleFlowService should not launch puzzles while the legacy screen is hidden.");
        }

        bool showedMenu = false;
        bool exited = false;
        service.Close(new PuzzleCloseActions
        {
            ExitOnReturn = false,
            ShowMenu = delegate { showedMenu = true; },
            ExitApplication = delegate { exited = true; }
        });
        if (!showedMenu || exited)
        {
            throw new Exception("PuzzleFlowService should return to the menu when exit-on-return is disabled.");
        }

        showedMenu = false;
        exited = false;
        service.Close(new PuzzleCloseActions
        {
            ExitOnReturn = true,
            ShowMenu = delegate { showedMenu = true; },
            ExitApplication = delegate { exited = true; }
        });
        if (!exited || showedMenu)
        {
            throw new Exception("PuzzleFlowService should exit the app when exit-on-return is enabled.");
        }
    }

    private static void VerifyPuzzleControllerCloseAndRebindSynchronization()
    {
        if (AppUiRoot.HasCurrent)
        {
            UnityEngine.Object.DestroyImmediate(AppUiRoot.Current.gameObject);
        }

        AppUiRoot uiRoot = AppUiRoot.EnsureInstance();
        GameObject menuRoot = new GameObject("Task6.MenuRoot");
        GameObject firstRoot = new GameObject("Task6.PuzzleRoot.First");
        GameObject secondRoot = new GameObject("Task6.PuzzleRoot.Second");
        int secondShowCount = 0;
        int secondHideCount = 0;

        try
        {
            TestScreenController menuController = new TestScreenController("menu.main", menuRoot);
            uiRoot.RegisterScreen(menuController);

            PuzzleScreenController puzzleController = new PuzzleScreenController(new PuzzleFlowService(new TestPaths("/task6-ui"), new PuzzleFlowCallbacks()));
            puzzleController.Bind(firstRoot, null, delegate { firstRoot.SetActive(true); }, delegate { firstRoot.SetActive(false); });

            puzzleController.SynchronizeLegacyShown();
            if (uiRoot.Router.CurrentRouteKey != PuzzleScreenController.Route || !firstRoot.activeSelf)
            {
                throw new Exception("PuzzleScreenController should register and activate the shared puzzle route during legacy show synchronization.");
            }

            uiRoot.Navigate(menuController.RouteKey);
            uiRoot.Navigate(puzzleController.RouteKey);

            bool fallbackInvoked = false;
            puzzleController.Close(new PuzzleCloseActions
            {
                ShowMenu = delegate { fallbackInvoked = true; }
            });
            if (fallbackInvoked || uiRoot.Router.CurrentRouteKey != menuController.RouteKey)
            {
                throw new Exception("PuzzleScreenController should navigate back through the shared router before using legacy close actions.");
            }

            puzzleController.Bind(
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

            puzzleController.SynchronizeLegacyShown();
            if (uiRoot.Router.CurrentRouteKey != PuzzleScreenController.Route || !secondRoot.activeSelf)
            {
                throw new Exception("PuzzleScreenController should move legacy shown state onto the rebound root.");
            }

            secondRoot.SetActive(false);
            puzzleController.SynchronizeLegacyHidden();
            if (uiRoot.Router.CurrentRouteKey == PuzzleScreenController.Route || secondRoot.activeSelf)
            {
                throw new Exception("PuzzleScreenController should clear the shared route when the rebound legacy screen hides.");
            }

            uiRoot.Navigate(puzzleController.RouteKey);
            if (secondShowCount != 1 || !secondRoot.activeSelf)
            {
                throw new Exception("PuzzleScreenController navigation should target the rebound root.");
            }

            uiRoot.Router.HideCurrent();
            if (secondHideCount != 1 || secondRoot.activeSelf)
            {
                throw new Exception("PuzzleScreenController hide should target the rebound root.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(menuRoot);
            UnityEngine.Object.DestroyImmediate(firstRoot);
            UnityEngine.Object.DestroyImmediate(secondRoot);
            if (AppUiRoot.Current != null)
            {
                UnityEngine.Object.DestroyImmediate(AppUiRoot.Current.gameObject);
            }
        }
    }

    private static void VerifyAiControllerFlowDelegation()
    {
        ResetUiRoot();
        AppUiRoot uiRoot = AppUiRoot.EnsureInstance();
        GameObject menuRoot = new GameObject("Task6.MenuRoot.AI.Close");
        GameObject aiRoot = new GameObject("Task6.AiRoot.Close");

        try
        {
            TestScreenController menuController = new TestScreenController("menu.main", menuRoot);
            uiRoot.RegisterScreen(menuController);

            AiScreenController controller = new AiScreenController(new AiFlowService(new AiRoomLaunchService()));
            controller.Bind(aiRoot, null, delegate { aiRoot.SetActive(true); }, delegate { aiRoot.SetActive(false); });
        RecordingAiFlowActions launchActions = new RecordingAiFlowActions();
        launchActions.QueueProcess("server", new[] { "7911" });
        launchActions.QueueProcess("bot", new[] { "started" });

            AiFlowLaunchResult result = controller.TryLaunch(
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
                    PlayerName = "Task6"
                },
                launchActions.CreateLaunchActions());

            if (!result.Started || launchActions.StartedProcesses.Count != 2 || launchActions.JoinRequests.Count != 1)
            {
                throw new Exception("AiScreenController should delegate AI launch orchestration to AiFlowService without changing the existing launch behavior.");
            }

            controller.SynchronizeLegacyShown();
            uiRoot.Navigate(menuController.RouteKey);
            uiRoot.Navigate(controller.RouteKey);

            RecordingAiCloseActions closeActions = new RecordingAiCloseActions();
            controller.Close(
                new AiFlowCloseRequest
                {
                    ExitOnReturn = false
                },
                closeActions.CreateCloseActions());
            if (closeActions.StopServerCount != 0 || closeActions.ReturnToMenuCount != 0 || closeActions.ExitCount != 0 || uiRoot.Router.CurrentRouteKey != menuController.RouteKey)
            {
                throw new Exception("AiScreenController should navigate back through the shared router before using legacy close actions.");
            }

            uiRoot.Router.HideCurrent();
            closeActions = new RecordingAiCloseActions();
            controller.Close(
                new AiFlowCloseRequest
                {
                    ExitOnReturn = false
                },
                closeActions.CreateCloseActions());
            if (closeActions.StopServerCount != 1 || closeActions.ReturnToMenuCount != 1 || closeActions.ExitCount != 0)
            {
                throw new Exception("AiScreenController should delegate close behavior to AiFlowService when no shared route history exists.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(menuRoot);
            UnityEngine.Object.DestroyImmediate(aiRoot);
            ResetUiRoot();
        }
    }

    private static void VerifyAiControllerRebindSynchronization()
    {
        if (AppUiRoot.HasCurrent)
        {
            UnityEngine.Object.DestroyImmediate(AppUiRoot.Current.gameObject);
        }

        AppUiRoot uiRoot = AppUiRoot.EnsureInstance();
        GameObject firstRoot = new GameObject("Task6.AiRoot.First");
        GameObject secondRoot = new GameObject("Task6.AiRoot.Second");
        int secondShowCount = 0;
        int secondHideCount = 0;

        try
        {
            AiScreenController controller = new AiScreenController(new AiFlowService(new AiRoomLaunchService()));
            controller.Bind(firstRoot, null, delegate { firstRoot.SetActive(true); }, delegate { firstRoot.SetActive(false); });

            controller.SynchronizeLegacyShown();
            if (uiRoot.Router.CurrentRouteKey != AiScreenController.Route || !firstRoot.activeSelf)
            {
                throw new Exception("AiScreenController should register and activate the shared AI route during legacy show synchronization.");
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
            if (uiRoot.Router.CurrentRouteKey != AiScreenController.Route || !secondRoot.activeSelf)
            {
                throw new Exception("AiScreenController should move legacy shown state onto the rebound root.");
            }

            secondRoot.SetActive(false);
            controller.SynchronizeLegacyHidden();
            if (uiRoot.Router.CurrentRouteKey == AiScreenController.Route || secondRoot.activeSelf)
            {
                throw new Exception("AiScreenController should clear the shared route when the rebound legacy screen hides.");
            }

            uiRoot.Navigate(controller.RouteKey);
            if (secondShowCount != 1 || !secondRoot.activeSelf)
            {
                throw new Exception("AiScreenController navigation should target the rebound root.");
            }

            uiRoot.Router.HideCurrent();
            if (secondHideCount != 1 || secondRoot.activeSelf)
            {
                throw new Exception("AiScreenController hide should target the rebound root.");
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

    private sealed class RecordingAiFlowActions
    {
        private readonly Queue<AiRoomProcessHandle> queuedProcesses = new Queue<AiRoomProcessHandle>();

        public readonly List<AiRoomProcessStartRequest> StartedProcesses = new List<AiRoomProcessStartRequest>();
        public readonly List<AiFlowJoinRequest> JoinRequests = new List<AiFlowJoinRequest>();
        public int StopServerCount;

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
                TrackProcess = delegate(AiRoomProcessHandle process) { },
                ShowMessage = delegate(string message) { },
                SetDuelReturnTarget = delegate { },
                RunAsync = delegate(Action action)
                {
                    if (action != null)
                    {
                        action();
                    }
                },
                Delay = delegate(int milliseconds) { },
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

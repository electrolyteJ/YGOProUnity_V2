using System;
using App.Features.Duel.Services;
using App.UI.Common;
using App.UI.Common.Navigation;
using App.UI.Screens.Duel;
using UnityEditor;
using UnityEngine;

public static class Task5DuelBatchTest
{
    public static void Run()
    {
        int exitCode = 0;

        try
        {
            VerifyCloseDecisionResolution();
            VerifySessionModeResolution();
            VerifySessionConditionResolution();
            VerifyResultConfirmationResolution();
            VerifyResultConfirmationBehavior();
            VerifyControllerCloseBehavior();
            VerifyLiveSessionExitBehavior();
            VerifyRouteRegistration();
            Debug.Log("Task5DuelBatchTest OK");
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

    private static void VerifyCloseDecisionResolution()
    {
        DuelFlowService service = new DuelFlowService();

        if (service.ResolveCloseResult(true, true, false) != DuelCloseResult.ExitApplication)
        {
            throw new Exception("DuelFlowService should preserve exit-on-return precedence for non-deck-manager returns.");
        }

        if (service.ResolveCloseResult(true, true, true) != DuelCloseResult.AssignedReturnTarget)
        {
            throw new Exception("DuelFlowService should keep deck-manager returns inside the app.");
        }

        if (service.ResolveCloseResult(false, true, false) != DuelCloseResult.AssignedReturnTarget)
        {
            throw new Exception("DuelFlowService should return to the assigned screen when one exists.");
        }

        if (service.ResolveCloseResult(false, false, false) != DuelCloseResult.ServerSelect)
        {
            throw new Exception("DuelFlowService should fall back to server select when no return screen exists.");
        }
    }

    private static void VerifySessionModeResolution()
    {
        DuelFlowService service = new DuelFlowService();

        if (service.ResolveSessionMode(false, false) != DuelSessionMode.LiveDuel)
        {
            throw new Exception("DuelFlowService should resolve live duel mode for non-observer sessions.");
        }

        if (service.ResolveSessionMode(false, true) != DuelSessionMode.LiveWatch)
        {
            throw new Exception("DuelFlowService should resolve watch mode for observer sessions.");
        }

        if (service.ResolveSessionMode(true, false) != DuelSessionMode.ReplayRecord)
        {
            throw new Exception("DuelFlowService should resolve replay-record mode ahead of live-session state.");
        }

        DuelSessionMode nextMode;
        if (!service.TryResolveSessionTransition(DuelSessionMode.LiveDuel, false, true, out nextMode)
            || nextMode != DuelSessionMode.LiveWatch)
        {
            throw new Exception("DuelFlowService should request a transition when the resolved live-session mode changes.");
        }

        if (service.TryResolveSessionTransition(DuelSessionMode.ReplayRecord, true, false, out nextMode))
        {
            throw new Exception("DuelFlowService should keep replay-record mode stable when it already matches.");
        }
    }

    private static void VerifySessionConditionResolution()
    {
        DuelFlowService service = new DuelFlowService();
        Ocgcore.Condition nextCondition;

        if (service.TryResolveSessionCondition(
                Ocgcore.Condition.duel,
                Ocgcore.Condition.duel,
                Ocgcore.Condition.watch,
                Ocgcore.Condition.record,
                false,
                out nextCondition)
            || nextCondition != Ocgcore.Condition.duel)
        {
            throw new Exception("DuelFlowService should keep live duel conditions stable for active players.");
        }

        if (!service.TryResolveSessionCondition(
                Ocgcore.Condition.duel,
                Ocgcore.Condition.duel,
                Ocgcore.Condition.watch,
                Ocgcore.Condition.record,
                true,
                out nextCondition)
            || nextCondition != Ocgcore.Condition.watch)
        {
            throw new Exception("DuelFlowService should switch live duel conditions into watch mode for observers.");
        }

        if (!service.TryResolveSessionCondition(
                Ocgcore.Condition.watch,
                Ocgcore.Condition.duel,
                Ocgcore.Condition.watch,
                Ocgcore.Condition.record,
                false,
                out nextCondition)
            || nextCondition != Ocgcore.Condition.duel)
        {
            throw new Exception("DuelFlowService should switch watch conditions back to duel mode for active players.");
        }

        if (service.TryResolveSessionCondition(
                Ocgcore.Condition.record,
                Ocgcore.Condition.duel,
                Ocgcore.Condition.watch,
                Ocgcore.Condition.record,
                true,
                out nextCondition)
            || nextCondition != Ocgcore.Condition.record)
        {
            throw new Exception("DuelFlowService should preserve record conditions when a replay is already active.");
        }
    }

    private static void VerifyResultConfirmationResolution()
    {
        DuelFlowService service = new DuelFlowService();

        if (service.ResolveDuelResultConfirmation(true, false, true, false, true) != DuelResultConfirmationResult.ExitDuel)
        {
            throw new Exception("DuelFlowService should exit immediately after a duel-end confirmation.");
        }

        if (service.ResolveDuelResultConfirmation(false, true, true, false, true) != DuelResultConfirmationResult.ExitDuel)
        {
            throw new Exception("DuelFlowService should exit immediately after a surrender confirmation.");
        }

        if (service.ResolveDuelResultConfirmation(false, false, false, false, true) != DuelResultConfirmationResult.ExitDuel)
        {
            throw new Exception("DuelFlowService should exit when the duel session is no longer connected.");
        }

        if (service.ResolveDuelResultConfirmation(false, false, true, true, true) != DuelResultConfirmationResult.EnterSideDeck)
        {
            throw new Exception("DuelFlowService should prioritize the side-deck handoff when it is available.");
        }

        if (service.ResolveDuelResultConfirmation(false, false, true, false, false) != DuelResultConfirmationResult.HideCalculator)
        {
            throw new Exception("DuelFlowService should hide the result calculator for non-duel sessions.");
        }

        if (service.ResolveDuelResultConfirmation(false, false, true, false, true) != DuelResultConfirmationResult.PromptSurrender)
        {
            throw new Exception("DuelFlowService should prompt for surrender when the duel remains active.");
        }
    }

    private static void VerifyResultConfirmationBehavior()
    {
        DuelFlowService service = new DuelFlowService();
        int step = 0;
        int resetStep = 0;
        int sideDeckStep = 0;
        bool exitCalled = false;
        bool hideCalled = false;
        bool promptCalled = false;

        service.ConfirmDuelResult(new DuelResultConfirmationActions
        {
            NeedSide = true,
            HasConnectedSession = true,
            IsDuelCondition = true,
            ResetReconnectState = delegate { resetStep = ++step; },
            ExitDuel = delegate { exitCalled = true; },
            EnterSideDeck = delegate { sideDeckStep = ++step; },
            HideCalculator = delegate { hideCalled = true; },
            PromptSurrender = delegate { promptCalled = true; }
        });

        if (resetStep != 1 || sideDeckStep != 2)
        {
            throw new Exception("DuelFlowService should reset reconnect state before running the resolved result-confirmation action.");
        }

        if (exitCalled || hideCalled || promptCalled)
        {
            throw new Exception("DuelFlowService should invoke only the selected result-confirmation branch.");
        }
    }

    private static void VerifyControllerCloseBehavior()
    {
        if (AppUiRoot.HasCurrent)
        {
            UnityEngine.Object.DestroyImmediate(AppUiRoot.Current.gameObject);
        }

        AppUiRoot uiRoot = AppUiRoot.EnsureInstance();
        GameObject menuRoot = new GameObject("Task5.MenuRoot");
        GameObject duelRoot = new GameObject("Task5.DuelRoot");

        try
        {
            TestScreenController menuController = new TestScreenController("menu.main", menuRoot);
            uiRoot.RegisterScreen(menuController);

            DuelScreenController duelController = new DuelScreenController(new DuelFlowService());
            duelController.Bind(
                duelRoot,
                null,
                delegate { duelRoot.SetActive(true); },
                delegate { duelRoot.SetActive(false); });

            uiRoot.Navigate(menuController.RouteKey);
            uiRoot.Navigate(duelController.RouteKey);

            bool saved = false;
            bool assignedReturnShown = false;
            duelController.Close(new DuelCloseActions
            {
                HasAssignedReturnTarget = true,
                SaveRecord = delegate { saved = true; },
                ShowAssignedReturnTarget = delegate { assignedReturnShown = true; }
            });

            if (!saved || assignedReturnShown || uiRoot.Router.CurrentRouteKey != menuController.RouteKey)
            {
                throw new Exception("DuelScreenController should save first and navigate back through the shared router when possible.");
            }

            uiRoot.Navigate(menuController.RouteKey);
            uiRoot.Navigate(duelController.RouteKey);

            saved = false;
            bool exited = false;
            duelController.Close(new DuelCloseActions
            {
                ExitOnReturn = true,
                HasAssignedReturnTarget = true,
                SaveRecord = delegate { saved = true; },
                ExitApplication = delegate { exited = true; }
            });

            if (!saved || !exited)
            {
                throw new Exception("DuelScreenController should honor explicit exit requests even when shared router history exists.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(menuRoot);
            UnityEngine.Object.DestroyImmediate(duelRoot);
            if (AppUiRoot.Current != null)
            {
                UnityEngine.Object.DestroyImmediate(AppUiRoot.Current.gameObject);
            }
        }
    }

    private static void VerifyLiveSessionExitBehavior()
    {
        if (AppUiRoot.HasCurrent)
        {
            UnityEngine.Object.DestroyImmediate(AppUiRoot.Current.gameObject);
        }

        DuelScreenController duelController = new DuelScreenController(new DuelFlowService());
        bool disconnected = false;
        bool serverStopped = false;
        bool saved = false;
        bool returned = false;

        duelController.ExitLiveSession(new DuelLiveSessionExitActions
        {
            HasAssignedReturnTarget = true,
            DisconnectSession = delegate { disconnected = true; },
            StopAiServer = delegate { serverStopped = true; },
            SaveRecord = delegate { saved = true; },
            ShowAssignedReturnTarget = delegate { returned = true; }
        });

        if (!disconnected || !serverStopped || !saved || !returned)
        {
            throw new Exception("DuelScreenController should run live-session cleanup before applying return actions.");
        }
    }

    private static void VerifyRouteRegistration()
    {
        if (AppUiRoot.HasCurrent)
        {
            UnityEngine.Object.DestroyImmediate(AppUiRoot.Current.gameObject);
        }

        AppUiRoot uiRoot = AppUiRoot.EnsureInstance();
        GameObject duelRoot = new GameObject("Task5.RouteRoot");
        bool hidden = false;

        try
        {
            DuelScreenController duelController = new DuelScreenController(new DuelFlowService());
            duelController.Bind(
                duelRoot,
                null,
                delegate { duelRoot.SetActive(true); },
                delegate
                {
                    hidden = true;
                    duelRoot.SetActive(false);
                });

            duelController.SynchronizeLegacyShown();
            if (uiRoot.Router.CurrentRouteKey != DuelScreenController.Route || !duelRoot.activeSelf)
            {
                throw new Exception("DuelScreenController should register itself with the shared UI root.");
            }

            duelController.SynchronizeLegacyHidden();
            if (hidden || uiRoot.Router.CurrentRouteKey != string.Empty)
            {
                throw new Exception("DuelScreenController should hide its route silently without replaying the legacy hide callback.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(duelRoot);
            if (AppUiRoot.Current != null)
            {
                UnityEngine.Object.DestroyImmediate(AppUiRoot.Current.gameObject);
            }
        }
    }

    private sealed class TestScreenController : IUiScreenController
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

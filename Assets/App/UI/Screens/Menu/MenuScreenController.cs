using System;
using App.Features.Menu.Services;
using App.UI.Common;
using App.UI.Common.Navigation;
using App.UI.Screens.AI;
using App.UI.Screens.Deck;
using App.UI.Screens.Online;
using App.UI.Screens.Puzzle;
using App.UI.Screens.Replay;
using UnityEngine;

namespace App.UI.Screens.Menu
{
    public sealed class MenuScreenController : IUiScreenController
    {
        public const string Route = "menu.main";

        private GameObject screenRoot;
        private MenuFlowService flowService;
        private AppUiRoot uiRoot;
        private Action showCallback;
        private Action hideCallback;
        private bool suppressNextShowCallback;
        private bool suppressNextHideCallback;

        public MenuScreenController()
        {
        }

        public MenuScreenController(MenuFlowService service)
        {
            flowService = service;
        }

        public string RouteKey
        {
            get { return Route; }
        }

        public bool IsVisible
        {
            get { return screenRoot != null && screenRoot.activeInHierarchy; }
        }

        public void Bind(GameObject root, MenuFlowService service, Action onShow, Action onHide)
        {
            screenRoot = root;
            flowService = service;
            showCallback = onShow;
            hideCallback = onHide;
            EnsureRegistered();
        }

        public void Navigate(MenuDestination destination, MenuNavigationActions actions)
        {
            if (TryNavigateWithSharedHost(destination))
            {
                return;
            }

            EnsureFlowService().Navigate(destination, actions);
        }

        public bool TryHandleShellCommand(string shellContents, MenuShellExecutionActions actions)
        {
            return EnsureFlowService().TryExecuteShellCommand(shellContents, actions);
        }

        public void SynchronizeLegacyShown()
        {
            EnsureRegistered();
            SetRootActive(true);
            NavigateSilentlyToSelf();
        }

        public void SynchronizeLegacyHidden()
        {
            HideRouteSilentlyIfCurrent();
        }

        public void RestoreAsCurrentRoute()
        {
            if (screenRoot == null || !screenRoot.activeInHierarchy)
            {
                return;
            }

            EnsureRegistered();
            NavigateSilentlyToSelf();
        }

        public void Show(object parameter)
        {
            if (suppressNextShowCallback)
            {
                suppressNextShowCallback = false;
                return;
            }

            if (showCallback != null)
            {
                showCallback();
            }
            else
            {
                SetRootActive(true);
            }
        }

        public void Hide()
        {
            if (suppressNextHideCallback)
            {
                suppressNextHideCallback = false;
                return;
            }

            if (hideCallback != null)
            {
                hideCallback();
            }
            else
            {
                SetRootActive(false);
            }
        }

        private bool TryNavigateWithSharedHost(MenuDestination destination)
        {
            string routeKey = GetRouteKey(destination);
            if (string.IsNullOrEmpty(routeKey))
            {
                return false;
            }

            AppUiRoot root = EnsureUiRoot();
            if (root == null || root.Router == null || !root.Router.HasRoute(routeKey))
            {
                return false;
            }

            return root.Navigate(routeKey);
        }

        private static string GetRouteKey(MenuDestination destination)
        {
            switch (destination)
            {
                case MenuDestination.Settings:
                    return SettingsScreenController.Route;
                case MenuDestination.Deck:
                    return DeckScreenController.Route;
                case MenuDestination.Online:
                    return SelectServerScreenController.Route;
                case MenuDestination.Replay:
                    return ReplayScreenController.Route;
                case MenuDestination.Puzzle:
                    return PuzzleScreenController.Route;
                case MenuDestination.AI:
                    return AiScreenController.Route;
                default:
                    return string.Empty;
            }
        }

        private void EnsureRegistered()
        {
            AppUiRoot root = EnsureUiRoot();
            if (root != null)
            {
                root.RegisterScreen(this);
            }
        }

        private AppUiRoot EnsureUiRoot()
        {
            if (uiRoot == null)
            {
                uiRoot = AppUiRoot.EnsureInstance();
            }

            return uiRoot;
        }

        private void NavigateSilentlyToSelf()
        {
            AppUiRoot root = EnsureUiRoot();
            if (root == null || root.Router == null)
            {
                return;
            }

            if (string.Equals(root.Router.CurrentRouteKey, Route, StringComparison.Ordinal))
            {
                return;
            }

            suppressNextShowCallback = true;
            root.Navigate(Route);
        }

        private void HideRouteSilentlyIfCurrent()
        {
            AppUiRoot root = EnsureUiRoot();
            if (root == null || root.Router == null)
            {
                return;
            }

            if (!string.Equals(root.Router.CurrentRouteKey, Route, StringComparison.Ordinal))
            {
                return;
            }

            suppressNextHideCallback = true;
            root.Router.HideCurrent();
        }

        private void SetRootActive(bool isActive)
        {
            if (screenRoot != null)
            {
                screenRoot.SetActive(isActive);
            }
        }

        private MenuFlowService EnsureFlowService()
        {
            if (flowService == null)
            {
                flowService = new MenuFlowService();
            }

            return flowService;
        }
    }
}

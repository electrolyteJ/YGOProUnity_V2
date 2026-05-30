using System;
using App.Features.Duel.Services;
using App.UI.Common;
using App.UI.Common.Navigation;
using UnityEngine;

namespace App.UI.Screens.Duel
{
    public sealed class DuelScreenController : IUiScreenController
    {
        public const string Route = "duel.main";

        private GameObject screenRoot;
        private DuelFlowService flowService;
        private AppUiRoot uiRoot;
        private Action showCallback;
        private Action hideCallback;
        private bool suppressNextShowCallback;
        private bool suppressNextHideCallback;

        public DuelScreenController()
        {
        }

        public DuelScreenController(DuelFlowService service)
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

        public void Bind(GameObject root, DuelFlowService service, Action onShow, Action onHide)
        {
            screenRoot = root;
            if (service != null)
            {
                flowService = service;
            }
            showCallback = onShow;
            hideCallback = onHide;
            EnsureRegistered();
        }

        public void Close(DuelCloseActions actions)
        {
            DuelFlowService service = EnsureFlowService();
            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            if (actions.ExitOnReturn)
            {
                service.Close(actions);
                return;
            }

            if (TryNavigateBack())
            {
                Invoke(actions.SaveRecord);
                return;
            }

            service.Close(actions);
        }

        public void ExitLiveSession(DuelLiveSessionExitActions actions)
        {
            EnsureFlowService().ExitLiveSession(actions);
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

        private bool TryNavigateBack()
        {
            AppUiRoot root = EnsureUiRoot();
            if (root == null || !root.CanNavigateBack)
            {
                return false;
            }

            return root.NavigateBack();
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

        private DuelFlowService EnsureFlowService()
        {
            if (flowService == null)
            {
                flowService = new DuelFlowService();
            }

            return flowService;
        }

        private static void Invoke(Action action)
        {
            if (action != null)
            {
                action();
            }
        }
    }
}

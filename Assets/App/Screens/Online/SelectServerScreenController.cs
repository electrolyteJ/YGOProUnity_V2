using System;
using App.Screens.Online.Services;
using App.UI.Common;
using App.UI.Common.Navigation;
using UnityEngine;

namespace App.UI.Screens.Online
{
    public sealed class SelectServerScreenController : IUiScreenController
    {
        public const string Route = "online.main";

        private GameObject screenRoot;
        private OnlineFlowService flowService;
        private AppUiRoot uiRoot;
        private Action showCallback;
        private Action hideCallback;
        private bool suppressNextShowCallback;
        private bool suppressNextHideCallback;

        public SelectServerScreenController()
        {
        }

        public SelectServerScreenController(OnlineFlowService service)
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

        public void Bind(GameObject root, OnlineFlowService service, Action onShow, Action onHide)
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

        public OnlineHistoryState LoadHistory()
        {
            return EnsureFlowService().LoadHistory();
        }

        public OnlineHistorySelection ParseHistoryEntry(string entry)
        {
            return EnsureFlowService().ParseHistoryEntry(entry);
        }

        public OnlineJoinResult PrepareJoin(OnlineJoinRequest request)
        {
            return EnsureFlowService().PrepareJoin(request);
        }

        public void Close(OnlineCloseActions actions)
        {
            if (TryNavigateBack())
            {
                return;
            }

            EnsureFlowService().Close(actions);
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
            if (root == null || root.Router == null || !root.CanNavigateBack)
            {
                return false;
            }

            if (!string.Equals(root.Router.CurrentRouteKey, Route, StringComparison.Ordinal))
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

        private OnlineFlowService EnsureFlowService()
        {
            if (flowService == null)
            {
                flowService = new OnlineFlowService();
            }

            return flowService;
        }
    }
}

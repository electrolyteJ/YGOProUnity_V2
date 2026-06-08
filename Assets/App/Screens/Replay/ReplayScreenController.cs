using System;
using System.Collections.Generic;
using System.IO;
using App.Screens.Replay.Services;
using App.UI.Common;
using App.UI.Common.Navigation;
using UnityEngine;

namespace App.UI.Screens.Replay
{
    public sealed class ReplayScreenController : IUiScreenController
    {
        public const string Route = "replay.main";

        private GameObject screenRoot;
        private ReplayFlowService flowService;
        private AppUiRoot uiRoot;
        private Action showCallback;
        private Action hideCallback;
        private bool suppressNextShowCallback;
        private bool suppressNextHideCallback;

        public ReplayScreenController()
        {
        }

        public ReplayScreenController(ReplayFlowService service)
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

        public void Bind(GameObject root, ReplayFlowService service, Action onShow, Action onHide)
        {
            screenRoot = root;
            flowService = service;
            showCallback = onShow;
            hideCallback = onHide;
            EnsureRegistered();
        }

        public ReplayListState LoadReplayList(string sortPreferenceValue, Comparison<FileInfo> timeComparison, Comparison<FileInfo> nameComparison)
        {
            return EnsureFlowService().LoadReplayList(sortPreferenceValue, timeComparison, nameComparison);
        }

        public string ToggleSort(string currentValue)
        {
            return EnsureFlowService().ToggleSort(currentValue);
        }

        public bool IsLegacyReplayFileName(string value)
        {
            return EnsureFlowService().IsLegacyReplayFileName(value);
        }

        public string GetRenameInputValue(string selectedReplay)
        {
            return EnsureFlowService().GetRenameInputValue(selectedReplay);
        }

        public ReplayOperationResult RenameReplay(string selectedReplay, string targetName)
        {
            return EnsureFlowService().RenameReplay(selectedReplay, targetName);
        }

        public ReplayDeleteResult DeleteReplay(string selectedReplay)
        {
            return EnsureFlowService().DeleteReplay(selectedReplay);
        }

        public ReplayCleanupResult DeleteUnnamedLegacyReplays()
        {
            return EnsureFlowService().DeleteUnnamedLegacyReplays();
        }

        public ReplayExportResult ExportLegacyReplays(string selectedReplay)
        {
            return EnsureFlowService().ExportLegacyReplays(selectedReplay);
        }

        public ReplayExportResult ExportDecks(string selectedReplay, IList<string> deckContents)
        {
            return EnsureFlowService().ExportDecks(selectedReplay, deckContents);
        }

        public ReplayExportResult ExportDecksFromReplay(string selectedReplay, Func<byte[], IList<string>> buildDeckContents)
        {
            return EnsureFlowService().ExportDecksFromReplay(selectedReplay, buildDeckContents);
        }

        public bool TryGetDeckExportSource(string selectedReplay, out byte[] replayBuffer)
        {
            return EnsureFlowService().TryGetDeckExportSource(selectedReplay, out replayBuffer);
        }

        public bool TryCreateOpenRequest(string selectedReplay, bool preferLegacyReplayBuffer, out ReplayOpenRequest request)
        {
            return EnsureFlowService().TryCreateOpenRequest(selectedReplay, preferLegacyReplayBuffer, out request);
        }

        public bool LaunchReplay(string selectedReplay, bool preferLegacyReplayBuffer, ReplayLaunchActions actions)
        {
            return EnsureFlowService().LaunchReplay(selectedReplay, preferLegacyReplayBuffer, actions);
        }

        public void Close(ReplayCloseActions actions)
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

        private ReplayFlowService EnsureFlowService()
        {
            if (flowService == null)
            {
                flowService = new ReplayFlowService();
            }

            return flowService;
        }
    }
}

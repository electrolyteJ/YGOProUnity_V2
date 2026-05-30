using System;
using System.Collections.Generic;
using App.Features.Menu.Services;
using App.UI.Common;
using App.UI.Common.Navigation;
using UnityEngine;

namespace App.UI.Screens.Menu
{
    public sealed class SettingsScreenController : IUiScreenController
    {
        public const string Route = "menu.settings";

        private GameObject screenRoot;
        private SettingsFlowService flowService;
        private AppUiRoot uiRoot;
        private Action showCallback;
        private Action hideCallback;
        private bool suppressNextShowCallback;
        private bool suppressNextHideCallback;

        public SettingsScreenController()
        {
        }

        public SettingsScreenController(SettingsFlowService service)
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

        public void Bind(GameObject root, SettingsFlowService service, Action onShow, Action onHide)
        {
            screenRoot = root;
            flowService = service;
            showCallback = onShow;
            hideCallback = onHide;
            EnsureRegistered();
        }

        public SettingsViewState LoadState(
            IEnumerable<string> additionalToggleNames,
            Func<string, string, string> readConfigValue,
            Func<bool> readFullScreen,
            Func<int> readQualityLevel)
        {
            return EnsureFlowService().LoadState(additionalToggleNames, readConfigValue, readFullScreen, readQualityLevel);
        }

        public void SaveLiveState(SettingsViewState state, Action<string, string> writeConfigValue, SettingsRuntimeActions runtimeActions)
        {
            EnsureFlowService().SaveLiveState(state, writeConfigValue, runtimeActions);
        }

        public void SaveQuitState(SettingsViewState state, Action<string, string> writeConfigValue, Func<bool> readIsMaximized)
        {
            EnsureFlowService().SaveQuitState(state, writeConfigValue, readIsMaximized);
        }

        public void ApplyCloudToggle(bool enabled, SettingsRuntimeActions runtimeActions)
        {
            EnsureFlowService().ApplyCloudToggle(enabled, runtimeActions);
        }

        public void ApplyMouseToggle(bool enabled, SettingsRuntimeActions runtimeActions)
        {
            EnsureFlowService().ApplyMouseToggle(enabled, runtimeActions);
        }

        public void ApplyLongFieldToggle(bool enabled, SettingsRuntimeActions runtimeActions)
        {
            EnsureFlowService().ApplyLongFieldToggle(enabled, runtimeActions);
        }

        public void ApplyFieldSize(float sliderValue, SettingsRuntimeActions runtimeActions)
        {
            EnsureFlowService().ApplyFieldSize(sliderValue, runtimeActions);
        }

        public void ApplyVerticalSize(float sliderValue, SettingsRuntimeActions runtimeActions)
        {
            EnsureFlowService().ApplyVerticalSize(sliderValue, runtimeActions);
        }

        public void ApplyCloseUpPreset(bool enabled, Action<float> applyAlpha, Action<float> applySize)
        {
            EnsureFlowService().ApplyCloseUpPreset(enabled, applyAlpha, applySize);
        }

        public bool TryBuildResizeRequest(string screenValue, bool fullScreen, out SettingsResizeRequest request)
        {
            return EnsureFlowService().TryBuildResizeRequest(screenValue, fullScreen, out request);
        }

        public void Close(Action hideSettings)
        {
            if (TryNavigateBack())
            {
                return;
            }

            EnsureFlowService().Close(hideSettings);
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

        private SettingsFlowService EnsureFlowService()
        {
            if (flowService == null)
            {
                flowService = new SettingsFlowService();
            }

            return flowService;
        }
    }
}

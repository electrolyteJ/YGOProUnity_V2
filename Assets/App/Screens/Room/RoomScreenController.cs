using System;
using App.Screens.Room.Services;
using App.UI.Common;
using App.UI.Common.Navigation;
using UnityEngine;

namespace App.UI.Screens.Room
{
    public sealed class RoomScreenController : IUiScreenController
    {
        public const string Route = "room.main";

        private GameObject screenRoot;
        private RoomFlowService flowService;
        private AppUiRoot uiRoot;
        private Action showCallback;
        private Action hideCallback;
        private bool suppressNextShowCallback;
        private bool suppressNextHideCallback;

        public RoomScreenController()
        {
        }

        public RoomScreenController(RoomFlowService service)
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

        public void Bind(GameObject root, RoomFlowService service, Action onShow, Action onHide)
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

        public bool SubmitChat(string message, RoomInteractionActions actions)
        {
            return EnsureFlowService().SubmitChat(message, actions);
        }

        public void HandleReadyToggle(RoomSeatInteractionRequest request, RoomInteractionActions actions)
        {
            EnsureFlowService().HandleReadyToggle(request, actions);
        }

        public void SelectDeck(string deckName, RoomSeatInteractionRequest request, RoomInteractionActions actions)
        {
            EnsureFlowService().SelectDeck(deckName, request, actions);
        }

        public void HandlePrepareChanged(bool isPrepared, string selectedDeckName, RoomInteractionActions actions)
        {
            EnsureFlowService().HandlePrepareChanged(isPrepared, selectedDeckName, actions);
        }

        public void MoveToDuelist(RoomInteractionActions actions)
        {
            EnsureFlowService().MoveToDuelist(actions);
        }

        public void MoveToObserver(RoomInteractionActions actions)
        {
            EnsureFlowService().MoveToObserver(actions);
        }

        public void StartDuel(RoomInteractionActions actions)
        {
            EnsureFlowService().StartDuel(actions);
        }

        public void KickPlayer(int position, RoomInteractionActions actions)
        {
            EnsureFlowService().KickPlayer(position, actions);
        }

        public void Close(RoomCloseActions actions)
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

        private RoomFlowService EnsureFlowService()
        {
            if (flowService == null)
            {
                flowService = new RoomFlowService();
            }

            return flowService;
        }
    }
}

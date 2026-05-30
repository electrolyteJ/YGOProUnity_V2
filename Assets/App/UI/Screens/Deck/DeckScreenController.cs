using System;
using App.Features.Deck.Services;
using App.UI.Common;
using App.UI.Common.Navigation;
using UnityEngine;

namespace App.UI.Screens.Deck
{
    public sealed class DeckScreenController : IUiScreenController
    {
        public const string Route = "deck.list";

        private GameObject screenRoot;
        private DeckFlowService flowService;
        private AppUiRoot uiRoot;
        private Action showCallback;
        private Action hideCallback;
        private bool suppressNextShowCallback;
        private bool suppressNextHideCallback;

        public DeckScreenController()
        {
        }

        public DeckScreenController(DeckFlowService service)
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

        public void Bind(GameObject root, DeckFlowService service, Action onShow, Action onHide)
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

        public string[] GetDeckNames(string currentDeckName, string searchText, DeckSortMode sortMode)
        {
            return EnsureFlowService().GetDeckNames(currentDeckName, searchText, sortMode);
        }

        public string GetDeckPath(string deckName)
        {
            return EnsureFlowService().GetDeckPath(deckName);
        }

        public bool DeckExists(string deckName)
        {
            return EnsureFlowService().DeckExists(deckName);
        }

        public void CreateDeck(string deckName)
        {
            EnsureFlowService().CreateDeck(deckName);
        }

        public void DeleteDeck(string deckName)
        {
            EnsureFlowService().DeleteDeck(deckName);
        }

        public void CopyDeck(string sourceDeckName, string targetDeckName)
        {
            EnsureFlowService().CopyDeck(sourceDeckName, targetDeckName);
        }

        public void RenameDeck(string sourceDeckName, string targetDeckName)
        {
            EnsureFlowService().RenameDeck(sourceDeckName, targetDeckName);
        }

        public string GetUniqueDeckName(string baseDeckName)
        {
            return EnsureFlowService().GetUniqueDeckName(baseDeckName);
        }

        public bool TryCreateEditorLaunchRequest(string deckName, out DeckEditorLaunchRequest request)
        {
            return EnsureFlowService().TryCreateEditorLaunchRequest(deckName, out request);
        }

        public bool TryOpenEditor(string deckName, DeckEditorOpenActions actions)
        {
            return EnsureFlowService().TryOpenEditor(deckName, actions);
        }

        public void HandleReturnDialogResult(string resultValue, DeckEditorReturnActions actions)
        {
            EnsureFlowService().HandleReturnDialogResult(resultValue, actions);
        }

        public DeckSaveResult SaveDeck(DeckSaveRequest request)
        {
            return EnsureFlowService().SaveDeck(request);
        }

        public void SaveSerializedDeck(string deckName, string serializedDeck)
        {
            EnsureFlowService().SaveSerializedDeck(deckName, serializedDeck);
        }

        public void Close(DeckCloseActions actions)
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

        private DeckFlowService EnsureFlowService()
        {
            if (flowService == null)
            {
                flowService = new DeckFlowService();
            }

            return flowService;
        }
    }
}

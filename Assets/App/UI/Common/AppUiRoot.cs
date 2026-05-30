using System;
using App.UI.Common.Dialogs;
using App.UI.Common.Navigation;
using App.UI.Common.States;
using UnityEngine;

namespace App.UI.Common
{
    public sealed class AppUiRoot : MonoBehaviour
    {
        private const string RouterHostName = "ScreenRouter";
        private const string DialogHostName = "DialogHost";
        private const string BusyOverlayHostName = "BusyOverlayHost";

        private static AppUiRoot current;
        private bool initialized;

        [SerializeField] private bool keepAliveAcrossScenes = true;
        [SerializeField] private Transform screenRegistrationRoot;
        [SerializeField] private ScreenRouter router;
        [SerializeField] private DialogHost dialogHost;
        [SerializeField] private BusyOverlayHost busyOverlayHost;

        public static AppUiRoot Current
        {
            get { return current; }
        }

        public static bool HasCurrent
        {
            get { return current != null; }
        }

        public ScreenRouter Router
        {
            get { return router; }
        }

        public DialogHost Dialogs
        {
            get { return dialogHost; }
        }

        public BusyOverlayHost BusyOverlay
        {
            get { return busyOverlayHost; }
        }

        public static AppUiRoot EnsureInstance()
        {
            if (current != null)
            {
                current.EnsureInitialized();
                return current;
            }

            GameObject rootObject = new GameObject("AppUiRoot");
            AppUiRoot root = rootObject.AddComponent<AppUiRoot>();
            current = root;
            root.EnsureInitialized();
            return root;
        }

        private void Awake()
        {
            if (current != null && current != this)
            {
                Debug.LogWarning("Duplicate AppUiRoot detected. Disabling the newer instance.");
                Destroy(gameObject);
                return;
            }

            current = this;

            if (keepAliveAcrossScenes && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (current == null)
            {
                current = this;
            }

            if (initialized)
            {
                return;
            }

            EnsureHosts();
            router.RegisterFromHierarchy(ResolveScreenRegistrationRoot());
            initialized = true;
        }

        private void OnDestroy()
        {
            if (current == this)
            {
                current = null;
            }
        }

        public bool Navigate(string routeKey)
        {
            return Navigate(routeKey, null);
        }

        public bool Navigate(string routeKey, object parameter)
        {
            return router != null && router.TryNavigate(routeKey, parameter);
        }

        public bool NavigateBack()
        {
            return router != null && router.TryNavigateBack();
        }

        public bool CanNavigateBack
        {
            get { return router != null && router.CanNavigateBack; }
        }

        public bool RegisterScreen(IUiScreenController controller)
        {
            return router != null && router.Register(controller);
        }

        public bool UnregisterScreen(IUiScreenController controller)
        {
            return router != null && router.Unregister(controller);
        }

        public IDisposable ShowBusy(string message)
        {
            if (busyOverlayHost == null)
            {
                return BusyOverlayHost.NoOpScope.Instance;
            }

            return busyOverlayHost.PushBusy(message);
        }

        public void ShowError(string message)
        {
            if (busyOverlayHost != null)
            {
                busyOverlayHost.ShowError(message);
            }
        }

        public void ClearError()
        {
            if (busyOverlayHost != null)
            {
                busyOverlayHost.ClearError();
            }
        }

        public void ShowMessage(string title, string message, Action onDismissed)
        {
            if (dialogHost != null)
            {
                dialogHost.ShowMessage(title, message, onDismissed);
            }
        }

        public void ShowConfirmation(string title, string message, Action<bool> onCompleted)
        {
            if (dialogHost != null)
            {
                dialogHost.ShowConfirmation(title, message, onCompleted);
            }
        }

        private void EnsureHosts()
        {
            router = EnsureHost(router, RouterHostName);
            dialogHost = EnsureHost(dialogHost, DialogHostName);
            busyOverlayHost = EnsureHost(busyOverlayHost, BusyOverlayHostName);
        }

        private Transform ResolveScreenRegistrationRoot()
        {
            if (screenRegistrationRoot == null)
            {
                screenRegistrationRoot = transform;
            }

            return screenRegistrationRoot;
        }

        private T EnsureHost<T>(T host, string hostName) where T : Component
        {
            if (host != null)
            {
                return host;
            }

            T existingHost = GetComponentInChildren<T>(true);
            if (existingHost != null)
            {
                return existingHost;
            }

            GameObject hostObject = new GameObject(hostName);
            hostObject.transform.SetParent(transform, false);
            return hostObject.AddComponent<T>();
        }
    }
}

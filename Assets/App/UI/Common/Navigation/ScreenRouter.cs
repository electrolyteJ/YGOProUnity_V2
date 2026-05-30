using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.UI.Common.Navigation
{
    public interface IUiScreenController
    {
        string RouteKey { get; }

        bool IsVisible { get; }

        void Show(object parameter);

        void Hide();
    }

    public abstract class UiScreenControllerBase : MonoBehaviour, IUiScreenController
    {
        [SerializeField] private string routeKey = string.Empty;
        [SerializeField] private GameObject screenRoot;
        [SerializeField] private bool hideOnAwake = true;

        public string RouteKey
        {
            get { return routeKey; }
        }

        public bool IsVisible
        {
            get
            {
                GameObject targetRoot = ResolveScreenRoot();
                return targetRoot != null && targetRoot.activeSelf;
            }
        }

        protected virtual void Awake()
        {
            if (hideOnAwake)
            {
                GameObject targetRoot = ResolveScreenRoot();
                if (targetRoot != null)
                {
                    targetRoot.SetActive(false);
                }
            }
        }

        public void Show(object parameter)
        {
            GameObject targetRoot = ResolveScreenRoot();
            if (targetRoot != null)
            {
                targetRoot.SetActive(true);
            }

            OnShow(parameter);
        }

        public void Hide()
        {
            OnHide();

            GameObject targetRoot = ResolveScreenRoot();
            if (targetRoot != null)
            {
                targetRoot.SetActive(false);
            }
        }

        protected virtual void OnShow(object parameter)
        {
        }

        protected virtual void OnHide()
        {
        }

        private GameObject ResolveScreenRoot()
        {
            if (screenRoot == null)
            {
                screenRoot = gameObject;
            }

            return screenRoot;
        }
    }

    public sealed class ScreenRouter : MonoBehaviour
    {
        private readonly Dictionary<string, IUiScreenController> routes = new Dictionary<string, IUiScreenController>(StringComparer.Ordinal);
        private readonly List<string> navigationHistory = new List<string>();

        [SerializeField] private Transform registrationRoot;

        private IUiScreenController currentController;
        private string currentRouteKey = string.Empty;

        public event Action<string, string> RouteChanged;

        public string CurrentRouteKey
        {
            get { return currentRouteKey; }
        }

        public IUiScreenController CurrentController
        {
            get { return currentController; }
        }

        public bool CanNavigateBack
        {
            get { return navigationHistory.Count > 0; }
        }

        public void RegisterFromChildren()
        {
            RegisterFromHierarchy(transform);
        }

        public void RegisterFromHierarchy(Transform root)
        {
            Transform resolvedRoot = root;
            if (resolvedRoot == null)
            {
                resolvedRoot = registrationRoot != null ? registrationRoot : transform;
            }

            registrationRoot = resolvedRoot;
            routes.Clear();

            MonoBehaviour[] behaviours = resolvedRoot.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                IUiScreenController controller = behaviours[i] as IUiScreenController;
                if (controller != null)
                {
                    Register(controller);
                }
            }
        }

        public bool Register(IUiScreenController controller)
        {
            if (controller == null)
            {
                return false;
            }

            string routeKey = controller.RouteKey;
            if (string.IsNullOrEmpty(routeKey))
            {
                Debug.LogWarning("Attempted to register a screen controller without a route key.");
                return false;
            }

            IUiScreenController existingController;
            if (routes.TryGetValue(routeKey, out existingController) && !ReferenceEquals(existingController, controller))
            {
                Debug.LogWarning("Replacing screen route registration for route '" + routeKey + "'.");
                routes[routeKey] = controller;
                if (ReferenceEquals(currentController, existingController))
                {
                    currentController = controller;
                }

                return true;
            }

            routes[routeKey] = controller;
            return true;
        }

        public bool Unregister(string routeKey)
        {
            if (string.IsNullOrEmpty(routeKey))
            {
                return false;
            }

            if (currentController != null && string.Equals(currentRouteKey, routeKey, StringComparison.Ordinal))
            {
                currentController.Hide();
                currentController = null;
                currentRouteKey = string.Empty;
            }

            RemoveHistoryEntries(routeKey);

            return routes.Remove(routeKey);
        }

        public bool Unregister(IUiScreenController controller)
        {
            if (controller == null)
            {
                return false;
            }

            return Unregister(controller.RouteKey);
        }

        public bool HasRoute(string routeKey)
        {
            if (string.IsNullOrEmpty(routeKey))
            {
                return false;
            }

            return routes.ContainsKey(routeKey);
        }

        public bool TryGetController(string routeKey, out IUiScreenController controller)
        {
            if (string.IsNullOrEmpty(routeKey))
            {
                controller = null;
                return false;
            }

            return routes.TryGetValue(routeKey, out controller);
        }

        public bool TryNavigate(string routeKey)
        {
            return TryNavigate(routeKey, null);
        }

        public bool TryNavigate(string routeKey, object parameter)
        {
            IUiScreenController targetController;
            if (!TryGetController(routeKey, out targetController))
            {
                Debug.LogWarning("Screen route was not found: '" + routeKey + "'.");
                return false;
            }

            string previousRoute = currentRouteKey;
            bool isSameController = ReferenceEquals(currentController, targetController);
            if (currentController != null && !isSameController)
            {
                if (!string.IsNullOrEmpty(currentRouteKey))
                {
                    navigationHistory.Add(currentRouteKey);
                }

                currentController.Hide();
            }

            currentController = targetController;
            currentRouteKey = routeKey;
            currentController.Show(parameter);

            if (!string.Equals(previousRoute, currentRouteKey, StringComparison.Ordinal))
            {
                Action<string, string> routeChanged = RouteChanged;
                if (routeChanged != null)
                {
                    routeChanged(previousRoute, currentRouteKey);
                }
            }

            return true;
        }

        public bool TryNavigateBack()
        {
            if (navigationHistory.Count == 0)
            {
                return false;
            }

            string previousRouteKey = navigationHistory[navigationHistory.Count - 1];
            navigationHistory.RemoveAt(navigationHistory.Count - 1);
            return TryNavigateWithoutHistory(previousRouteKey);
        }

        public void HideCurrent()
        {
            if (currentController == null)
            {
                return;
            }

            string previousRoute = currentRouteKey;
            currentController.Hide();
            currentController = null;
            currentRouteKey = string.Empty;

            Action<string, string> routeChanged = RouteChanged;
            if (routeChanged != null)
            {
                routeChanged(previousRoute, currentRouteKey);
            }
        }

        private bool TryNavigateWithoutHistory(string routeKey)
        {
            IUiScreenController targetController;
            if (!TryGetController(routeKey, out targetController))
            {
                return false;
            }

            string previousRoute = currentRouteKey;
            bool isSameController = ReferenceEquals(currentController, targetController);
            if (currentController != null && !isSameController)
            {
                currentController.Hide();
            }

            currentController = targetController;
            currentRouteKey = routeKey;
            currentController.Show(null);

            if (!string.Equals(previousRoute, currentRouteKey, StringComparison.Ordinal))
            {
                Action<string, string> routeChanged = RouteChanged;
                if (routeChanged != null)
                {
                    routeChanged(previousRoute, currentRouteKey);
                }
            }

            return true;
        }

        private void RemoveHistoryEntries(string routeKey)
        {
            for (int i = navigationHistory.Count - 1; i >= 0; i--)
            {
                if (string.Equals(navigationHistory[i], routeKey, StringComparison.Ordinal))
                {
                    navigationHistory.RemoveAt(i);
                }
            }
        }
    }
}

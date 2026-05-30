using System;
using App.Core;
using App.Platform;
using UnityEngine;
using CoreAppContext = App.Core.AppContext;

namespace App.Boot
{
    public static class AppBootstrapContextFactory
    {
        public static CoreAppContext CreateContext()
        {
            return new CoreAppContext(
                new UnityAppLogger(),
                new NoOpEventBus(),
                new LegacyAppConfig(),
                new RuntimeFileStorage(),
                new LegacyNetworkPlatform(),
                new RuntimePlatformPaths());
        }

        private sealed class UnityAppLogger : IAppLogger
        {
            public void Log(string message)
            {
                Debug.Log(message);
            }

            public void LogWarning(string message)
            {
                Debug.LogWarning(message);
            }

            public void LogError(string message)
            {
                Debug.LogError(message);
            }
        }

        private sealed class NoOpEventBus : IAppEventBus
        {
            public void Publish<TEvent>(TEvent appEvent)
            {
            }

            public void Subscribe<TEvent>(Action<TEvent> handler)
            {
            }

            public void Unsubscribe<TEvent>(Action<TEvent> handler)
            {
            }
        }

        private sealed class LegacyAppConfig : IAppConfig
        {
            public string Get(string key, string defaultValue)
            {
                return Config.Get(key, defaultValue);
            }
        }
    }
}

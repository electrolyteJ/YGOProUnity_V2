using System;

namespace App.Core
{
    public sealed class AppContext
    {
        public AppContext(
            IAppLogger logger,
            IAppEventBus events,
            IAppConfig config,
            IFileStorage storage,
            INetworkPlatform network,
            IPlatformPaths paths)
        {
            Logger = logger ?? throw new ArgumentNullException("logger");
            Events = events ?? throw new ArgumentNullException("events");
            Config = config ?? throw new ArgumentNullException("config");
            Storage = storage ?? throw new ArgumentNullException("storage");
            Network = network ?? throw new ArgumentNullException("network");
            Paths = paths ?? throw new ArgumentNullException("paths");
        }

        public IAppLogger Logger { get; private set; }

        public IAppEventBus Events { get; private set; }

        public IAppConfig Config { get; private set; }

        public IFileStorage Storage { get; private set; }

        public INetworkPlatform Network { get; private set; }

        public IPlatformPaths Paths { get; private set; }
    }
}

using System;

namespace App.Boot
{
    public sealed class AppSceneStartup
    {
        public AppSceneStartup(Action prepare, Action legacyInitialize, Action complete)
        {
            Prepare = prepare;
            LegacyInitialize = legacyInitialize ?? throw new ArgumentNullException("legacyInitialize");
            Complete = complete;
        }

        public Action Prepare { get; private set; }

        public Action LegacyInitialize { get; private set; }

        public Action Complete { get; private set; }
    }
}

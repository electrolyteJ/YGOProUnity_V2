using System;
using CoreAppContext = App.Core.AppContext;

namespace App.Boot
{
    public sealed class AppBootstrap
    {
        public AppBootstrap(CoreAppContext context)
        {
            Context = context ?? throw new ArgumentNullException("context");
        }

        public CoreAppContext Context { get; private set; }

        public void Start(Action legacyInitialize)
        {
            Start(new AppSceneStartup(null, legacyInitialize, null));
        }

        public void Start(AppSceneStartup startup)
        {
            AppSceneEntry sceneEntry = new AppSceneEntry(Context);
            sceneEntry.Enter(startup);
        }
    }
}

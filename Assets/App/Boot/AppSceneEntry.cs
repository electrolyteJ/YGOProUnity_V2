using System;
using CoreAppContext = App.Core.AppContext;

namespace App.Boot
{
    public sealed class AppSceneEntry
    {
        public AppSceneEntry(CoreAppContext context)
        {
            Context = context ?? throw new ArgumentNullException("context");
        }

        public CoreAppContext Context { get; private set; }

        public void Enter(AppSceneStartup startup)
        {
            if (startup == null)
            {
                throw new ArgumentNullException("startup");
            }

            if (startup.Prepare != null)
            {
                startup.Prepare();
            }

            startup.LegacyInitialize();

            if (startup.Complete != null)
            {
                startup.Complete();
            }
        }
    }
}

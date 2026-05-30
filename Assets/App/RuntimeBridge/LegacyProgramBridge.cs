using System;
using App.Boot;
using UnityEngine;
using CoreAppContext = App.Core.AppContext;

namespace App.RuntimeBridge
{
    public sealed class LegacyProgramBridge
    {
        public static void Start(Program owner, Action legacyInitialize)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            if (legacyInitialize == null)
            {
                throw new ArgumentNullException("legacyInitialize");
            }

            CoreAppContext context = AppBootstrapContextFactory.CreateContext();

            AppBootstrap bootstrap = new AppBootstrap(context);
            bootstrap.Start(new AppSceneStartup(
                delegate { InitializeStartup(owner, context); },
                legacyInitialize,
                delegate { ScheduleGameStart(owner); }));
        }

        private static void InitializeStartup(Program owner, CoreAppContext context)
        {
            if (Screen.width < 100 || Screen.height < 100)
            {
                Screen.SetResolution(1300, 700, false);
            }

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 144;
            owner.mouseParticle = UnityEngine.Object.Instantiate(owner.new_mouse);
            RuntimeArchiveBootstrap.EnsureRequiredDirectories(context.Logger.Log);
        }

        private static void ScheduleGameStart(Program owner)
        {
            Program.go(500, owner.RunGameStartFromBridge);
        }
    }
}

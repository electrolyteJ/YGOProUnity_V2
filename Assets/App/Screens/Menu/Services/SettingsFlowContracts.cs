using System;
using System.Collections.Generic;

namespace App.Screens.Menu.Services
{
    public sealed class SettingsViewState
    {
        public SettingsViewState()
        {
            AdditionalToggles = new Dictionary<string, bool>();
        }

        public bool FullScreen { get; set; }

        public bool IgnoreWatcher { get; set; }

        public bool IgnoreOpponent { get; set; }

        public bool SmartSelect { get; set; }

        public bool AutoChain { get; set; }

        public bool HandPosition { get; set; }

        public bool HandMirrorPosition { get; set; }

        public bool Spyer { get; set; }

        public bool Resize { get; set; }

        public bool LongField { get; set; }

        public bool HighQuality { get; set; }

        public float Volume { get; set; }

        public float Size { get; set; }

        public float VerticalSize { get; set; }

        public string ShowoffAttack { get; set; }

        public string ShowoffStar { get; set; }

        public Dictionary<string, bool> AdditionalToggles { get; private set; }
    }

    public class SettingsResizeRequest
    {
        public SettingsResizeRequest(int width, int height, bool fullScreen)
        {
            Width = width;
            Height = height;
            FullScreen = fullScreen;
        }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public bool FullScreen { get; private set; }
    }

    public sealed class ResolutionRequest : SettingsResizeRequest
    {
        public ResolutionRequest(int width, int height, bool fullScreen)
            : base(width, height, fullScreen)
        {
        }
    }

    public sealed class SettingsRuntimeActions
    {
        public Action<bool> SetMonsterCloudEnabled { get; set; }

        public Action<bool> SetMouseParticleVisible { get; set; }

        public Action<bool> SetLongFieldEnabled { get; set; }

        public Action<float> SetFieldSize { get; set; }

        public Action<float> SetVerticalScale { get; set; }

        public Action RefreshFieldLayout { get; set; }

        public Action<int> SetQualityLevel { get; set; }
    }
}

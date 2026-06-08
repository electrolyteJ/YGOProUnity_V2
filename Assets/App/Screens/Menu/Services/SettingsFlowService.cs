using System;
using System.Collections.Generic;

namespace App.Screens.Menu.Services
{
    public sealed class SettingsFlowService
    {
        public sealed class Callbacks
        {
            public Action CloseRequested;
            public Action ResizeRequested;
            public Action SaveRequested;
            public Action PersistRequested;
            public Action LongFieldChanged;
            public Action FieldSizeChanged;
            public Action VerticalSizeChanged;
            public Action AlphaChanged;
            public Action MouseEffectChanged;
            public Action CloseUpChanged;
            public Action CloudChanged;
            public Action RefreshCoreRequested;
        }

        private readonly Callbacks callbacks;

        public SettingsFlowService()
            : this(new Callbacks())
        {
        }

        public SettingsFlowService(Callbacks callbacks)
        {
            this.callbacks = callbacks ?? new Callbacks();
        }

        public void RequestClose()
        {
            Invoke(callbacks.CloseRequested);
        }

        public void RequestResize()
        {
            Invoke(callbacks.ResizeRequested);
        }

        public void RequestSave()
        {
            Invoke(callbacks.SaveRequested);
        }

        public void RequestPersist()
        {
            Invoke(callbacks.PersistRequested);
        }

        public void RequestLongFieldChange()
        {
            Invoke(callbacks.LongFieldChanged);
        }

        public void RequestFieldSizeChange()
        {
            Invoke(callbacks.FieldSizeChanged);
        }

        public void RequestVerticalSizeChange()
        {
            Invoke(callbacks.VerticalSizeChanged);
        }

        public void RequestAlphaChange()
        {
            Invoke(callbacks.AlphaChanged);
        }

        public void RequestMouseEffectChange()
        {
            Invoke(callbacks.MouseEffectChanged);
        }

        public void RequestCloseUpChange()
        {
            Invoke(callbacks.CloseUpChanged);
        }

        public void RequestCloudChange()
        {
            Invoke(callbacks.CloudChanged);
        }

        public void RequestRefreshCore()
        {
            Invoke(callbacks.RefreshCoreRequested);
        }

        public SettingsViewState LoadState(
            IEnumerable<string> additionalToggleNames,
            Func<string, string, string> readConfigValue,
            Func<bool> readFullScreen,
            Func<int> readQualityLevel)
        {
            if (readConfigValue == null)
            {
                throw new ArgumentNullException("readConfigValue");
            }

            SettingsViewState state = new SettingsViewState();
            state.FullScreen = readFullScreen != null && readFullScreen();
            state.IgnoreWatcher = ReadBool(readConfigValue, "ignoreWatcher_", "0");
            state.IgnoreOpponent = ReadBool(readConfigValue, "ignoreOP_", "0");
            state.SmartSelect = ReadBool(readConfigValue, "smartSelect_", "1");
            state.AutoChain = ReadBool(readConfigValue, "autoChain_", "1");
            state.HandPosition = ReadBool(readConfigValue, "handPosition_", "1");
            state.HandMirrorPosition = ReadBool(readConfigValue, "handmPosition_", "1");
            state.Spyer = ReadBool(readConfigValue, "spyer_", "1");
            state.Resize = ReadBool(readConfigValue, "resize_", "0");
            state.LongField = ReadBool(readConfigValue, "longField_", "0");
            state.HighQuality = readQualityLevel != null && readQualityLevel() >= 3;
            state.Volume = ReadScaledFloat(readConfigValue, "vol_", "750");
            state.Size = ReadScaledFloat(readConfigValue, "size_", "500");
            state.VerticalSize = ReadScaledFloat(readConfigValue, "vSize_", "500");
            state.ShowoffAttack = readConfigValue("showoffATK", "1800");
            state.ShowoffStar = readConfigValue("showoffStar", "5");

            if (additionalToggleNames != null)
            {
                foreach (string additionalToggleName in additionalToggleNames)
                {
                    if (string.IsNullOrEmpty(additionalToggleName))
                    {
                        continue;
                    }

                    state.AdditionalToggles[additionalToggleName] = ReadBool(readConfigValue, additionalToggleName, GetDefaultAdditionalToggle(additionalToggleName));
                }
            }

            return state;
        }

        public void SaveLiveState(SettingsViewState state, Action<string, string> writeConfigValue, SettingsRuntimeActions runtimeActions)
        {
            if (state == null)
            {
                throw new ArgumentNullException("state");
            }

            if (writeConfigValue == null)
            {
                throw new ArgumentNullException("writeConfigValue");
            }

            writeConfigValue("ignoreWatcher_", WriteBool(state.IgnoreWatcher));
            writeConfigValue("ignoreOP_", WriteBool(state.IgnoreOpponent));
            writeConfigValue("smartSelect_", WriteBool(state.SmartSelect));
            writeConfigValue("autoChain_", WriteBool(state.AutoChain));
            writeConfigValue("handPosition_", WriteBool(state.HandPosition));
            writeConfigValue("handmPosition_", WriteBool(state.HandMirrorPosition));
            writeConfigValue("spyer_", WriteBool(state.Spyer));

            if (runtimeActions != null && runtimeActions.SetQualityLevel != null)
            {
                runtimeActions.SetQualityLevel(state.HighQuality ? 5 : 0);
            }
        }

        public void SaveQuitState(SettingsViewState state, Action<string, string> writeConfigValue, Func<bool> readIsMaximized)
        {
            if (state == null)
            {
                throw new ArgumentNullException("state");
            }

            if (writeConfigValue == null)
            {
                throw new ArgumentNullException("writeConfigValue");
            }

            writeConfigValue("vol_", WriteScaledFloat(state.Volume));
            writeConfigValue("size_", WriteScaledFloat(state.Size));
            writeConfigValue("vSize_", WriteScaledFloat(state.VerticalSize));
            writeConfigValue("longField_", WriteBool(state.LongField));

            foreach (KeyValuePair<string, bool> pair in state.AdditionalToggles)
            {
                writeConfigValue(pair.Key, WriteBool(pair.Value));
            }

            writeConfigValue("showoffATK", state.ShowoffAttack ?? "1800");
            writeConfigValue("showoffStar", state.ShowoffStar ?? "5");
            writeConfigValue("resize_", WriteBool(state.Resize));
            writeConfigValue("maximize_", WriteBool(readIsMaximized != null && readIsMaximized()));
        }

        public void ApplyCloudToggle(bool enabled, SettingsRuntimeActions runtimeActions)
        {
            if (runtimeActions != null && runtimeActions.SetMonsterCloudEnabled != null)
            {
                runtimeActions.SetMonsterCloudEnabled(enabled);
            }
        }

        public void ApplyMouseToggle(bool enabled, SettingsRuntimeActions runtimeActions)
        {
            if (runtimeActions != null && runtimeActions.SetMouseParticleVisible != null)
            {
                runtimeActions.SetMouseParticleVisible(enabled);
            }
        }

        public void ApplyLongFieldToggle(bool enabled, SettingsRuntimeActions runtimeActions)
        {
            if (runtimeActions != null && runtimeActions.SetLongFieldEnabled != null)
            {
                runtimeActions.SetLongFieldEnabled(enabled);
            }

            if (runtimeActions != null && runtimeActions.RefreshFieldLayout != null)
            {
                runtimeActions.RefreshFieldLayout();
            }
        }

        public void ApplyFieldSize(float sliderValue, SettingsRuntimeActions runtimeActions)
        {
            if (runtimeActions != null && runtimeActions.SetFieldSize != null)
            {
                runtimeActions.SetFieldSize(1f + sliderValue * 0.21f);
            }
        }

        public void ApplyVerticalSize(float sliderValue, SettingsRuntimeActions runtimeActions)
        {
            if (runtimeActions != null && runtimeActions.SetVerticalScale != null)
            {
                runtimeActions.SetVerticalScale(4f + 2f * sliderValue);
            }
        }

        public void ApplyCloseUpPreset(bool enabled, Action<float> applyAlpha, Action<float> applySize)
        {
            if (applyAlpha != null)
            {
                applyAlpha(enabled ? 0.6666f : 0f);
            }

            if (applySize != null)
            {
                applySize(enabled ? 1f : 0f);
            }
        }

        public bool TryBuildResizeRequest(string screenValue, bool fullScreen, out SettingsResizeRequest request)
        {
            ResolutionRequest parsedRequest;
            bool result = TryParseResolution(screenValue, fullScreen, out parsedRequest);
            request = parsedRequest;
            return result;
        }

        public bool TryParseResolution(string screenValue, bool fullScreen, out ResolutionRequest request)
        {
            request = null;
            if (string.IsNullOrEmpty(screenValue))
            {
                return false;
            }

            string[] parts = screenValue.Split(new[] { "*" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                return false;
            }

            int width;
            int height;
            if (!int.TryParse(parts[0], out width) || !int.TryParse(parts[1], out height))
            {
                return false;
            }

            request = new ResolutionRequest(width, height, fullScreen);
            return true;
        }

        public void Close(Action hideSettings)
        {
            if (hideSettings != null)
            {
                hideSettings();
            }
        }

        private static bool ReadBool(Func<string, string, string> readConfigValue, string key, string defaultValue)
        {
            return readConfigValue(key, defaultValue) == "1";
        }

        private static float ReadScaledFloat(Func<string, string, string> readConfigValue, string key, string defaultValue)
        {
            int rawValue;
            if (!int.TryParse(readConfigValue(key, defaultValue), out rawValue))
            {
                rawValue = int.Parse(defaultValue);
            }

            return rawValue / 1000f;
        }

        private static string WriteBool(bool value)
        {
            return value ? "1" : "0";
        }

        private static string WriteScaledFloat(float value)
        {
            return ((int)(value * 1000f)).ToString();
        }

        private static string GetDefaultAdditionalToggle(string toggleName)
        {
            switch (toggleName)
            {
                case "*mouseParticle":
                case "*showOff":
                case "*Efield":
                case "*Ewin":
                    return "1";
                default:
                    return "0";
            }
        }

        private static void Invoke(Action handler)
        {
            if (handler != null)
            {
                handler();
            }
        }
    }
}

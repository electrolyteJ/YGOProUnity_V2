using System;
using App.Features.Menu.Services;
using AppSettingsScreenController = App.UI.Screens.Menu.SettingsScreenController;
using UnityEngine;

public static class SettingsLegacyBindings
{
    public static SettingsViewState CaptureCurrentState(GameObject root, LAZYsetting settingView, Func<bool> readFullScreen)
    {
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        if (settingView == null)
        {
            throw new ArgumentNullException("settingView");
        }

        SettingsViewState state = new SettingsViewState();
        state.FullScreen = readFullScreen != null && readFullScreen();
        state.IgnoreWatcher = UIHelper.getByName<UIToggle>(root, "ignoreWatcher_").value;
        state.IgnoreOpponent = UIHelper.getByName<UIToggle>(root, "ignoreOP_").value;
        state.SmartSelect = UIHelper.getByName<UIToggle>(root, "smartSelect_").value;
        state.AutoChain = UIHelper.getByName<UIToggle>(root, "autoChain_").value;
        state.HandPosition = UIHelper.getByName<UIToggle>(root, "handPosition_").value;
        state.HandMirrorPosition = UIHelper.getByName<UIToggle>(root, "handmPosition_").value;
        state.Spyer = UIHelper.getByName<UIToggle>(root, "spyer_").value;
        state.Resize = UIHelper.getByName<UIToggle>(root, "resize_").value;
        state.LongField = UIHelper.getByName<UIToggle>(root, "longField_").value;
        state.HighQuality = UIHelper.getByName<UIToggle>(root, "high_").value;
        state.Volume = UIHelper.getByName<UISlider>(root, "vol_").value;
        state.Size = UIHelper.getByName<UISlider>(root, "size_").value;
        state.VerticalSize = UIHelper.getByName<UISlider>(root, "vSize_").value;
        state.ShowoffAttack = settingView.showoffATK.value;
        state.ShowoffStar = settingView.showoffStar.value;

        string[] additionalToggleNames = GetAdditionalToggleNames(root);
        for (int i = 0; i < additionalToggleNames.Length; i++)
        {
            state.AdditionalToggles[additionalToggleNames[i]] = UIHelper.getByName<UIToggle>(root, additionalToggleNames[i]).value;
        }

        return state;
    }

    public static void ApplyLoadedState(GameObject root, LAZYsetting settingView, SettingsViewState state)
    {
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        if (settingView == null)
        {
            throw new ArgumentNullException("settingView");
        }

        if (state == null)
        {
            throw new ArgumentNullException("state");
        }

        UIHelper.getByName<UIToggle>(root, "full_").value = state.FullScreen;
        UIHelper.getByName<UIToggle>(root, "ignoreWatcher_").value = state.IgnoreWatcher;
        UIHelper.getByName<UIToggle>(root, "ignoreOP_").value = state.IgnoreOpponent;
        UIHelper.getByName<UIToggle>(root, "smartSelect_").value = state.SmartSelect;
        UIHelper.getByName<UIToggle>(root, "autoChain_").value = state.AutoChain;
        UIHelper.getByName<UIToggle>(root, "handPosition_").value = state.HandPosition;
        UIHelper.getByName<UIToggle>(root, "handmPosition_").value = state.HandMirrorPosition;
        UIHelper.getByName<UIToggle>(root, "spyer_").value = state.Spyer;
        UIHelper.getByName<UIToggle>(root, "resize_").value = state.Resize;
        UIHelper.getByName<UIToggle>(root, "longField_").value = state.LongField;
        UIHelper.getByName<UIToggle>(root, "high_").value = state.HighQuality;

        UIToggle[] collection = root.GetComponentsInChildren<UIToggle>();
        for (int i = 0; i < collection.Length; i++)
        {
            bool value;
            if (state.AdditionalToggles.TryGetValue(collection[i].name, out value))
            {
                collection[i].value = value;
            }
        }

        settingView.showoffATK.value = state.ShowoffAttack;
        settingView.showoffStar.value = state.ShowoffStar;
    }

    public static string[] GetAdditionalToggleNames(GameObject root)
    {
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        UIToggle[] collection = root.GetComponentsInChildren<UIToggle>();
        int count = 0;
        for (int i = 0; i < collection.Length; i++)
        {
            if (collection[i].name.Length > 0 && collection[i].name[0] == '*')
            {
                count++;
            }
        }

        string[] names = new string[count];
        int writeIndex = 0;
        for (int i = 0; i < collection.Length; i++)
        {
            if (collection[i].name.Length > 0 && collection[i].name[0] == '*')
            {
                names[writeIndex++] = collection[i].name;
            }
        }

        return names;
    }

    public static SettingsRuntimeActions CreateRuntimeActions(
        Action<bool> setMonsterCloudEnabled,
        Action<bool> setMouseParticleVisible,
        Action<bool> setLongFieldEnabled,
        Action<float> setFieldSize,
        Action<float> setVerticalScale,
        Action refreshFieldLayout,
        Action<int> setQualityLevel)
    {
        return new SettingsRuntimeActions
        {
            SetMonsterCloudEnabled = setMonsterCloudEnabled,
            SetMouseParticleVisible = setMouseParticleVisible,
            SetLongFieldEnabled = setLongFieldEnabled,
            SetFieldSize = setFieldSize,
            SetVerticalScale = setVerticalScale,
            RefreshFieldLayout = refreshFieldLayout,
            SetQualityLevel = setQualityLevel,
        };
    }

    public static void ApplyInitialRuntimeState(AppSettingsScreenController controller, SettingsViewState state, SettingsRuntimeActions runtimeActions)
    {
        if (controller == null)
        {
            throw new ArgumentNullException("controller");
        }

        if (state == null)
        {
            throw new ArgumentNullException("state");
        }

        if (runtimeActions == null)
        {
            throw new ArgumentNullException("runtimeActions");
        }

        if (runtimeActions.SetQualityLevel != null)
        {
            runtimeActions.SetQualityLevel(state.HighQuality ? 5 : 0);
        }

        controller.ApplyFieldSize(state.Size, runtimeActions);
        controller.ApplyLongFieldToggle(state.LongField, runtimeActions);
        controller.ApplyVerticalSize(state.VerticalSize, runtimeActions);
    }
}

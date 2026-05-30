using App.Features.Menu.Services;
using AppSettingsScreenController = App.UI.Screens.Menu.SettingsScreenController;
using UnityEngine;
using System;
public class Setting : WindowServant2D
{
    private SettingsFlowService flowService;
    private AppSettingsScreenController screenController;

    private EventDelegate onChange;

    public LAZYsetting setting;

    private SettingsFlowService FlowService
    {
        get
        {
            if (flowService == null)
            {
                flowService = new SettingsFlowService();
            }

            return flowService;
        }
    }

    private AppSettingsScreenController ScreenController
    {
        get
        {
            if (screenController == null)
            {
                screenController = new AppSettingsScreenController(FlowService);
            }

            return screenController;
        }
    }

    public override void initialize()
    {
        gameObject = createWindow(this, Program.I().new_ui_setting);
        setting = gameObject.GetComponentInChildren<LAZYsetting>();
        ScreenController.Bind(gameObject, FlowService, ApplyLegacyShow, ApplyLegacyHide);
        UIHelper.registEvent(gameObject, "exit_", onClickExit);
        UIHelper.registEvent(gameObject, "screen_", resizeScreen);
        UIHelper.registEvent(gameObject, "full_", resizeScreen);
        UIHelper.registEvent(gameObject, "resize_", resizeScreen);
        SettingsViewState loadedState = LoadCurrentState();
        ApplyLoadedState(loadedState);
        UIHelper.registEvent(gameObject, "ignoreWatcher_", save);
        UIHelper.registEvent(gameObject, "ignoreOP_", save);
        UIHelper.registEvent(gameObject, "smartSelect_", save);
        UIHelper.registEvent(gameObject, "autoChain_", save);
        UIHelper.registEvent(gameObject, "handPosition_", save);
        UIHelper.registEvent(gameObject, "handmPosition_", save);
        UIHelper.registEvent(gameObject, "spyer_", save);
        UIHelper.registEvent(gameObject, "high_", save);
        UIHelper.registEvent(gameObject, "longField_", onChangeLongField);
        UIHelper.registEvent(gameObject, "size_", onChangeSize);
        //UIHelper.registEvent(gameObject, "alpha_", onChangeAlpha);
        UIHelper.registEvent(gameObject, "vSize_", onChangeVsize);
        sliderSize = UIHelper.getByName<UISlider>(gameObject, "size_");
        //sliderAlpha = UIHelper.getByName<UISlider>(gameObject, "alpha_");
        sliderVsize = UIHelper.getByName<UISlider>(gameObject, "vSize_");
        Program.go(2000,readVales);
        UIHelper.registEvent(setting.showoffATK.gameObject, onchangeClose);
        UIHelper.registEvent(setting.showoffStar.gameObject, onchangeClose);
        UIHelper.registEvent(setting.mouseEffect.gameObject, onchangeMouse);
        UIHelper.registEvent(setting.closeUp.gameObject, onchangeCloseUp);
        UIHelper.registEvent(setting.cloud.gameObject, onchangeCloud);  
        UIHelper.registEvent(setting.Vpedium.gameObject, onCP);
        UIHelper.registEvent(setting.Vfield.gameObject, onCP);
        UIHelper.registEvent(setting.Vlink.gameObject, onCP);
        ApplyInitialRuntimeState(loadedState);
        onchangeMouse();
        onchangeCloud();
        setScreenSizeValue();
    }

    public override void show()
    {
        ApplyLegacyShow();
        ScreenController.SynchronizeLegacyShown();
    }

    public override void hide()
    {
        ApplyLegacyHide();
        ScreenController.SynchronizeLegacyHidden();
    }

    private void ApplyLegacyShow()
    {
        base.show();
    }

    private void ApplyLegacyHide()
    {
        base.hide();
    }

    private void ApplyInitialRuntimeState(SettingsViewState state)
    {
        if (state == null)
        {
            return;
        }

        SettingsLegacyBindings.ApplyInitialRuntimeState(ScreenController, state, CreateRuntimeActions());
    }

    private void readVales()
    {
        try
        {
            SettingsViewState state = LoadCurrentState();
            setting.sliderVolum.forceValue(state.Volume);
            setting.sliderSize.forceValue(state.Size);
            setting.sliderSizeDrawing.forceValue(state.VerticalSize);
            //setting.sliderAlpha.forceValue(((float)(int.Parse(Config.Get("alpha_", "666")))) / 1000f);
            onChangeAlpha();
            onChangeSize();
            onChangeVsize();
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    public void onchangeCloud()
    {
        ScreenController.ApplyCloudToggle(setting.cloud.value, CreateRuntimeActions());
    }

    public void onchangeMouse()
    {
        ScreenController.ApplyMouseToggle(setting.mouseEffect.value, CreateRuntimeActions());
    }

    //private int dontResizeTwice = 2;

    public void setScreenSizeValue()
    {
        //dontResizeTwice = 3;
        UIHelper.getByName<UIPopupList>(gameObject, "screen_").value = Screen.width.ToString() + "*" + Screen.height.ToString();
    }

    void onCP()
    {
        try
        {
            Program.I().ocgcore.realize(true);
        }
        catch (Exception e) 
        {
        }
    }


    public void onchangeCloseUp()   
    {
        ScreenController.ApplyCloseUpPreset(
            setting.closeUp.value,
            delegate(float value) { setting.sliderAlpha.forceValue(value); },
            delegate(float value) { setting.sliderSize.forceValue(value); });
        onChangeSize();
        onChangeAlpha();
    }

    public int atk = 1800;
    public int star = 5;

    void onchangeClose()
    {
        atk = 1800;
        star = 5;
        try
        {
            atk = int.Parse(setting.showoffATK.value);
        }
        catch (Exception)
        {

        }
        try
        {
            star = int.Parse(setting.showoffStar.value);
        }
        catch (Exception)
        {

        }
    }


    UISlider sliderAlpha;
    void onChangeAlpha()
    {
        if (sliderAlpha != null)
        {
            Program.transparency = 1.5f * sliderAlpha.value;
        }
        Program.transparency = 1f;
    }

    void onChangeLongField()
    {
        ScreenController.ApplyLongFieldToggle(UIHelper.getByName<UIToggle>(gameObject, "longField_").value, CreateRuntimeActions());
    }

    UISlider sliderVsize;
    void onChangeVsize()
    {
        if (sliderVsize != null)
        {
            ScreenController.ApplyVerticalSize(sliderVsize.value, CreateRuntimeActions());
        }
    }

    UISlider sliderSize;
    void onChangeSize()  
    {
        if (sliderSize != null)
        {
            ScreenController.ApplyFieldSize(sliderSize.value, CreateRuntimeActions());
        }
    }

    public float vol() 
    {
        return UIHelper.getByName<UISlider>(gameObject, "vol_").value;
    }

    public override void preFrameFunction()
    {
        base.preFrameFunction();
    }

    void onClickExit()
    {
        ScreenController.Close(hide);
    }

    void resizeScreen()
    {
        //if (dontResizeTwice > 0)
        //{
        //    dontResizeTwice--;
        //    return;
        //}
        //dontResizeTwice = 2;
        if (UIHelper.isMaximized())
            UIHelper.RestoreWindow();
        SettingsResizeRequest resizeRequest;
        if (ScreenController.TryBuildResizeRequest(
            UIHelper.getByName<UIPopupList>(gameObject, "screen_").value,
            UIHelper.getByName<UIToggle>(gameObject, "full_").value,
            out resizeRequest))
        {
            Screen.SetResolution(resizeRequest.Width, resizeRequest.Height, resizeRequest.FullScreen);
        }
        Program.go(100, () => { Program.I().fixScreenProblems(); });
    }

    public void saveWhenQuit()
    {
        ScreenController.SaveQuitState(
            CaptureCurrentState(),
            delegate(string key, string value) { Config.Set(key, value); },
            delegate { return UIHelper.isMaximized(); });
    }

    public void save()
    {
        ScreenController.SaveLiveState(
            CaptureCurrentState(),
            delegate(string key, string value) { Config.Set(key, value); },
            CreateRuntimeActions());
    }

    public float soundValue()
    {
        return UIHelper.getByName<UISlider>(gameObject, "vol_").value;
    }

    private SettingsViewState LoadCurrentState()
    {
        return ScreenController.LoadState(
            SettingsLegacyBindings.GetAdditionalToggleNames(gameObject),
            delegate(string key, string defaultValue) { return Config.Get(key, defaultValue); },
            delegate { return Screen.fullScreen; },
            delegate { return QualitySettings.GetQualityLevel(); });
    }

    private void ApplyLoadedState(SettingsViewState state)
    {
        SettingsLegacyBindings.ApplyLoadedState(gameObject, setting, state);
    }

    private SettingsViewState CaptureCurrentState()
    {
        return SettingsLegacyBindings.CaptureCurrentState(
            gameObject,
            setting,
            delegate { return Screen.fullScreen; });
    }

    private SettingsRuntimeActions CreateRuntimeActions()
    {
        return SettingsLegacyBindings.CreateRuntimeActions(
            delegate(bool value) { Program.MonsterCloud = value; },
            delegate(bool value)
            {
                if (Program.I().mouseParticle != null)
                {
                    Program.I().mouseParticle.SetActive(value);
                }
            },
            delegate(bool value) { Program.longField = value; },
            delegate(float value) { Program.fieldSize = value; },
            delegate(float value) { Program.verticleScale = value; },
            onCP,
            delegate(int value) { QualitySettings.SetQualityLevel(value); });
    }
}

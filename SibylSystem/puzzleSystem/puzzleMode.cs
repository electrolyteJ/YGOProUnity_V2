using System;
using App.Screens.Puzzle.Services;
using AppPuzzleScreenController = App.UI.Screens.Puzzle.PuzzleScreenController;
using UnityEngine;

public class puzzleMode : WindowServantSP
{
    UIselectableList superScrollView = null;
    private PuzzleFlowService flowService;
    private AppPuzzleScreenController screenController;

    private PuzzleFlowService FlowService
    {
        get
        {
            if (flowService == null)
            {
                flowService = new PuzzleFlowService();
            }

            return flowService;
        }
    }

    private AppPuzzleScreenController ScreenController
    {
        get
        {
            if (screenController == null)
            {
                screenController = new AppPuzzleScreenController(FlowService);
            }

            return screenController;
        }
    }

    public override void initialize()
    {
        createWindow(Program.I().remaster_puzzleManager);
        ScreenController.Bind(gameObject, FlowService, ApplyLegacyShow, ApplyLegacyHide);
        UIHelper.registEvent(gameObject, "exit_", onClickExit);
        superScrollView = gameObject.GetComponentInChildren<UIselectableList>();
        superScrollView.selectedAction = onSelected;
        superScrollView.install();
        SetActiveFalse();
    }


    string selectedString = "miaomiaomiao";
    void onSelected()
    {
        if (!isShowed)
        {
            return;
        }

        PuzzleSelectionResult result = ScreenController.HandleSelection(isShowed, selectedString, superScrollView.selectedString);
        if (result.ShouldLaunch)
        {
            launch(result.PuzzlePath);
        }
        selectedString = result.SelectedName;
    }

    public override void preFrameFunction()
    {
        base.preFrameFunction();
        Menu.checkCommend();
    }

    public void KF_puzzle(string name)
    {
        launch(ScreenController.GetPuzzlePath(name));
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
        printFile();
    }

    private void ApplyLegacyHide()
    {
        base.hide();
    }

    void printFile()
    {
        superScrollView.clear();
        PuzzleListState listState = ScreenController.LoadPuzzleList(UIHelper.CompareName);
        for (int index = 0; index < listState.DisplayNames.Count; index++)
        {
            superScrollView.add(listState.DisplayNames[index]);
        }
    }

    void onClickExit()
    {
        ScreenController.Close(new PuzzleCloseActions
        {
            ExitOnReturn = Program.exitOnReturn,
            ExitApplication = delegate { Program.I().menu.onClickExit(); },
            ShowMenu = delegate { Program.I().shiftToServant(Program.I().menu); }
        });
    }

    PrecyOcg precy;

    public void launch(string path)
    {
        if (precy != null)
        {
            precy.dispose();
        }
        precy = new PrecyOcg();
        precy.startPuzzle(path);
    }
}

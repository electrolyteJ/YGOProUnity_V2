using System;
using System.Collections.Generic;
using App.Screens.Deck.Services;
using AppDeckScreenController = App.UI.Screens.Deck.DeckScreenController;
using UnityEngine;
using YGOSharp.OCGWrapper.Enums;

public class selectDeck : WindowServantSP
{
    UIselectableList superScrollView = null;
    UIInput searchInput = null;
    UIDeckPanel deckPanel = null;

    string sort = "sortByTimeDeck";
    private DeckFlowService flowService;
    private AppDeckScreenController screenController;

    cardPicLoader[] quickCards = new cardPicLoader[200];

    private DeckFlowService FlowService
    {
        get
        {
            if (flowService == null)
            {
                flowService = DeckLegacyBindings.CreateFlowService();
            }

            return flowService;
        }
    }

    private AppDeckScreenController ScreenController
    {
        get
        {
            if (screenController == null)
            {
                screenController = new AppDeckScreenController(FlowService);
            }

            return screenController;
        }
    }

    public override void initialize()
    {
        createWindow(Program.I().remaster_deckManager);
        ScreenController.Bind(gameObject, FlowService, ApplyLegacyShow, ApplyLegacyHide);
        deckPanel = gameObject.GetComponentInChildren<UIDeckPanel>();
        UIHelper.registEvent(gameObject, "exit_", onClickExit);
        superScrollView = gameObject.GetComponentInChildren<UIselectableList>();
        superScrollView.selectedAction = onSelected;
        UIHelper.registEvent(gameObject, "sort_", onSort);
        setSortLable();
        UIHelper.registEvent(gameObject, "edit_", onEdit);
        UIHelper.registEvent(gameObject, "new_", onNew);
        UIHelper.registEvent(gameObject, "dispose_", onDispose);
        UIHelper.registEvent(gameObject, "copy_", onCopy);
        UIHelper.registEvent(gameObject, "rename_", onRename);
        UIHelper.registEvent(gameObject, "code_", onCode);
        searchInput = UIHelper.getByName<UIInput>(gameObject, "search_");
        superScrollView.install();
        for (int i = 0; i < quickCards.Length; i++)
        {
            quickCards[i] = deckPanel.createCard();
            quickCards[i].relayer(i);
        }
        SetActiveFalse();

    }

    void onSearch()
    {
        printFile();
        superScrollView.toTop();
    }

    void onEdit()
    {
        if (!superScrollView.Selected())
        {
            return;
        }
        if (!isShowed)
        {
            return;
        }
        KF_editDeck(superScrollView.selectedString);
    }

    void returnToSelect()
    {
        Program.I().shiftToServant(Program.I().selectDeck);
    }

    string preString = "";

    public override void preFrameFunction()
    {
        base.preFrameFunction();
        Menu.checkCommend();
        if (searchInput.value != preString)
        {
            preString = searchInput.value;
            onSearch();
        }
    }

    public void KF_editDeck(string deckName)
    {
        if (ScreenController.TryOpenEditor(deckName, CreateDeckEditorOpenActions()))
        {
            return;
        }
    }

    public override void ES_RMS(string hashCode, List<messageSystemValue> result)
    {
        base.ES_RMS(hashCode, result);
        if (hashCode == "deckManager_returnAction")
        {
            ScreenController.HandleReturnDialogResult(result[0].value, CreateDeckEditorReturnActions());
        }
        if (hashCode == "onNew")
        {
            try
            {
                ScreenController.CreateDeck(result[0].value);
                RMSshow_none(InterString.Get("「[?]」创建完毕。", result[0].value));
                superScrollView.selectedString = result[0].value;
                printFile();
            }
            catch (Exception)
            {
                RMSshow_none(InterString.Get("创建卡组失败！请检查输入的文件名，以及文件夹权限。"));
            }
        }
        if (hashCode == "onDispose")
        {
            if (result[0].value == "yes")
            {
                try
                {
                    ScreenController.DeleteDeck(superScrollView.selectedString);
                    RMSshow_none(InterString.Get("「[?]」删除完毕。", superScrollView.selectedString));
                    printFile();
                }
                catch (Exception)
                {
                    RMSshow_none(InterString.Get("删除卡组失败！请检查文件夹权限。"));
                }
            }
        }
        if (hashCode == "onCopy")
        {
            try
            {
                ScreenController.CopyDeck(superScrollView.selectedString, result[0].value);
                RMSshow_none(InterString.Get("「[?]」复制完毕。", superScrollView.selectedString));
                superScrollView.selectedString = result[0].value;
                printFile();
            }
            catch (Exception)
            {
                RMSshow_none(InterString.Get("复制卡组失败！请检查输入的文件名，以及文件夹权限。"));
            }
        }
        if (hashCode == "onRename")
        {
            try
            {
                ScreenController.RenameDeck(superScrollView.selectedString, result[0].value);
                RMSshow_none(InterString.Get("「[?]」重命名完毕。", superScrollView.selectedString));
                superScrollView.selectedString = result[0].value;
                printFile();
            }
            catch (Exception)
            {
                RMSshow_none(InterString.Get("重命名卡组失败！请检查输入的文件名，以及文件夹权限。"));
            }
        }
    }

    void onNew()
    {
        RMSshow_input("onNew", InterString.Get("请输入要创建的卡组名"), UIHelper.getTimeString());
    }

    void onDispose()
    {
        if (!superScrollView.Selected())
        {
            return;
        }
        if (ScreenController.DeckExists(superScrollView.selectedString))
        {
            RMSshow_yesOrNo(
                          "onDispose"
                        , InterString.Get("确认删除「[?]」吗？", superScrollView.selectedString)
                        , new messageSystemValue { hint = "yes", value = "yes" }
                        , new messageSystemValue { hint = "no", value = "no" }
                        );
        }
    }

    void onCopy()
    {
        if (!superScrollView.Selected())
        {
            return;
        }
        if (ScreenController.DeckExists(superScrollView.selectedString))
        {
            string newname = InterString.Get("[?]的副本", superScrollView.selectedString);
            string newnamer = ScreenController.GetUniqueDeckName(newname);
            RMSshow_input("onCopy", InterString.Get("请输入复制后的卡组名"), newnamer);
        }
    }

    void onRename()
    {
        if (!superScrollView.Selected())
        {
            return;
        }
        if (ScreenController.DeckExists(superScrollView.selectedString))
        {
            RMSshow_input("onRename", InterString.Get("新的卡组名"), superScrollView.selectedString);
        }
    }

    void onCode()
    {
        if (!superScrollView.Selected())
        {
            return;
        }
        if (ScreenController.DeckExists(superScrollView.selectedString))
        {
            string path = ScreenController.GetDeckPath(superScrollView.selectedString);
            try
            {
                #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                    System.Diagnostics.Process.Start("notepad.exe", "\"" + path + "\"");
                #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                    System.Diagnostics.Process.Start("/usr/bin/open", "-e \"" + path + "\"");
                #elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
                    System.Diagnostics.Process.Start("gedit", "\"" + path + "\"");
                #else
                    RMSshow_none("当前平台不支持直接打开卡组文件。");
                #endif
            }
            catch (System.Exception)
            {
                RMSshow_none("未找到可用的卡组编辑器。");
            }
        }
    }

    private void setSortLable()
    {
        if (ReadSortMode() == DeckSortMode.ByTime)
        {
            UIHelper.trySetLableText(gameObject, "sort_", InterString.Get("时间排序"));
        }
        else
        {
            UIHelper.trySetLableText(gameObject, "sort_", InterString.Get("名称排序"));
        }
    }

    private void onSort()
    {
        Config.Set(sort, DeckLegacyBindings.WriteSortMode(DeckLegacyBindings.ToggleSortMode(ReadSortMode())));
        setSortLable();
        printFile();
    }

    string deckSelected = "";
    void onSelected()
    {
        if (deckSelected == superScrollView.selectedString)
        {
            onEdit();
        }
        deckSelected = superScrollView.selectedString;
        printSelected();
    }

    private void printSelected()
    {
        GameTextureManager.clearUnloaded();
        YGOSharp.Deck deck;
        DeckManager.FromYDKtoCodedDeck(ScreenController.GetDeckPath(deckSelected), out deck);
        int mainAll = 0;
        int mainMonster = 0;
        int mainSpell = 0;
        int mainTrap = 0;
        int sideAll = 0;
        int sideMonster = 0;
        int sideSpell = 0;
        int sideTrap = 0;
        int extraAll = 0;
        int extraFusion = 0;
        int extraLink = 0;
        int extraSync = 0;
        int extraXyz = 0;
        int currentIndex = 0;

        int[] hangshu = UIHelper.get_decklieshuArray(deck.Main.Count);
        foreach (var item in deck.Main)
        {
            mainAll++;
            YGOSharp.Card c = YGOSharp.CardsManager.Get(item);
            if ((c.Type & (UInt32)CardType.Monster) > 0)
            {
                mainMonster++;
            }
            if ((c.Type & (UInt32)CardType.Spell) > 0)
            {
                mainSpell++;
            }
            if ((c.Type & (UInt32)CardType.Trap) > 0)
            {
                mainTrap++;
            }
            quickCards[currentIndex].reCode(item);
            Vector2 v = UIHelper.get_hang_lieArry(mainAll - 1, hangshu);
            quickCards[currentIndex].transform.localPosition = new Vector3
                (
                -176.3f + UIHelper.get_left_right_indexZuo(0, 352f, (int)v.y, hangshu[(int)v.x],10)
                ,
                161.6f - v.x * 60f
                ,
                0
                );
            if (currentIndex <= 198)
            {
                currentIndex++;
            }
        }
        foreach (var item in deck.Side)
        {
            sideAll++;
            YGOSharp.Card c = YGOSharp.CardsManager.Get(item);
            if ((c.Type & (UInt32)CardType.Monster) > 0)
            {
                sideMonster++;
            }
            if ((c.Type & (UInt32)CardType.Spell) > 0)
            {
                sideSpell++;
            }
            if ((c.Type & (UInt32)CardType.Trap) > 0)
            {
                sideTrap++;
            }
            quickCards[currentIndex].reCode(item);
            quickCards[currentIndex].transform.localPosition = new Vector3
                (
                -176.3f + UIHelper.get_left_right_indexZuo(0, 352f, sideAll - 1, deck.Side.Count,10)
                ,
                -181.1f
                ,
                0
                );
            if (currentIndex <= 198)
            {
                currentIndex++;
            }
        }
        foreach (var item in deck.Extra)
        {
            extraAll++;
            YGOSharp.Card c = YGOSharp.CardsManager.Get(item);
            if ((c.Type & (UInt32)CardType.Fusion) > 0)
            {
                extraFusion++;
            }
            if ((c.Type & (UInt32)CardType.Synchro) > 0)
            {
                extraSync++;
            }
            if ((c.Type & (UInt32)CardType.Xyz) > 0)
            {
                extraXyz++;
            }
            if ((c.Type & (UInt32)CardType.Link) > 0)
            {
                extraLink++;
            }
            quickCards[currentIndex].reCode(item);
            quickCards[currentIndex].transform.localPosition = new Vector3
                (
                -176.3f + UIHelper.get_left_right_indexZuo(0, 352f, extraAll - 1, deck.Extra.Count, 10)
                ,
                -99.199f
                ,
                0
                );
            if (currentIndex <= 198)
            {
                currentIndex++;
            }
        }
        while (true)
        {
            quickCards[currentIndex].clear();
            if (currentIndex <= 198)
            {
                currentIndex++;
            }
            else
            {
                break;
            }
        }
        deckPanel.leftMain.text = GameStringHelper._zhukazu + mainAll;
        deckPanel.leftExtra.text = GameStringHelper._ewaikazu + extraAll;
        deckPanel.leftSide.text = GameStringHelper._fukazu + sideAll;
        deckPanel.rightMain.text = GameStringHelper._guaishou + mainMonster + " "+ GameStringHelper._mofa + mainSpell + " " + GameStringHelper._xianjing + mainTrap;
        deckPanel.rightExtra.text = GameStringHelper._ronghe + extraFusion + " " + GameStringHelper._tongtiao + extraSync + " " + GameStringHelper._chaoliang + extraXyz + " " + GameStringHelper._lianjie + extraLink;
        deckPanel.rightSide.text = GameStringHelper._guaishou + sideMonster + " " + GameStringHelper._mofa + sideSpell + " " + GameStringHelper._xianjing + sideTrap;
    }

    public override void show()
    {
        ApplyLegacyShow();
        ScreenController.SynchronizeLegacyShown();
        printFile();
        superScrollView.toTop();
        superScrollView.selectedString = Config.Get("deckInUse", "miaowu");
        printSelected();
    }

    public override void hide()
    {
        ApplyLegacyHide();
        ScreenController.SynchronizeLegacyHidden();
    }

    void printFile()
    {
        string deckInUse = Config.Get("deckInUse", "miaowu");
        superScrollView.clear();
        string[] deckNames = ScreenController.GetDeckNames(deckInUse, searchInput.value, ReadSortMode());
        for (int index = 0; index < deckNames.Length; index++)
        {
            superScrollView.add(deckNames[index]);
        }
        if (superScrollView.Selected() == false)
        {
            superScrollView.selectTop();
        }
    }

    void onClickExit()
    {
        ScreenController.Close(new DeckCloseActions
        {
            ExitOnReturn = Program.exitOnReturn,
            ShowMenu = delegate { Program.I().shiftToServant(Program.I().menu); },
            ExitApplication = delegate { Program.I().menu.onClickExit(); }
        });
    }

    private DeckSortMode ReadSortMode()
    {
        return DeckLegacyBindings.ReadSortMode(Config.Get(sort, "1"));
    }

    private void ApplyLegacyShow()
    {
        base.show();
        Program.charge();
    }

    private void ApplyLegacyHide()
    {
        if (isShowed && superScrollView != null && superScrollView.Selected())
        {
            Config.Set("deckInUse", superScrollView.selectedString);
        }

        base.hide();
    }

    private DeckEditorOpenActions CreateDeckEditorOpenActions()
    {
        return new DeckEditorOpenActions
        {
            SetDeckInUse = delegate(string deckName) { Config.Set("deckInUse", deckName); },
            EnterEditMode = delegate { ((DeckManager)Program.I().deckManager).shiftCondition(DeckManager.Condition.editDeck); },
            ShowEditor = delegate { Program.I().shiftToServant(Program.I().deckManager); },
            LoadDeck = delegate(string deckName) { ((DeckManager)Program.I().deckManager).loadDeck(deckName); },
            SetTitle = delegate(string deckName) { ((CardDescription)Program.I().cardDescription).setTitle(deckName); },
            ApplyLayout = delegate { ((DeckManager)Program.I().deckManager).setGoodLooking(); },
            SetReturnAction = delegate(Action returnAction) { ((DeckManager)Program.I().deckManager).returnAction = returnAction; },
            ReturnActions = CreateDeckEditorReturnActions()
        };
    }

    private DeckEditorReturnActions CreateDeckEditorReturnActions()
    {
        return new DeckEditorReturnActions
        {
            HasUnsavedChanges = delegate { return ((DeckManager)Program.I().deckManager).deckDirty; },
            SaveChanges = delegate { return Program.I().deckManager.onSave(); },
            ShowSavePrompt = delegate
            {
                RMSshow_yesOrNoOrCancle(
                    "deckManager_returnAction",
                    InterString.Get("要保存卡组的变更吗？"),
                    new messageSystemValue { hint = "yes", value = "yes" },
                    new messageSystemValue { hint = "no", value = "no" },
                    new messageSystemValue { hint = "cancle", value = "cancle" });
            },
            ReturnToDeckList = returnToSelect
        };
    }

}

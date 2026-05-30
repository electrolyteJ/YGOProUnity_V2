using System;
using System.Collections.Generic;
using System.IO;
using App.Features.Replay.Services;
using AppReplayScreenController = App.UI.Screens.Replay.ReplayScreenController;
using UnityEngine;

public class selectReplay : WindowServantSP
{
    private static readonly ReplayFlowService FlowService = ReplayLegacyBindings.CreateFlowService();
    private static readonly AppReplayScreenController ScreenController = new AppReplayScreenController(FlowService);

    private UIselectableList superScrollView = null;
    private PrecyOcg precy;
    private string selectedTrace = string.Empty;

    public override void initialize()
    {
        createWindow(Program.I().remaster_replayManager);
        ScreenController.Bind(gameObject, FlowService, ApplyLegacyShow, ApplyLegacyHide);
        UIHelper.registEvent(gameObject, "exit_", onClickExit);
        superScrollView = gameObject.GetComponentInChildren<UIselectableList>();
        superScrollView.selectedAction = onSelected;
        UIHelper.registEvent(gameObject, "sort_", onSort);
        UIHelper.registEvent(gameObject, "launch_", onLaunch);
        UIHelper.registEvent(gameObject, "rename_", onRename);
        UIHelper.registEvent(gameObject, "delete_", onDelete);
        UIHelper.registEvent(gameObject, "yrp_", onYrp);
        UIHelper.registEvent(gameObject, "ydk_", onYdk);
        UIHelper.registEvent(gameObject, "god_", onGod);
        UIHelper.registEvent(gameObject, "value_", onValue);
        RefreshSortLabel();
        superScrollView.install();
        SetActiveFalse();
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

    public override void preFrameFunction()
    {
        base.preFrameFunction();
        Menu.checkCommend();
    }

    void onValue()
    {
        RMSshow_yesOrNo(
            "onValue",
            InterString.Get("您确定要删除所有未命名的录像？"),
            new messageSystemValue { hint = "yes", value = "yes" },
            new messageSystemValue { hint = "no", value = "no" });
    }

    private void RefreshSortLabel()
    {
        if (FlowService.IsSortByTimeEnabled(GetSortPreferenceValue()))
        {
            UIHelper.trySetLableText(gameObject, "sort_", InterString.Get("时间排序"));
        }
        else
        {
            UIHelper.trySetLableText(gameObject, "sort_", InterString.Get("名称排序"));
        }
    }

    private string GetSortPreferenceValue()
    {
        return Config.Get(ReplayFlowService.SortPreferenceConfigKey, ReplayFlowService.EnabledSortValue);
    }

    private void RefreshReplayList()
    {
        superScrollView.clear();
        ReplayListState listState = ScreenController.LoadReplayList(
            GetSortPreferenceValue(),
            ReplayLegacyBindings.CreateTimeComparison(),
            ReplayLegacyBindings.CreateNameComparison());

        for (int index = 0; index < listState.DisplayNames.Count; index++)
        {
            superScrollView.add(listState.DisplayNames[index]);
        }
    }

    private void onLaunch()
    {
        if (!superScrollView.Selected() || !isShowed)
        {
            return;
        }

        KF_replay(superScrollView.selectedString);
    }

    private void onGod()
    {
        if (!superScrollView.Selected() || !isShowed)
        {
            return;
        }

        KF_replay(superScrollView.selectedString, true);
    }

    private void onSort()
    {
        Config.Set(
            ReplayFlowService.SortPreferenceConfigKey,
            ScreenController.ToggleSort(GetSortPreferenceValue()));
        RefreshSortLabel();
        RefreshReplayList();
    }

    private void onRename()
    {
        if (!superScrollView.Selected())
        {
            return;
        }

        RMSshow_input(
            "onRename",
            InterString.Get("请输入重命名后的录像名"),
            ScreenController.GetRenameInputValue(superScrollView.selectedString));
    }

    private void onDelete()
    {
        if (!superScrollView.Selected())
        {
            return;
        }

        RMSshow_yesOrNo(
            "onDelete",
            InterString.Get("删除[?],@n请确认。", superScrollView.selectedString),
            new messageSystemValue { hint = "yes", value = "yes" },
            new messageSystemValue { hint = "no", value = "no" });
    }

    Percy.YRP getYRP(byte[] buffer)
    {
        Percy.YRP returnValue = new Percy.YRP();
        try
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(buffer));
            returnValue.ID = reader.ReadInt32();
            returnValue.Version = reader.ReadInt32();
            returnValue.Flag = reader.ReadInt32();
            returnValue.Seed = reader.ReadUInt32();
            returnValue.DataSize = reader.ReadInt32();
            returnValue.Hash = reader.ReadInt32();
            returnValue.Props = reader.ReadBytes(8);
            if (returnValue.ID == 0x32707279)
            {
                for (int i = 0; i < 8; i++)
                {
                    returnValue.SeedsV2[i] = reader.ReadUInt32();
                }
                for (int i = 0; i < 4; i++)
                {
                    reader.ReadUInt32();
                }
            }
            byte[] raw = reader.ReadToEnd();
            if ((returnValue.Flag & 0x1) > 0)
            {
                SevenZip.Compression.LZMA.Decoder lzma = new SevenZip.Compression.LZMA.Decoder();
                lzma.SetDecoderProperties(returnValue.Props);
                MemoryStream decompressed = new MemoryStream();
                lzma.Code(new MemoryStream(raw), decompressed, raw.LongLength, returnValue.DataSize, null);
                raw = decompressed.ToArray();
            }
            reader = new BinaryReader(new MemoryStream(raw));
            if ((returnValue.Flag & 0x2) > 0)
            {
                Program.I().room.mode = 2;
                returnValue.playerData.Add(new Percy.YRP.PlayerData());
                returnValue.playerData.Add(new Percy.YRP.PlayerData());
                returnValue.playerData.Add(new Percy.YRP.PlayerData());
                returnValue.playerData.Add(new Percy.YRP.PlayerData());
                returnValue.playerData[0].name = reader.ReadUnicode(20);
                returnValue.playerData[1].name = reader.ReadUnicode(20);
                returnValue.playerData[2].name = reader.ReadUnicode(20);
                returnValue.playerData[3].name = reader.ReadUnicode(20);
                returnValue.StartLp = reader.ReadInt32();
                returnValue.StartHand = reader.ReadInt32();
                returnValue.DrawCount = reader.ReadInt32();
                returnValue.opt = reader.ReadUInt32();
                Program.I().ocgcore.MasterRule = (int)(returnValue.opt >> 16);
                for (int i = 0; i < 4; i++)
                {
                    int count = reader.ReadInt32();
                    for (int i2 = 0; i2 < count; i2++)
                    {
                        returnValue.playerData[i].main.Add(reader.ReadInt32());
                    }
                    count = reader.ReadInt32();
                    for (int i2 = 0; i2 < count; i2++)
                    {
                        returnValue.playerData[i].extra.Add(reader.ReadInt32());
                    }
                }
            }
            else
            {
                returnValue.playerData.Add(new Percy.YRP.PlayerData());
                returnValue.playerData.Add(new Percy.YRP.PlayerData());
                returnValue.playerData[0].name = reader.ReadUnicode(20);
                returnValue.playerData[1].name = reader.ReadUnicode(20);
                returnValue.StartLp = reader.ReadInt32();
                returnValue.StartHand = reader.ReadInt32();
                returnValue.DrawCount = reader.ReadInt32();
                returnValue.opt = reader.ReadUInt32();
                Program.I().ocgcore.MasterRule = (int)(returnValue.opt >> 16);
                for (int i = 0; i < 2; i++)
                {
                    int count = reader.ReadInt32();
                    for (int i2 = 0; i2 < count; i2++)
                    {
                        returnValue.playerData[i].main.Add(reader.ReadInt32());
                    }
                    count = reader.ReadInt32();
                    for (int i2 = 0; i2 < count; i2++)
                    {
                        returnValue.playerData[i].extra.Add(reader.ReadInt32());
                    }
                }
            }
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                returnValue.gameData.Add(reader.ReadBytes(reader.ReadByte()));
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
        return returnValue;
    }

    private void onYdk()
    {
        if (!superScrollView.Selected())
        {
            return;
        }
        try
        {
            ReplayExportResult exportResult = ScreenController.ExportDecksFromReplay(
                superScrollView.selectedString,
                BuildDeckContentsFromReplayBuffer);
            if (!exportResult.Succeeded)
            {
                ShowIncompleteDeckExportMessage();
                return;
            }

            for (int index = 0; index < exportResult.WrittenDisplayPaths.Count; index++)
            {
                RMSshow_none(InterString.Get("卡组入库：[?]", exportResult.WrittenDisplayPaths[index]));
            }
        }
        catch (Exception)
        {
            ShowIncompleteDeckExportMessage();
        }
    }

    private IList<string> BuildDeckContentsFromReplayBuffer(byte[] replayBuffer)
    {
        Percy.YRP yrp = getYRP(replayBuffer);
        if (yrp == null || yrp.playerData == null || yrp.playerData.Count == 0)
        {
            return new List<string>();
        }

        List<string> deckContents = new List<string>();
        for (int index = 0; index < yrp.playerData.Count; index++)
        {
            deckContents.Add(
                YGOSharp.YdkDeckSerializer.Serialize(
                    yrp.playerData[index].main,
                    yrp.playerData[index].extra,
                    new List<int>()));
        }

        return deckContents;
    }

    private void ShowIncompleteDeckExportMessage()
    {
        RMSshow_none(InterString.Get("录像没有录制完整。"));
        RMSshow_none(InterString.Get("MATCH局中可能只有最后一局决斗才包含卡组信息。"));
    }

    private void onYrp()
    {
        if (!superScrollView.Selected())
        {
            return;
        }
        try
        {
            ReplayExportResult exportResult = ScreenController.ExportLegacyReplays(superScrollView.selectedString);
            if (!exportResult.Succeeded)
            {
                ShowIncompleteReplayExportMessage();
                return;
            }

            for (int index = 0; index < exportResult.WrittenDisplayPaths.Count; index++)
            {
                RMSshow_none(InterString.Get("录像入库：[?]", exportResult.WrittenDisplayPaths[index]));
            }

            RefreshReplayList();
        }
        catch (Exception)
        {
            ShowIncompleteReplayExportMessage();
        }
    }

    private void ShowIncompleteReplayExportMessage()
    {
        RMSshow_none(InterString.Get("录像没有录制完整。"));
        RMSshow_none(InterString.Get("MATCH局中可能只有最后一局决斗才包含旧版录像信息。"));
    }

    public override void ES_RMS(string hashCode, List<messageSystemValue> result)
    {
        base.ES_RMS(hashCode, result);
        if (hashCode == "onRename")
        {
            ReplayOperationResult renameResult = ScreenController.RenameReplay(superScrollView.selectedString, result[0].value);
            if (renameResult.Succeeded)
            {
                RefreshReplayList();
                RMSshow_none(InterString.Get("重命名成功。"));
            }
            else
            {
                RMSshow_none(InterString.Get("重命名失败！请检查输入的文件名，以及文件夹权限。"));
            }
        }

        if (hashCode == "onDelete" && result[0].value == "yes")
        {
            ReplayDeleteResult deleteResult = ScreenController.DeleteReplay(superScrollView.selectedString);
            if (deleteResult.DeletedReplayRecord || deleteResult.DeletedLegacyReplay)
            {
                RMSshow_none(InterString.Get("[?]已经被删除。", superScrollView.selectedString));
                RefreshReplayList();
            }
        }

        if (hashCode == "onValue" && result[0].value == "yes")
        {
            ScreenController.DeleteUnnamedLegacyReplays();
            RMSshow_none(InterString.Get("清理完毕。"));
            RefreshReplayList();
        }
    }

    void onSelected()
    {
        if (selectedTrace == superScrollView.selectedString)
        {
            KF_replay(selectedTrace);
        }
        selectedTrace = superScrollView.selectedString;
    }

    public void KF_replay(string name, bool god = false)
    {
        try
        {
            if (!ScreenController.LaunchReplay(name, god, CreateReplayLaunchActions()))
            {
                return;
            }
        }
        catch (Exception)
        {
            ShowIncompleteReplayExportMessage();
        }
    }

    private void pushCollection(List<Package> collection)
    {
        Program.I().ocgcore.returnServant = Program.I().selectReplay;
        Program.I().ocgcore.handler = delegate { };
        Program.I().ocgcore.name_0 = Config.Get("name", "一秒一喵机会");
        Program.I().ocgcore.name_0_c = Program.I().ocgcore.name_0;
        Program.I().ocgcore.name_1 = "Percy AI";
        Program.I().ocgcore.name_0_tag = "---";
        Program.I().ocgcore.name_1_tag = "---";
        Program.I().ocgcore.timeLimit = 240;
        Program.I().ocgcore.lpLimit = 8000;
        Program.I().ocgcore.isFirst = true;
        Program.I().shiftToServant(Program.I().ocgcore);
        Program.I().ocgcore.InAI = false;
        Program.I().ocgcore.shiftCondition(Ocgcore.Condition.record);
        Program.I().ocgcore.flushPackages(collection);
    }

    private void ApplyLegacyShow()
    {
        base.show();
        RefreshReplayList();
        Program.charge();
    }

    private void ApplyLegacyHide()
    {
        base.hide();
    }

    void onClickExit()
    {
        ScreenController.Close(new ReplayCloseActions
        {
            ExitOnReturn = Program.exitOnReturn,
            ShowMenu = delegate { Program.I().shiftToServant(Program.I().menu); },
            ExitApplication = delegate { Program.I().menu.onClickExit(); }
        });
    }

    private ReplayLaunchActions CreateReplayLaunchActions()
    {
        return new ReplayLaunchActions
        {
            OpenReplayRecord = delegate(byte[] recordBuffer)
            {
                pushCollection(ReplayPackageCodec.Decode(recordBuffer));
            },
            ShowLegacyReplayNotice = delegate
            {
                RMSshow_none(InterString.Get("您正在观看旧版的录像（上帝视角），不保证稳定性。"));
            },
            OpenLegacyReplayBuffers = delegate(IList<byte[]> replayBuffers)
            {
                if (replayBuffers == null || replayBuffers.Count == 0)
                {
                    return;
                }

                if (precy != null)
                {
                    precy.dispose();
                }

                precy = new PrecyOcg();
                byte[] legacyReplayBuffer = replayBuffers[replayBuffers.Count - 1];
                List<Package> collections = ReplayPackageCodec.Decode(precy.ygopro.getYRP3dBuffer(getYRP(legacyReplayBuffer)));
                pushCollection(collections);
            }
        };
    }
}

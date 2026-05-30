using System;
using System.Collections.Generic;
using System.IO;
using App.Features.Replay.Services;
using App.UI.Common;
using App.UI.Screens.Replay;
using UnityEditor;
using UnityEngine;

public static class Task4ReplayBatchTest
{
    public static void Run()
    {
        int exitCode = 0;

        try
        {
            VerifyReplayListingAndFileOperations();
            VerifyReplayOpenRequests();
            VerifyReplayRouteRegistration();
            Debug.Log("Task4ReplayBatchTest OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            exitCode = 1;
        }
        finally
        {
            EditorApplication.Exit(exitCode);
        }
    }

    private static void VerifyReplayListingAndFileOperations()
    {
        FakeReplayStorage storage = new FakeReplayStorage();
        storage.ReplayFiles.Add(new FileInfo("/tmp/Beta.yrp"));
        storage.ReplayFiles.Add(new FileInfo("/tmp/Alpha.yrp3d"));

        ReplayFlowService flowService = new ReplayFlowService(storage.CreateCallbacks());
        ReplayListState timeList = flowService.LoadReplayList(
            "1",
            delegate(FileInfo left, FileInfo right) { return string.CompareOrdinal(right.Name, left.Name); },
            delegate(FileInfo left, FileInfo right) { return string.CompareOrdinal(left.Name, right.Name); });

        if (!timeList.SortByTime || timeList.DisplayNames.Count != 2 || timeList.DisplayNames[0] != "Beta.yrp" || timeList.DisplayNames[1] != "Alpha")
        {
            throw new Exception("ReplayFlowService did not build the replay list with the expected display names.");
        }

        ReplayListState nameList = flowService.LoadReplayList(
            "0",
            delegate(FileInfo left, FileInfo right) { return 0; },
            delegate(FileInfo left, FileInfo right) { return string.CompareOrdinal(left.Name, right.Name); });

        if (nameList.SortByTime || nameList.DisplayNames[0] != "Alpha" || nameList.DisplayNames[1] != "Beta.yrp")
        {
            throw new Exception("ReplayFlowService did not switch to name ordering.");
        }

        if (flowService.ToggleSort("1") != "0" || flowService.ToggleSort("0") != "1")
        {
            throw new Exception("ReplayFlowService did not toggle replay sort config values correctly.");
        }

        if (!flowService.RenameReplay("Beta.yrp", "Gamma").Succeeded || storage.MovedLegacyReplaySource != "Beta.yrp" || storage.MovedLegacyReplayTarget != "Gamma.yrp")
        {
            throw new Exception("ReplayFlowService did not rename legacy replay files through the injected storage.");
        }

        if (!flowService.RenameReplay("Alpha", "Omega").Succeeded || storage.MovedReplayRecordSource != "Alpha" || storage.MovedReplayRecordTarget != "Omega")
        {
            throw new Exception("ReplayFlowService did not rename replay record files through the injected storage.");
        }

        storage.RecordBytes["Alpha"] = new byte[] { 9, 9, 9 };
        ReplayDeleteResult deleteResult = flowService.DeleteReplay("Alpha");
        if (!deleteResult.DeletedReplayRecord || deleteResult.DeletedLegacyReplay)
        {
            throw new Exception("ReplayFlowService did not report replay record deletion correctly.");
        }

        storage.ReplayFiles.Add(new FileInfo("/tmp/12-34「67：89012345.yrp"));
        storage.ReplayFiles.Add(new FileInfo("/tmp/34-56「78：901234567890.yrp"));
        storage.LegacyReplayBytes["12-34「67：89012345.yrp"] = new byte[] { 1 };
        storage.LegacyReplayBytes["34-56「78：901234567890.yrp"] = new byte[] { 2 };
        ReplayCleanupResult cleanup = flowService.DeleteUnnamedLegacyReplays();
        if (cleanup.DeletedCount != 2)
        {
            throw new Exception("ReplayFlowService did not delete the expected unnamed legacy replays.");
        }

        storage.RecordBuffers["MatchA"] = new List<byte[]>
        {
            new byte[] { 1, 2 },
            new byte[] { 3, 4 }
        };
        ReplayExportResult replayExport = flowService.ExportLegacyReplays("MatchA");
        if (!replayExport.Succeeded || replayExport.WrittenDisplayPaths.Count != 2 || storage.WrittenLegacyReplayFiles.Count != 2)
        {
            throw new Exception("ReplayFlowService did not export legacy replay files from a replay record.");
        }

        ReplayExportResult deckExport = flowService.ExportDecks(
            "MatchA",
            new List<string> { "#deck", "#deck2" });
        if (!deckExport.Succeeded || deckExport.WrittenDisplayPaths.Count != 2 || storage.WrittenDeckFiles.Count != 2)
        {
            throw new Exception("ReplayFlowService did not export deck files with the expected names.");
        }

        byte[] deckExportSource;
        if (!flowService.TryGetDeckExportSource("MatchA", out deckExportSource) || deckExportSource[0] != 3)
        {
            throw new Exception("ReplayFlowService should prefer the final replay buffer for deck export.");
        }

        ReplayExportResult extractedDeckExport = flowService.ExportDecksFromReplay(
            "MatchA",
            delegate(byte[] replayBuffer)
            {
                return new List<string> { replayBuffer[0].ToString() };
            });
        if (!extractedDeckExport.Succeeded || storage.WrittenDeckFiles["MatchA_1.ydk"] != "3")
        {
            throw new Exception("ReplayFlowService did not export deck content from the selected replay buffer.");
        }
    }

    private static void VerifyReplayOpenRequests()
    {
        FakeReplayStorage storage = new FakeReplayStorage();
        storage.RecordBuffers["MatchB"] = new List<byte[]>
        {
            new byte[] { 9 },
            new byte[] { 8 }
        };
        storage.RecordBytes["MatchB"] = new byte[] { 7, 6, 5 };
        storage.LegacyReplayBytes["Legacy.yrp"] = new byte[] { 4, 3, 2, 1 };

        ReplayFlowService flowService = new ReplayFlowService(storage.CreateCallbacks());
        ReplayOpenRequest request;

        if (!flowService.TryCreateOpenRequest("MatchB", false, out request) || request.Mode != ReplayOpenMode.ReplayRecord || request.RecordBuffer.Length != 3)
        {
            throw new Exception("ReplayFlowService did not create the expected replay record open request.");
        }

        if (!flowService.TryCreateOpenRequest("MatchB", true, out request) || request.Mode != ReplayOpenMode.LegacyReplayBuffer || request.LegacyReplayBuffers.Count != 2)
        {
            throw new Exception("ReplayFlowService did not create the expected replay record legacy buffer request.");
        }

        if (!flowService.TryCreateOpenRequest("Legacy.yrp", false, out request) || request.Mode != ReplayOpenMode.LegacyReplayBuffer || request.LegacyReplayBuffers.Count != 1)
        {
            throw new Exception("ReplayFlowService did not create the expected legacy replay open request.");
        }

        bool openedRecord = false;
        bool openedLegacy = false;
        bool showedLegacyNotice = false;
        if (!flowService.LaunchReplay(
            "MatchB",
            true,
            new ReplayLaunchActions
            {
                OpenReplayRecord = delegate { openedRecord = true; },
                OpenLegacyReplayBuffers = delegate(IList<byte[]> buffers) { openedLegacy = buffers.Count == 2; },
                ShowLegacyReplayNotice = delegate { showedLegacyNotice = true; }
            })
            || openedRecord
            || !openedLegacy
            || !showedLegacyNotice)
        {
            throw new Exception("ReplayFlowService did not route legacy replay launch through the injected actions.");
        }
    }

    private static void VerifyReplayRouteRegistration()
    {
        if (AppUiRoot.HasCurrent)
        {
            UnityEngine.Object.DestroyImmediate(AppUiRoot.Current.gameObject);
        }

        AppUiRoot uiRoot = AppUiRoot.EnsureInstance();
        GameObject replayObject = new GameObject("Task4.Replay");
        bool replayShown = false;
        bool replayHidden = false;

        ReplayScreenController controller = new ReplayScreenController(new ReplayFlowService(new ReplayFlowService.Callbacks()));
        controller.Bind(
            replayObject,
            new ReplayFlowService(new ReplayFlowService.Callbacks()),
            delegate
            {
                replayShown = true;
                replayObject.SetActive(true);
            },
            delegate
            {
                replayHidden = true;
                replayObject.SetActive(false);
            });

        controller.SynchronizeLegacyShown();
        if (uiRoot.Router.CurrentRouteKey != ReplayScreenController.Route || !replayObject.activeSelf)
        {
            throw new Exception("ReplayScreenController did not register the replay route with the shared UI root.");
        }

        controller.SynchronizeLegacyHidden();
        if (replayHidden || uiRoot.Router.CurrentRouteKey != string.Empty)
        {
            throw new Exception("ReplayScreenController did not hide the current route silently.");
        }

        UnityEngine.Object.DestroyImmediate(replayObject);
        UnityEngine.Object.DestroyImmediate(uiRoot.gameObject);
    }

    private sealed class FakeReplayStorage
    {
        public readonly List<FileInfo> ReplayFiles = new List<FileInfo>();
        public readonly Dictionary<string, List<byte[]>> RecordBuffers = new Dictionary<string, List<byte[]>>(StringComparer.Ordinal);
        public readonly Dictionary<string, byte[]> RecordBytes = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        public readonly Dictionary<string, byte[]> LegacyReplayBytes = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        public readonly Dictionary<string, byte[]> WrittenLegacyReplayFiles = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        public readonly Dictionary<string, string> WrittenDeckFiles = new Dictionary<string, string>(StringComparer.Ordinal);

        public string MovedLegacyReplaySource;
        public string MovedLegacyReplayTarget;
        public string MovedReplayRecordSource;
        public string MovedReplayRecordTarget;

        public ReplayFlowService.Callbacks CreateCallbacks()
        {
            ReplayFlowService.Callbacks callbacks = new ReplayFlowService.Callbacks();
            callbacks.GetReplayFiles = delegate { return ReplayFiles.ToArray(); };
            callbacks.TryReadReplayRecordBuffers = delegate(string replayName, out List<byte[]> buffers)
            {
                return RecordBuffers.TryGetValue(replayName, out buffers);
            };
            callbacks.TryReadReplayRecord = delegate(string replayName, out byte[] buffer)
            {
                return RecordBytes.TryGetValue(replayName, out buffer);
            };
            callbacks.TryReadLegacyReplay = delegate(string fileName, out byte[] buffer)
            {
                return LegacyReplayBytes.TryGetValue(fileName, out buffer);
            };
            callbacks.MoveReplayFile = delegate(string sourceFileName, string targetFileName)
            {
                MovedLegacyReplaySource = sourceFileName;
                MovedLegacyReplayTarget = targetFileName;
            };
            callbacks.MoveReplayRecord = delegate(string sourceReplayName, string targetReplayName)
            {
                MovedReplayRecordSource = sourceReplayName;
                MovedReplayRecordTarget = targetReplayName;
            };
            callbacks.DeleteReplayFile = delegate(string fileName)
            {
                return LegacyReplayBytes.Remove(fileName);
            };
            callbacks.DeleteReplayRecord = delegate(string replayName)
            {
                return RecordBytes.Remove(replayName) || RecordBuffers.Remove(replayName);
            };
            callbacks.WriteReplayFile = delegate(string fileName, byte[] buffer)
            {
                WrittenLegacyReplayFiles[fileName] = buffer;
            };
            callbacks.WriteDeckFile = delegate(string fileName, string contents)
            {
                WrittenDeckFiles[fileName] = contents;
            };
            callbacks.GetReplayDisplayPath = delegate(string fileName) { return "replay/" + fileName; };
            callbacks.GetDeckDisplayPath = delegate(string fileName) { return "deck/" + fileName; };
            return callbacks;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using App.Core;
using App.Features.Replay.Services;
using App.Platform;
using YGOSharp.OCGWrapper.Enums;

public static class ReplayLegacyBindings
{
    private const string DeckDirectoryName = "deck";

    public static ReplayFlowService CreateFlowService()
    {
        return new ReplayFlowService(CreateCallbacks());
    }

    public static ReplayFlowService.Callbacks CreateCallbacks()
    {
        IFileStorage storage = new RuntimeFileStorage();
        IPlatformPaths paths = new RuntimePlatformPaths();

        ReplayFlowService.Callbacks callbacks = new ReplayFlowService.Callbacks();
        callbacks.GetReplayFiles = RuntimeReplayFile.GetReplayFiles;
        callbacks.TryReadReplayRecordBuffers = TryReadReplayRecordBuffers;
        callbacks.TryReadReplayRecord = RuntimeReplayFile.TryReadReplayRecord;
        callbacks.TryReadLegacyReplay = RuntimeReplayFile.TryReadReplayFile;
        callbacks.MoveReplayFile = RuntimeReplayFile.MoveReplayFile;
        callbacks.MoveReplayRecord = delegate(string sourceReplayName, string destinationReplayName)
        {
            RuntimeReplayFile.MoveReplayRecord(sourceReplayName, destinationReplayName, false);
        };
        callbacks.DeleteReplayFile = RuntimeReplayFile.DeleteReplayFileIfExists;
        callbacks.DeleteReplayRecord = RuntimeReplayFile.DeleteReplayRecordIfExists;
        callbacks.WriteReplayFile = RuntimeReplayFile.WriteReplayFile;
        callbacks.WriteDeckFile = delegate(string fileName, string contents)
        {
            EnsureDirectory(paths.GetDirectoryPath(DeckDirectoryName));
            storage.WriteAllText(paths.GetFilePath(DeckDirectoryName, fileName), contents);
        };
        callbacks.GetReplayDisplayPath = RuntimeReplayFile.GetReplayDisplayPath;
        callbacks.GetDeckDisplayPath = delegate(string fileName)
        {
            return DeckDirectoryName + "/" + fileName;
        };
        return callbacks;
    }

    public static Comparison<FileInfo> CreateTimeComparison()
    {
        return UIHelper.CompareTime;
    }

    public static Comparison<FileInfo> CreateNameComparison()
    {
        return UIHelper.CompareName;
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static bool TryReadReplayRecordBuffers(string replayName, out List<byte[]> replays)
    {
        replays = null;

        byte[] recordBuffer;
        if (!RuntimeReplayFile.TryReadReplayRecord(replayName, out recordBuffer) || recordBuffer == null)
        {
            return false;
        }

        List<byte[]> buffers = new List<byte[]>();
        try
        {
            List<Package> collection = ReplayPackageCodec.Decode(recordBuffer);
            for (int index = 0; index < collection.Count; index++)
            {
                Package item = collection[index];
                if (item.Fuction == (int)GameMessage.sibyl_replay)
                {
                    buffers.Add(item.Data.reader.ReadToEnd());
                }
            }
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.Log(exception);
            return false;
        }

        replays = buffers;
        return true;
    }
}

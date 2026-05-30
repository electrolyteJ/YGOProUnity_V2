using System;
using System.Collections.Generic;
using System.IO;

namespace App.Features.Replay.Services
{
    public enum ReplayOpenMode
    {
        None = 0,
        ReplayRecord = 1,
        LegacyReplayBuffer = 2,
    }

    public sealed class ReplayListState
    {
        public ReplayListState()
        {
            DisplayNames = new List<string>();
        }

        public bool SortByTime { get; set; }

        public List<string> DisplayNames { get; private set; }
    }

    public sealed class ReplayOperationResult
    {
        public bool Succeeded { get; set; }
    }

    public sealed class ReplayDeleteResult
    {
        public bool DeletedReplayRecord { get; set; }

        public bool DeletedLegacyReplay { get; set; }

        public bool DeletedAny
        {
            get { return DeletedReplayRecord || DeletedLegacyReplay; }
        }
    }

    public sealed class ReplayCleanupResult
    {
        public int DeletedCount { get; set; }
    }

    public sealed class ReplayExportResult
    {
        public ReplayExportResult()
        {
            WrittenDisplayPaths = new List<string>();
        }

        public bool Succeeded { get; set; }

        public List<string> WrittenDisplayPaths { get; private set; }
    }

    public sealed class ReplayOpenRequest
    {
        public ReplayOpenRequest()
        {
            LegacyReplayBuffers = new List<byte[]>();
        }

        public ReplayOpenMode Mode { get; set; }

        public byte[] RecordBuffer { get; set; }

        public List<byte[]> LegacyReplayBuffers { get; private set; }
    }

    public sealed class ReplayCloseActions
    {
        public bool ExitOnReturn { get; set; }

        public Action ShowMenu { get; set; }

        public Action ExitApplication { get; set; }
    }

    public sealed class ReplayLaunchActions
    {
        public Action<byte[]> OpenReplayRecord { get; set; }

        public Action<IList<byte[]>> OpenLegacyReplayBuffers { get; set; }

        public Action ShowLegacyReplayNotice { get; set; }
    }

    public sealed class ReplayFlowService
    {
        public const string SortPreferenceConfigKey = "sortByTimeReplay";
        public const string EnabledSortValue = "1";
        public const string DisabledSortValue = "0";
        public const string LegacyReplayExtension = ".yrp";
        public const string ReplayRecordExtension = ".yrp3d";

        public delegate bool TryReadReplayBytes(string replayName, out byte[] buffer);
        public delegate bool TryReadReplayBuffers(string replayName, out List<byte[]> buffers);

        public sealed class Callbacks
        {
            public Func<FileInfo[]> GetReplayFiles;
            public TryReadReplayBuffers TryReadReplayRecordBuffers;
            public TryReadReplayBytes TryReadReplayRecord;
            public TryReadReplayBytes TryReadLegacyReplay;
            public Action<string, string> MoveReplayFile;
            public Action<string, string> MoveReplayRecord;
            public Func<string, bool> DeleteReplayFile;
            public Func<string, bool> DeleteReplayRecord;
            public Action<string, byte[]> WriteReplayFile;
            public Action<string, string> WriteDeckFile;
            public Func<string, string> GetReplayDisplayPath;
            public Func<string, string> GetDeckDisplayPath;
        }

        private readonly Callbacks callbacks;

        public ReplayFlowService()
            : this(new Callbacks())
        {
        }

        public ReplayFlowService(Callbacks callbacks)
        {
            this.callbacks = callbacks ?? new Callbacks();
        }

        public ReplayListState LoadReplayList(string sortPreferenceValue, Comparison<FileInfo> timeComparison, Comparison<FileInfo> nameComparison)
        {
            ReplayListState state = new ReplayListState();
            state.SortByTime = IsSortByTimeEnabled(sortPreferenceValue);

            FileInfo[] files = callbacks.GetReplayFiles != null ? callbacks.GetReplayFiles() : null;
            if (files == null || files.Length == 0)
            {
                return state;
            }

            Comparison<FileInfo> comparison = state.SortByTime ? timeComparison : nameComparison;
            if (comparison != null)
            {
                Array.Sort(files, comparison);
            }

            for (int index = 0; index < files.Length; index++)
            {
                string displayName = TryGetDisplayName(files[index]);
                if (!string.IsNullOrEmpty(displayName))
                {
                    state.DisplayNames.Add(displayName);
                }
            }

            return state;
        }

        public string ToggleSort(string currentValue)
        {
            return IsSortByTimeEnabled(currentValue) ? DisabledSortValue : EnabledSortValue;
        }

        public bool IsSortByTimeEnabled(string sortPreferenceValue)
        {
            return string.Equals(sortPreferenceValue, EnabledSortValue, StringComparison.Ordinal);
        }

        public bool IsLegacyReplayFileName(string value)
        {
            return HasReplayExtension(value, LegacyReplayExtension);
        }

        public string GetRenameInputValue(string selectedReplay)
        {
            if (IsLegacyReplayFileName(selectedReplay))
            {
                return selectedReplay.Substring(0, selectedReplay.Length - LegacyReplayExtension.Length);
            }

            return selectedReplay ?? string.Empty;
        }

        public ReplayOperationResult RenameReplay(string selectedReplay, string targetName)
        {
            ReplayOperationResult result = new ReplayOperationResult();
            if (string.IsNullOrEmpty(selectedReplay) || string.IsNullOrEmpty(targetName))
            {
                return result;
            }

            try
            {
                if (IsLegacyReplayFileName(selectedReplay))
                {
                    Invoke(callbacks.MoveReplayFile, selectedReplay, targetName + LegacyReplayExtension);
                }
                else
                {
                    Invoke(callbacks.MoveReplayRecord, selectedReplay, targetName);
                }

                result.Succeeded = true;
            }
            catch
            {
                result.Succeeded = false;
            }

            return result;
        }

        public ReplayDeleteResult DeleteReplay(string selectedReplay)
        {
            ReplayDeleteResult result = new ReplayDeleteResult();
            if (string.IsNullOrEmpty(selectedReplay))
            {
                return result;
            }

            result.DeletedReplayRecord = InvokeDelete(callbacks.DeleteReplayRecord, selectedReplay);
            result.DeletedLegacyReplay = InvokeDelete(callbacks.DeleteReplayFile, selectedReplay);
            return result;
        }

        public ReplayCleanupResult DeleteUnnamedLegacyReplays()
        {
            ReplayCleanupResult result = new ReplayCleanupResult();
            FileInfo[] files = callbacks.GetReplayFiles != null ? callbacks.GetReplayFiles() : null;
            if (files == null || files.Length == 0)
            {
                return result;
            }

            for (int index = 0; index < files.Length; index++)
            {
                string fileName = files[index].Name;
                if (IsLegacyReplayFileName(fileName)
                    && IsUnnamedLegacyReplay(fileName)
                    && InvokeDelete(callbacks.DeleteReplayFile, fileName))
                {
                    result.DeletedCount++;
                }
            }

            return result;
        }

        public ReplayExportResult ExportLegacyReplays(string selectedReplay)
        {
            ReplayExportResult result = new ReplayExportResult();
            List<byte[]> replayBuffers;
            if (!TryReadReplayRecordBuffers(selectedReplay, out replayBuffers) || replayBuffers.Count == 0 || callbacks.WriteReplayFile == null)
            {
                return result;
            }

            try
            {
                for (int index = 0; index < replayBuffers.Count; index++)
                {
                    string fileName = selectedReplay + "-Game" + (index + 1).ToString() + LegacyReplayExtension;
                    callbacks.WriteReplayFile(fileName, replayBuffers[index]);
                    result.WrittenDisplayPaths.Add(GetReplayDisplayPath(fileName));
                }

                result.Succeeded = true;
            }
            catch
            {
                result.Succeeded = false;
                result.WrittenDisplayPaths.Clear();
            }

            return result;
        }

        public ReplayExportResult ExportDecks(string selectedReplay, IList<string> deckContents)
        {
            ReplayExportResult result = new ReplayExportResult();
            if (string.IsNullOrEmpty(selectedReplay) || deckContents == null || deckContents.Count == 0 || callbacks.WriteDeckFile == null)
            {
                return result;
            }

            try
            {
                for (int index = 0; index < deckContents.Count; index++)
                {
                    string fileName = selectedReplay + "_" + (index + 1).ToString() + ".ydk";
                    callbacks.WriteDeckFile(fileName, deckContents[index]);
                    result.WrittenDisplayPaths.Add(GetDeckDisplayPath(fileName));
                }

                result.Succeeded = true;
            }
            catch
            {
                result.Succeeded = false;
                result.WrittenDisplayPaths.Clear();
            }

            return result;
        }

        public ReplayExportResult ExportDecksFromReplay(string selectedReplay, Func<byte[], IList<string>> buildDeckContents)
        {
            ReplayExportResult result = new ReplayExportResult();
            if (buildDeckContents == null)
            {
                return result;
            }

            byte[] replayBuffer;
            if (!TryGetDeckExportSource(selectedReplay, out replayBuffer))
            {
                return result;
            }

            IList<string> deckContents = buildDeckContents(replayBuffer);
            if (deckContents == null || deckContents.Count == 0)
            {
                return result;
            }

            return ExportDecks(selectedReplay, deckContents);
        }

        public bool TryGetDeckExportSource(string selectedReplay, out byte[] replayBuffer)
        {
            replayBuffer = null;
            if (string.IsNullOrEmpty(selectedReplay))
            {
                return false;
            }

            if (TryReadLegacyReplay(selectedReplay, out replayBuffer))
            {
                return true;
            }

            List<byte[]> replayBuffers;
            if (TryReadReplayRecordBuffers(selectedReplay, out replayBuffers) && replayBuffers.Count > 0)
            {
                replayBuffer = replayBuffers[replayBuffers.Count - 1];
                return true;
            }

            return false;
        }

        public bool LaunchReplay(string selectedReplay, bool preferLegacyReplayBuffer, ReplayLaunchActions actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            ReplayOpenRequest request;
            if (!TryCreateOpenRequest(selectedReplay, preferLegacyReplayBuffer, out request))
            {
                return false;
            }

            if (request.Mode == ReplayOpenMode.ReplayRecord)
            {
                Invoke(actions.OpenReplayRecord, request.RecordBuffer);
                return true;
            }

            if (request.Mode == ReplayOpenMode.LegacyReplayBuffer && request.LegacyReplayBuffers.Count > 0)
            {
                Invoke(actions.ShowLegacyReplayNotice);
                Invoke(actions.OpenLegacyReplayBuffers, request.LegacyReplayBuffers);
                return true;
            }

            return false;
        }

        public void Close(ReplayCloseActions actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            if (actions.ExitOnReturn)
            {
                Invoke(actions.ExitApplication);
                return;
            }

            Invoke(actions.ShowMenu);
        }

        public bool TryCreateOpenRequest(string selectedReplay, bool preferLegacyReplayBuffer, out ReplayOpenRequest request)
        {
            request = null;
            if (string.IsNullOrEmpty(selectedReplay))
            {
                return false;
            }

            List<byte[]> replayBuffers;
            if (TryReadReplayRecordBuffers(selectedReplay, out replayBuffers) && replayBuffers.Count > 0)
            {
                if (preferLegacyReplayBuffer)
                {
                    request = CreateLegacyReplayRequest(replayBuffers);
                    return true;
                }

                byte[] recordBuffer;
                if (TryReadReplayRecord(selectedReplay, out recordBuffer))
                {
                    request = new ReplayOpenRequest();
                    request.Mode = ReplayOpenMode.ReplayRecord;
                    request.RecordBuffer = recordBuffer;
                    return true;
                }

                return false;
            }

            byte[] legacyReplayBuffer;
            if (IsLegacyReplayFileName(selectedReplay) && TryReadLegacyReplay(selectedReplay, out legacyReplayBuffer))
            {
                request = CreateLegacyReplayRequest(new List<byte[]>
                {
                    legacyReplayBuffer
                });
                return true;
            }

            return false;
        }

        private string TryGetDisplayName(FileInfo fileInfo)
        {
            if (fileInfo == null)
            {
                return string.Empty;
            }

            if (HasReplayExtension(fileInfo.Name, ReplayRecordExtension))
            {
                return fileInfo.Name.Substring(0, fileInfo.Name.Length - ReplayRecordExtension.Length);
            }

            if (HasReplayExtension(fileInfo.Name, LegacyReplayExtension))
            {
                return fileInfo.Name;
            }

            return string.Empty;
        }

        private static bool HasReplayExtension(string value, string extension)
        {
            return !string.IsNullOrEmpty(value)
                   && value.Length > extension.Length
                   && value.EndsWith(extension, StringComparison.Ordinal);
        }

        private static bool IsUnnamedLegacyReplay(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            if (fileName.Length != 21 && fileName.Length != 25)
            {
                return false;
            }

            return fileName.Length > 8
                   && fileName[2] == '-'
                   && fileName[5] == '\u300c'
                   && fileName[8] == '\uff1a';
        }

        private bool TryReadReplayRecordBuffers(string selectedReplay, out List<byte[]> replayBuffers)
        {
            replayBuffers = null;
            if (callbacks.TryReadReplayRecordBuffers == null)
            {
                return false;
            }

            return callbacks.TryReadReplayRecordBuffers(selectedReplay, out replayBuffers) && replayBuffers != null;
        }

        private bool TryReadReplayRecord(string selectedReplay, out byte[] replayRecord)
        {
            replayRecord = null;
            return callbacks.TryReadReplayRecord != null
                   && callbacks.TryReadReplayRecord(selectedReplay, out replayRecord)
                   && replayRecord != null;
        }

        private bool TryReadLegacyReplay(string selectedReplay, out byte[] replayBuffer)
        {
            replayBuffer = null;
            return callbacks.TryReadLegacyReplay != null
                   && callbacks.TryReadLegacyReplay(selectedReplay, out replayBuffer)
                   && replayBuffer != null;
        }

        private ReplayOpenRequest CreateLegacyReplayRequest(List<byte[]> replayBuffers)
        {
            ReplayOpenRequest request = new ReplayOpenRequest();
            request.Mode = ReplayOpenMode.LegacyReplayBuffer;
            for (int index = 0; index < replayBuffers.Count; index++)
            {
                request.LegacyReplayBuffers.Add(replayBuffers[index]);
            }

            return request;
        }

        private string GetReplayDisplayPath(string fileName)
        {
            if (callbacks.GetReplayDisplayPath != null)
            {
                return callbacks.GetReplayDisplayPath(fileName);
            }

            return fileName;
        }

        private string GetDeckDisplayPath(string fileName)
        {
            if (callbacks.GetDeckDisplayPath != null)
            {
                return callbacks.GetDeckDisplayPath(fileName);
            }

            return fileName;
        }

        private static bool InvokeDelete(Func<string, bool> deleteCallback, string name)
        {
            if (deleteCallback == null)
            {
                return false;
            }

            try
            {
                return deleteCallback(name);
            }
            catch
            {
                return false;
            }
        }

        private static void Invoke(Action<string, string> callback, string firstValue, string secondValue)
        {
            if (callback == null)
            {
                throw new InvalidOperationException("Replay callback was not configured.");
            }

            callback(firstValue, secondValue);
        }

        private static void Invoke(Action action)
        {
            if (action != null)
            {
                action();
            }
        }

        private static void Invoke(Action<byte[]> action, byte[] buffer)
        {
            if (action != null)
            {
                action(buffer);
            }
        }

        private static void Invoke(Action<IList<byte[]>> action, IList<byte[]> buffers)
        {
            if (action != null)
            {
                action(buffers);
            }
        }
    }
}

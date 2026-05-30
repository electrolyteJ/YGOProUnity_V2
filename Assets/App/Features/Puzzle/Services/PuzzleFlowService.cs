using System;
using System.IO;
using App.Core;
using App.Platform;

namespace App.Features.Puzzle.Services
{
    public sealed class PuzzleFlowService
    {
        public const string PuzzleDirectoryName = "puzzle";
        public const string PuzzleScriptExtension = ".lua";

        private readonly IPlatformPaths platformPaths;
        private readonly PuzzleFlowCallbacks callbacks;

        public PuzzleFlowService()
            : this(new RuntimePlatformPaths(), new PuzzleFlowCallbacks())
        {
        }

        public PuzzleFlowService(IPlatformPaths platformPaths, PuzzleFlowCallbacks callbacks)
        {
            if (platformPaths == null)
            {
                throw new ArgumentNullException("platformPaths");
            }

            this.platformPaths = platformPaths;
            this.callbacks = callbacks ?? new PuzzleFlowCallbacks();
        }

        public PuzzleListState LoadPuzzleList(Comparison<FileInfo> nameComparison)
        {
            PuzzleListState state = new PuzzleListState();
            FileInfo[] fileInfos = GetPuzzleFiles();
            if (fileInfos == null || fileInfos.Length == 0)
            {
                return state;
            }

            FileInfo[] orderedFiles = (FileInfo[])fileInfos.Clone();
            if (nameComparison != null)
            {
                Array.Sort(orderedFiles, nameComparison);
            }

            for (int index = 0; index < orderedFiles.Length; index++)
            {
                string displayName = TryGetPuzzleDisplayName(orderedFiles[index]);
                if (!string.IsNullOrEmpty(displayName))
                {
                    state.DisplayNames.Add(displayName);
                }
            }

            return state;
        }

        public PuzzleSelectionResult HandleSelection(bool isVisible, string previousSelection, string currentSelection)
        {
            PuzzleSelectionResult result = new PuzzleSelectionResult();
            result.SelectedName = currentSelection ?? string.Empty;

            if (!isVisible || string.IsNullOrEmpty(currentSelection))
            {
                return result;
            }

            if (string.Equals(previousSelection, currentSelection, StringComparison.Ordinal))
            {
                result.ShouldLaunch = true;
                result.PuzzlePath = GetPuzzlePath(currentSelection);
            }

            return result;
        }

        public string GetPuzzlePath(string puzzleName)
        {
            return platformPaths.GetFilePath(PuzzleDirectoryName, (puzzleName ?? string.Empty) + PuzzleScriptExtension);
        }

        public void Close(PuzzleCloseActions actions)
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

        private FileInfo[] GetPuzzleFiles()
        {
            if (callbacks.GetPuzzleFiles != null)
            {
                return callbacks.GetPuzzleFiles();
            }

            string directoryPath = platformPaths.GetDirectoryPath(PuzzleDirectoryName);
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                return new FileInfo[0];
            }

            return new DirectoryInfo(directoryPath).GetFiles();
        }

        private static string TryGetPuzzleDisplayName(FileInfo fileInfo)
        {
            if (fileInfo == null)
            {
                return string.Empty;
            }

            string fileName = fileInfo.Name;
            if (!IsPuzzleScriptFile(fileName))
            {
                return string.Empty;
            }

            return fileName.Substring(0, fileName.Length - PuzzleScriptExtension.Length);
        }

        private static bool IsPuzzleScriptFile(string fileName)
        {
            return !string.IsNullOrEmpty(fileName)
                && fileName.Length > PuzzleScriptExtension.Length
                && fileName.EndsWith(PuzzleScriptExtension, StringComparison.Ordinal);
        }

        private static void Invoke(Action action)
        {
            if (action != null)
            {
                action();
            }
        }
    }
}

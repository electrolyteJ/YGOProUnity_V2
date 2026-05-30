using System;
using System.Collections.Generic;
using App.Core;
using App.Platform;

namespace App.Features.Deck.Services
{
    public enum DeckSortMode
    {
        ByTime = 0,
        ByName = 1,
    }

    public sealed class DeckEditorLaunchRequest
    {
        public DeckEditorLaunchRequest(string deckName, string deckPath)
        {
            DeckName = deckName ?? string.Empty;
            DeckPath = deckPath ?? string.Empty;
        }

        public string DeckName { get; private set; }

        public string DeckPath { get; private set; }
    }

    public sealed class DeckCloseActions
    {
        public bool ExitOnReturn { get; set; }

        public Action ShowMenu { get; set; }

        public Action ExitApplication { get; set; }
    }

    public sealed class DeckEditorReturnActions
    {
        public Func<bool> HasUnsavedChanges { get; set; }

        public Func<bool> SaveChanges { get; set; }

        public Action ShowSavePrompt { get; set; }

        public Action ReturnToDeckList { get; set; }
    }

    public sealed class DeckEditorOpenActions
    {
        public Action<string> SetDeckInUse { get; set; }

        public Action EnterEditMode { get; set; }

        public Action ShowEditor { get; set; }

        public Action<string> LoadDeck { get; set; }

        public Action<string> SetTitle { get; set; }

        public Action ApplyLayout { get; set; }

        public Action<Action> SetReturnAction { get; set; }

        public DeckEditorReturnActions ReturnActions { get; set; }
    }

    public sealed class DeckSaveRequest
    {
        public string DeckName { get; set; }

        public int MainCount { get; set; }

        public int ExtraCount { get; set; }

        public int SideCount { get; set; }

        public string SerializedDeck { get; set; }
    }

    public sealed class DeckSaveResult
    {
        public bool Succeeded { get; set; }

        public bool ExceededLimit { get; set; }
    }

    public sealed class DeckFlowService
    {
        private const string DeckDirectoryName = "deck";
        private const string DeckFileExtension = ".ydk";

        private readonly IPlatformPaths platformPaths;
        private readonly IDeckFileStorage fileStorage;

        public DeckFlowService()
            : this(new RuntimePlatformPaths(), new RuntimeFileStorage())
        {
        }

        public DeckFlowService(IPlatformPaths platformPaths, IDeckFileStorage fileStorage)
        {
            if (platformPaths == null)
            {
                throw new ArgumentNullException("platformPaths");
            }

            if (fileStorage == null)
            {
                throw new ArgumentNullException("fileStorage");
            }

            this.platformPaths = platformPaths;
            this.fileStorage = fileStorage;
        }

        public string[] GetDeckNames(string currentDeckName, string searchText, DeckSortMode sortMode)
        {
            FileStorageEntry[] fileEntries = GetDeckFiles();
            Array.Sort(fileEntries, sortMode == DeckSortMode.ByName ? CompareName : CompareTime);

            List<string> prioritized = new List<string>(fileEntries.Length);
            List<string> remaining = new List<string>(fileEntries.Length);
            for (int index = 0; index < fileEntries.Length; index++)
            {
                FileStorageEntry fileEntry = fileEntries[index];
                if (!IsDeckFileName(fileEntry.Name) || !MatchesSearch(fileEntry.Name, searchText))
                {
                    continue;
                }

                string deckName = GetDeckDisplayName(fileEntry.Name);
                if (string.Equals(deckName, currentDeckName, StringComparison.Ordinal))
                {
                    prioritized.Add(deckName);
                }
                else
                {
                    remaining.Add(deckName);
                }
            }

            prioritized.AddRange(remaining);
            return prioritized.ToArray();
        }

        public string GetDeckPath(string deckName)
        {
            return platformPaths.GetFilePath(DeckDirectoryName, GetDeckFileName(deckName));
        }

        public bool DeckExists(string deckName)
        {
            return fileStorage.FileExists(GetDeckPath(deckName));
        }

        public void CreateDeck(string deckName)
        {
            EnsureDeckDirectory();
            string path = GetDeckPath(deckName);
            if (!fileStorage.FileExists(path))
            {
                fileStorage.WriteAllText(path, string.Empty);
            }
        }

        public void DeleteDeck(string deckName)
        {
            string path = GetDeckPath(deckName);
            if (fileStorage.FileExists(path))
            {
                fileStorage.DeleteFile(path);
            }
        }

        public void CopyDeck(string sourceDeckName, string targetDeckName)
        {
            EnsureDeckDirectory();
            fileStorage.CopyFile(GetDeckPath(sourceDeckName), GetDeckPath(targetDeckName));
        }

        public void RenameDeck(string sourceDeckName, string targetDeckName)
        {
            EnsureDeckDirectory();
            fileStorage.MoveFile(GetDeckPath(sourceDeckName), GetDeckPath(targetDeckName));
        }

        public string GetUniqueDeckName(string baseDeckName)
        {
            string deckName = baseDeckName;
            int index = 1;
            while (DeckExists(deckName))
            {
                deckName = baseDeckName + index.ToString();
                index++;
            }

            return deckName;
        }

        public bool TryCreateEditorLaunchRequest(string deckName, out DeckEditorLaunchRequest request)
        {
            if (!DeckExists(deckName))
            {
                request = null;
                return false;
            }

            request = new DeckEditorLaunchRequest(deckName, GetDeckPath(deckName));
            return true;
        }

        public bool TryOpenEditor(string deckName, DeckEditorOpenActions actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            DeckEditorLaunchRequest request;
            if (!TryCreateEditorLaunchRequest(deckName, out request))
            {
                return false;
            }

            Invoke(actions.SetDeckInUse, request.DeckName);
            Invoke(actions.EnterEditMode);
            Invoke(actions.ShowEditor);
            Invoke(actions.LoadDeck, request.DeckName);
            Invoke(actions.SetTitle, request.DeckName);
            Invoke(actions.ApplyLayout);
            Invoke(actions.SetReturnAction, CreateReturnAction(actions.ReturnActions));
            return true;
        }

        public void HandleReturnDialogResult(string resultValue, DeckEditorReturnActions actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            if (string.Equals(resultValue, "yes", StringComparison.Ordinal))
            {
                if (actions.SaveChanges != null && actions.SaveChanges())
                {
                    Invoke(actions.ReturnToDeckList);
                }
                return;
            }

            if (string.Equals(resultValue, "no", StringComparison.Ordinal))
            {
                Invoke(actions.ReturnToDeckList);
            }
        }

        public DeckSaveResult SaveDeck(DeckSaveRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            DeckSaveResult result = new DeckSaveResult();
            if (request.MainCount > 60 || request.ExtraCount > 15 || request.SideCount > 15)
            {
                result.ExceededLimit = true;
                return result;
            }

            SaveSerializedDeck(request.DeckName, request.SerializedDeck);
            result.Succeeded = true;
            return result;
        }

        public void SaveSerializedDeck(string deckName, string serializedDeck)
        {
            EnsureDeckDirectory();
            fileStorage.WriteAllText(GetDeckPath(deckName), serializedDeck ?? string.Empty);
        }

        public void Close(DeckCloseActions actions)
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

        private FileStorageEntry[] GetDeckFiles()
        {
            string directoryPath = platformPaths.GetDirectoryPath(DeckDirectoryName);
            if (!fileStorage.DirectoryExists(directoryPath))
            {
                return new FileStorageEntry[0];
            }

            return fileStorage.GetFiles(directoryPath);
        }

        private void EnsureDeckDirectory()
        {
            fileStorage.CreateDirectory(platformPaths.GetDirectoryPath(DeckDirectoryName));
        }

        private static string GetDeckFileName(string deckName)
        {
            return (deckName ?? string.Empty) + DeckFileExtension;
        }

        private static bool IsDeckFileName(string fileName)
        {
            return !string.IsNullOrEmpty(fileName)
                   && fileName.Length > DeckFileExtension.Length
                   && fileName.EndsWith(DeckFileExtension, StringComparison.Ordinal);
        }

        private static string GetDeckDisplayName(string fileName)
        {
            return fileName.Substring(0, fileName.Length - DeckFileExtension.Length);
        }

        private static bool MatchesSearch(string fileName, string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                return true;
            }

            return fileName != null
                   && fileName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int CompareTime(FileStorageEntry left, FileStorageEntry right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            return right.LastWriteTimeUtc.CompareTo(left.LastWriteTimeUtc);
        }

        private static int CompareName(FileStorageEntry left, FileStorageEntry right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            return string.CompareOrdinal(left.FullPath, right.FullPath);
        }

        private static void Invoke(Action action)
        {
            if (action != null)
            {
                action();
            }
        }

        private static void Invoke(Action<string> action, string value)
        {
            if (action != null)
            {
                action(value);
            }
        }

        private static void Invoke(Action<Action> action, Action value)
        {
            if (action != null)
            {
                action(value);
            }
        }

        private static Action CreateReturnAction(DeckEditorReturnActions actions)
        {
            if (actions == null)
            {
                return null;
            }

            return delegate
            {
                if (actions.HasUnsavedChanges != null && actions.HasUnsavedChanges())
                {
                    Invoke(actions.ShowSavePrompt);
                    return;
                }

                Invoke(actions.ReturnToDeckList);
            };
        }
    }
}

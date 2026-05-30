using System;
using System.Collections.Generic;
using App.Core;
using App.Features.Deck.Services;
using App.UI.Common;
using App.UI.Common.Navigation;
using App.UI.Screens.Deck;
using UnityEditor;
using UnityEngine;

public static class Task4DeckBatchTest
{
    public static void Run()
    {
        int exitCode = 0;

        try
        {
            VerifyDeckListingAndCrud();
            VerifyDeckControllerCloseBehavior();
            Debug.Log("Task4DeckBatchTest OK");
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

    private static void VerifyDeckListingAndCrud()
    {
        TestPaths paths = new TestPaths("/task4");
        TestStorage storage = new TestStorage();
        DeckFlowService service = new DeckFlowService(paths, storage);

        storage.SeedFile(service.GetDeckPath("alpha"), "#created by test", new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        storage.SeedFile(service.GetDeckPath("beta"), "#created by test", new DateTime(2025, 1, 3, 0, 0, 0, DateTimeKind.Utc));
        storage.SeedFile(service.GetDeckPath("literal[deck]"), "#special", new DateTime(2025, 1, 4, 0, 0, 0, DateTimeKind.Utc));

        string[] byName = service.GetDeckNames("beta", string.Empty, DeckSortMode.ByName);
        if (byName.Length != 3 || byName[0] != "beta" || byName[1] != "alpha" || byName[2] != "literal[deck]")
        {
            throw new Exception("DeckFlowService did not prioritize the selected deck in name sort through IFileStorage.");
        }

        string[] filtered = service.GetDeckNames("beta", "alp", DeckSortMode.ByTime);
        if (filtered.Length != 1 || filtered[0] != "alpha")
        {
            throw new Exception("DeckFlowService search filtering did not preserve legacy behavior through IFileStorage.");
        }

        string[] specialCharacterSearch = service.GetDeckNames("beta", "[", DeckSortMode.ByTime);
        if (specialCharacterSearch.Length != 1 || specialCharacterSearch[0] != "literal[deck]")
        {
            throw new Exception("DeckFlowService should treat special-character search text as plain text through IFileStorage.");
        }

        if (service.GetUniqueDeckName("beta") != "beta1")
        {
            throw new Exception("DeckFlowService unique naming no longer appends an index.");
        }

        service.CreateDeck("gamma");
        if (!storage.FileExists(service.GetDeckPath("gamma")))
        {
            throw new Exception("DeckFlowService did not create a new deck file through IFileStorage.");
        }

        service.CopyDeck("alpha", "alphaCopy");
        if (!storage.FileExists(service.GetDeckPath("alphaCopy"))
            || storage.ReadAllText(service.GetDeckPath("alphaCopy")) != "#created by test")
        {
            throw new Exception("DeckFlowService did not copy a deck file through IFileStorage.");
        }

        service.RenameDeck("alphaCopy", "alphaRenamed");
        if (storage.FileExists(service.GetDeckPath("alphaCopy")) || !storage.FileExists(service.GetDeckPath("alphaRenamed")))
        {
            throw new Exception("DeckFlowService did not rename a deck file through IFileStorage.");
        }

        service.SaveSerializedDeck("gamma", "#main\n123\n");
        if (storage.LastWritePath != service.GetDeckPath("gamma") || storage.LastWriteContents != "#main\n123\n")
        {
            throw new Exception("DeckFlowService save no longer routes through IFileStorage.");
        }

        DeckEditorLaunchRequest request;
        if (!service.TryCreateEditorLaunchRequest("gamma", out request))
        {
            throw new Exception("DeckFlowService should create an editor launch request for an existing deck.");
        }

        if (request.DeckName != "gamma" || request.DeckPath != service.GetDeckPath("gamma"))
        {
            throw new Exception("DeckFlowService editor launch request lost deck identity.");
        }

        bool opened = service.TryOpenEditor("gamma", new DeckEditorOpenActions());
        if (!opened)
        {
            throw new Exception("DeckFlowService should open an existing deck editor session.");
        }

        service.DeleteDeck("gamma");
        if (storage.FileExists(service.GetDeckPath("gamma")))
        {
            throw new Exception("DeckFlowService did not delete a deck file through IFileStorage.");
        }
    }

    private static void VerifyDeckControllerCloseBehavior()
    {
        AppUiRoot uiRoot = AppUiRoot.EnsureInstance();
        GameObject menuRoot = new GameObject("Task4.MenuRoot");
        GameObject deckRoot = new GameObject("Task4.DeckRoot");

        try
        {
            TestScreenController menuController = new TestScreenController("menu.main", menuRoot);
            uiRoot.RegisterScreen(menuController);

            DeckScreenController deckController = new DeckScreenController(new DeckFlowService(new TestPaths("/task4-ui"), new TestStorage()));
            deckController.Bind(deckRoot, null, delegate { deckRoot.SetActive(true); }, delegate { deckRoot.SetActive(false); });

            uiRoot.Navigate(menuController.RouteKey);
            uiRoot.Navigate(deckController.RouteKey);

            bool fallbackInvoked = false;
            deckController.Close(new DeckCloseActions
            {
                ShowMenu = delegate { fallbackInvoked = true; }
            });

            if (fallbackInvoked || uiRoot.Router.CurrentRouteKey != menuController.RouteKey)
            {
                throw new Exception("DeckScreenController should navigate back through the shared router when history exists.");
            }

            uiRoot.Router.HideCurrent();

            fallbackInvoked = false;
            deckController.Close(new DeckCloseActions
            {
                ShowMenu = delegate { fallbackInvoked = true; }
            });

            if (!fallbackInvoked)
            {
                throw new Exception("DeckScreenController should fall back to legacy close actions when no shared route history exists.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(menuRoot);
            UnityEngine.Object.DestroyImmediate(deckRoot);
            if (AppUiRoot.Current != null)
            {
                UnityEngine.Object.DestroyImmediate(AppUiRoot.Current.gameObject);
            }
        }
    }

    private sealed class TestPaths : IPlatformPaths
    {
        private readonly string projectRoot;

        public TestPaths(string projectRoot)
        {
            this.projectRoot = projectRoot;
        }

        public string ProjectRoot
        {
            get { return projectRoot; }
        }

        public string GetDirectoryPath(string directoryName)
        {
            return Combine(projectRoot, directoryName);
        }

        public string GetProjectRootFilePath(params string[] segments)
        {
            string path = projectRoot;
            for (int index = 0; index < segments.Length; index++)
            {
                path = Combine(path, segments[index]);
            }

            return path;
        }

        public string GetFilePath(string directoryName, params string[] segments)
        {
            string path = GetDirectoryPath(directoryName);
            for (int index = 0; index < segments.Length; index++)
            {
                path = Combine(path, segments[index]);
            }

            return path;
        }

        private static string Combine(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
            {
                return right ?? string.Empty;
            }

            if (string.IsNullOrEmpty(right))
            {
                return left;
            }

            return left.TrimEnd('/') + "/" + right.TrimStart('/');
        }
    }

    private sealed class TestStorage : IDeckFileStorage
    {
        private readonly Dictionary<string, TestFileRecord> files = new Dictionary<string, TestFileRecord>(StringComparer.Ordinal);
        private readonly HashSet<string> directories = new HashSet<string>(StringComparer.Ordinal);

        public string LastWritePath { get; private set; }

        public string LastWriteContents { get; private set; }

        public void SeedFile(string path, string contents, DateTime lastWriteTimeUtc)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            EnsureParentDirectory(path);
            files[path] = new TestFileRecord(contents ?? string.Empty, lastWriteTimeUtc);
        }

        public bool FileExists(string path)
        {
            return files.ContainsKey(path);
        }

        public string ReadAllText(string path)
        {
            TestFileRecord record;
            return files.TryGetValue(path, out record) ? record.Contents : string.Empty;
        }

        public void WriteAllText(string path, string contents)
        {
            EnsureParentDirectory(path);
            files[path] = new TestFileRecord(contents ?? string.Empty, DateTime.UtcNow);
            LastWritePath = path;
            LastWriteContents = contents;
        }

        public bool DirectoryExists(string path)
        {
            return directories.Contains(path);
        }

        public void CreateDirectory(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                directories.Add(path);
            }
        }

        public FileStorageEntry[] GetFiles(string directoryPath)
        {
            List<FileStorageEntry> entries = new List<FileStorageEntry>();
            foreach (KeyValuePair<string, TestFileRecord> pair in files)
            {
                if (GetParentDirectory(pair.Key) != directoryPath)
                {
                    continue;
                }

                entries.Add(new FileStorageEntry(pair.Key, GetFileName(pair.Key), pair.Value.LastWriteTimeUtc));
            }

            return entries.ToArray();
        }

        public void DeleteFile(string path)
        {
            files.Remove(path);
        }

        public void CopyFile(string sourcePath, string targetPath)
        {
            TestFileRecord source;
            if (!files.TryGetValue(sourcePath, out source))
            {
                throw new InvalidOperationException("Missing source file: " + sourcePath);
            }

            EnsureParentDirectory(targetPath);
            files[targetPath] = new TestFileRecord(source.Contents, DateTime.UtcNow);
        }

        public void MoveFile(string sourcePath, string targetPath)
        {
            TestFileRecord source;
            if (!files.TryGetValue(sourcePath, out source))
            {
                throw new InvalidOperationException("Missing source file: " + sourcePath);
            }

            files.Remove(sourcePath);
            EnsureParentDirectory(targetPath);
            files[targetPath] = source;
        }

        private void EnsureParentDirectory(string path)
        {
            string parentDirectory = GetParentDirectory(path);
            if (string.IsNullOrEmpty(parentDirectory))
            {
                return;
            }

            directories.Add(parentDirectory);
        }

        private static string GetParentDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            int separatorIndex = path.LastIndexOf('/');
            return separatorIndex <= 0 ? string.Empty : path.Substring(0, separatorIndex);
        }

        private static string GetFileName(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            int separatorIndex = path.LastIndexOf('/');
            return separatorIndex < 0 ? path : path.Substring(separatorIndex + 1);
        }

        private sealed class TestFileRecord
        {
            public TestFileRecord(string contents, DateTime lastWriteTimeUtc)
            {
                Contents = contents;
                LastWriteTimeUtc = lastWriteTimeUtc;
            }

            public string Contents { get; private set; }

            public DateTime LastWriteTimeUtc { get; private set; }
        }
    }

    private sealed class TestScreenController : IUiScreenController
    {
        private readonly GameObject root;

        public TestScreenController(string routeKey, GameObject root)
        {
            RouteKey = routeKey;
            this.root = root;
        }

        public string RouteKey { get; private set; }

        public bool IsVisible
        {
            get { return root.activeSelf; }
        }

        public void Show(object parameter)
        {
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }
    }
}

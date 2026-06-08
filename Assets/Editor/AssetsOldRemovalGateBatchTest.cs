using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Task 17: Assets/old removal gate.
///
/// This test is an explicit automated gate that blocks Wave 4 destructive cleanup
/// until all preconditions for final deletion of Assets/old are met.
///
/// Preconditions verified:
/// 1. Assets/old directory does NOT exist
/// 2. Assets/old.meta file does NOT exist
/// 3. No serialized references to "Assets/old" in any .unity/.prefab/.mat/.asset files
/// 4. No serialized references to the legacy old GUID (d130df7ae47578041b524d4be0be3227)
///
/// If any check fails, this gate blocks the migration and reports exactly what
/// must be cleaned up before Wave 4 can proceed.
/// </summary>
public static class AssetsOldRemovalGateBatchTest
{
    // The known GUID of the deleted Assets/old directory's meta file
    private const string OldDirectoryGuid = "d130df7ae47578041b524d4be0be3227";

    public static void Run()
    {
        int exitCode = 0;
        List<string> blockers = new List<string>();

        try
        {
            VerifyDirectoryAbsent(blockers);
            VerifyMetaFileAbsent(blockers);
            VerifyNoSerializedReferences(blockers);

            if (blockers.Count > 0)
            {
                string blockerList = string.Join("\n  - ", blockers.ToArray());
                throw new Exception(
                    "Assets/old removal gate BLOCKED. The following issues must be resolved before Wave 4 deletion:\n  - "
                    + blockerList);
            }

            Debug.Log("AssetsOldRemovalGateBatchTest OK — Assets/old is safe to delete");
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

    /// <summary>
    /// Verifies that the Assets/old directory itself does not exist.
    /// </summary>
    private static void VerifyDirectoryAbsent(List<string> blockers)
    {
        string oldDirectoryPath = Path.Combine(Application.dataPath, "old");
        if (Directory.Exists(oldDirectoryPath))
        {
            int fileCount = Directory.GetFiles(oldDirectoryPath, "*.*", SearchOption.AllDirectories).Length;
            blockers.Add(
                "Assets/old directory still exists with " + fileCount + " file(s). "
                + "All content must be migrated to Assets/Content or retired before deletion.");
        }
    }

    /// <summary>
    /// Verifies that the Assets/old.meta file does not exist.
    /// </summary>
    private static void VerifyMetaFileAbsent(List<string> blockers)
    {
        string oldMetaPath = Path.Combine(Application.dataPath, "old.meta");
        if (File.Exists(oldMetaPath))
        {
            blockers.Add(
                "Assets/old.meta file still exists. "
                + "This meta file must be removed after the directory is emptied.");
        }
    }

    /// <summary>
    /// Scans all serialized Unity assets for any remaining references to Assets/old
    /// or its known directory GUID.
    /// </summary>
    private static void VerifyNoSerializedReferences(List<string> blockers)
    {
        string[] extensions = { "*.unity", "*.prefab", "*.mat", "*.asset" };
        List<string> matches = new List<string>();

        foreach (string extension in extensions)
        {
            string[] files = Directory.GetFiles("Assets", extension, SearchOption.AllDirectories);
            foreach (string filePath in files)
            {
                // Skip meta files themselves
                if (filePath.EndsWith(".meta"))
                {
                    continue;
                }

                try
                {
                    string content = File.ReadAllText(filePath);
                    if (content.Contains("Assets/old"))
                    {
                        matches.Add(filePath + " (contains 'Assets/old')");
                    }
                    else if (content.Contains(OldDirectoryGuid))
                    {
                        matches.Add(filePath + " (contains old directory GUID)");
                    }
                }
                catch (Exception)
                {
                    // Binary or unreadable files are skipped; they should not contain text references
                }
            }
        }

        if (matches.Count > 0)
        {
            string fileList = string.Join(", ", matches.ToArray());
            blockers.Add(
                "Found " + matches.Count + " serialized file(s) with references to Assets/old:\n      "
                + fileList);
        }
    }
}

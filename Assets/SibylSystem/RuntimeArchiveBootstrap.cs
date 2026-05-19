using System;
using System.IO;
using Ionic.Zip;

public static class RuntimeArchiveBootstrap
{
    private const string ArchiveDirectoryName = "runtime-zips";
    private const string StampFileName = ".archive-stamp";

    public static void EnsureRequiredDirectories(Action<string> log)
    {
        EnsureExtracted(Environment.CurrentDirectory, "picture", log);
        EnsureExtracted(Environment.CurrentDirectory, "script", log);
    }

    public static void EnsureExtracted(string rootPath, string directoryName)
    {
        EnsureExtracted(rootPath, directoryName, null);
    }

    public static void EnsureExtracted(string rootPath, string directoryName, Action<string> log)
    {
        string archivePath = Path.Combine(rootPath, ArchiveDirectoryName, directoryName + ".zip");
        string destinationPath = Path.Combine(rootPath, directoryName);

        if (!File.Exists(archivePath))
        {
            return;
        }

        string archiveStamp = BuildArchiveStamp(archivePath);
        string stampPath = Path.Combine(destinationPath, StampFileName);

        if (Directory.Exists(destinationPath))
        {
            if (!File.Exists(stampPath))
            {
                WriteStamp(destinationPath, archiveStamp);
                return;
            }

            string currentStamp = File.ReadAllText(stampPath);
            if (currentStamp == archiveStamp)
            {
                return;
            }
        }

        ReplaceDirectoryFromArchive(archivePath, destinationPath, archiveStamp, log);
    }

    private static void ReplaceDirectoryFromArchive(string archivePath, string destinationPath, string archiveStamp, Action<string> log)
    {
        string parentPath = Path.GetDirectoryName(destinationPath);
        string directoryName = Path.GetFileName(destinationPath);
        string extractingPath = destinationPath + "__extracting";
        string backupPath = destinationPath + "__backup";
        string extractedPathToMove = extractingPath;
        bool movedCurrentDirectory = false;

        if (string.IsNullOrEmpty(parentPath))
        {
            parentPath = ".";
        }

        DeleteDirectoryIfExists(extractingPath);
        DeleteDirectoryIfExists(backupPath);
        Directory.CreateDirectory(parentPath);
        Directory.CreateDirectory(extractingPath);

        if (log != null)
        {
            log("Extracting " + archivePath + " -> " + destinationPath);
        }

        try
        {
            using (ZipFile zip = ZipFile.Read(archivePath))
            {
                zip.ExtractAll(extractingPath, ExtractExistingFileAction.OverwriteSilently);
            }

            string nestedExtractedPath = Path.Combine(extractingPath, directoryName);
            if (Directory.Exists(nestedExtractedPath) && Directory.GetFileSystemEntries(extractingPath).Length == 1)
            {
                extractedPathToMove = nestedExtractedPath;
            }

            WriteStamp(extractedPathToMove, archiveStamp);

            if (Directory.Exists(destinationPath))
            {
                Directory.Move(destinationPath, backupPath);
                movedCurrentDirectory = true;
            }

            Directory.Move(extractedPathToMove, destinationPath);

            if (extractedPathToMove != extractingPath)
            {
                DeleteDirectoryIfExists(extractingPath);
            }

            if (movedCurrentDirectory)
            {
                DeleteDirectoryIfExists(backupPath);
            }
        }
        catch
        {
            DeleteDirectoryIfExists(extractingPath);

            if (movedCurrentDirectory && !Directory.Exists(destinationPath) && Directory.Exists(backupPath))
            {
                Directory.Move(backupPath, destinationPath);
            }

            throw;
        }
    }

    private static string BuildArchiveStamp(string archivePath)
    {
        FileInfo info = new FileInfo(archivePath);
        return info.Length.ToString() + ":" + info.LastWriteTimeUtc.Ticks.ToString();
    }

    private static void WriteStamp(string destinationPath, string archiveStamp)
    {
        Directory.CreateDirectory(destinationPath);
        File.WriteAllText(Path.Combine(destinationPath, StampFileName), archiveStamp);
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }
}

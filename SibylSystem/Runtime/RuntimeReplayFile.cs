using System.IO;

public static class RuntimeReplayFile
{
    public const string LegacyReplayExtension = ".yrp";
    public const string ReplayRecordExtension = ".yrp3d";
    private const string ReplayDirectoryName = "replay";

    public static string GetReplayPath(string fileName)
    {
        return RuntimePaths.GetFilePath(RuntimeDirectory.Replay, fileName);
    }

    public static string GetReplayRecordPath(string replayName)
    {
        return GetReplayPath(replayName + ReplayRecordExtension);
    }

    public static string GetReplayDisplayPath(string fileName)
    {
        return ReplayDirectoryName + "/" + fileName;
    }

    public static FileInfo[] GetReplayFiles()
    {
        return RuntimePaths.GetFiles(RuntimeDirectory.Replay);
    }

    public static bool ReplayFileExists(string fileName)
    {
        return File.Exists(GetReplayPath(fileName));
    }

    public static bool ReplayRecordExists(string replayName)
    {
        return File.Exists(GetReplayRecordPath(replayName));
    }

    public static byte[] ReadReplayFile(string fileName)
    {
        return ReadAllBytes(GetReplayPath(fileName));
    }

    public static bool TryReadReplayFile(string fileName, out byte[] buffer)
    {
        return TryReadAllBytes(GetReplayPath(fileName), out buffer);
    }

    public static byte[] ReadBytes(string path)
    {
        return ReadAllBytes(path);
    }

    public static byte[] ReadReplayRecord(string replayName)
    {
        return ReadAllBytes(GetReplayRecordPath(replayName));
    }

    public static bool TryReadReplayRecord(string replayName, out byte[] buffer)
    {
        return TryReadAllBytes(GetReplayRecordPath(replayName), out buffer);
    }

    public static void WriteReplayFile(string fileName, byte[] buffer)
    {
        WriteAllBytes(GetReplayPath(fileName), buffer);
    }

    public static void WriteBytes(string path, byte[] buffer)
    {
        WriteAllBytes(path, buffer);
    }

    public static void WriteReplayRecord(string replayName, byte[] buffer)
    {
        WriteAllBytes(GetReplayRecordPath(replayName), buffer);
    }

    public static bool DeleteReplayFileIfExists(string fileName)
    {
        return DeleteFileIfExists(GetReplayPath(fileName));
    }

    public static bool DeleteReplayRecordIfExists(string replayName)
    {
        return DeleteFileIfExists(GetReplayRecordPath(replayName));
    }

    public static void MoveReplayFile(string sourceFileName, string destinationFileName)
    {
        MoveFile(GetReplayPath(sourceFileName), GetReplayPath(destinationFileName), false);
    }

    public static void MoveReplayRecord(string sourceReplayName, string destinationReplayName, bool deleteTargetFirst)
    {
        MoveFile(GetReplayRecordPath(sourceReplayName), GetReplayRecordPath(destinationReplayName), deleteTargetFirst);
    }

    private static byte[] ReadAllBytes(string path)
    {
        using (FileStream stream = new FileInfo(path).OpenRead())
        using (MemoryStream memory = new MemoryStream())
        {
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }

    private static bool TryReadAllBytes(string path, out byte[] buffer)
    {
        buffer = null;
        FileInfo fileInfo = new FileInfo(path);
        if (!fileInfo.Exists)
        {
            return false;
        }

        using (FileStream stream = fileInfo.OpenRead())
        using (MemoryStream memory = new MemoryStream())
        {
            stream.CopyTo(memory);
            buffer = memory.ToArray();
            return true;
        }
    }

    private static void WriteAllBytes(string path, byte[] buffer)
    {
        FileInfo fileInfo = new FileInfo(path);
        EnsureParentDirectory(fileInfo);
        using (FileStream stream = fileInfo.Create())
        {
            stream.Write(buffer, 0, buffer.Length);
        }
    }

    private static bool DeleteFileIfExists(string path)
    {
        FileInfo fileInfo = new FileInfo(path);
        if (!fileInfo.Exists)
        {
            return false;
        }

        fileInfo.Delete();
        return true;
    }

    private static void MoveFile(string sourcePath, string destinationPath, bool deleteTargetFirst)
    {
        FileInfo sourceFile = new FileInfo(sourcePath);
        if (!sourceFile.Exists)
        {
            return;
        }

        FileInfo destinationFile = new FileInfo(destinationPath);
        EnsureParentDirectory(destinationFile);
        if (deleteTargetFirst && destinationFile.Exists)
        {
            destinationFile.Delete();
        }

        sourceFile.MoveTo(destinationPath);
    }

    private static void EnsureParentDirectory(FileInfo fileInfo)
    {
        if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
        {
            fileInfo.Directory.Create();
        }
    }
}

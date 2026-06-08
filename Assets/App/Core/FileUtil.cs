using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class FileUtil
{
    public static string ProjectRoot => Path.Combine(Application.dataPath, "..");

    public static string GetFilePath(RuntimeDirectory dir, string name)
    {
        return Path.Combine(Application.dataPath, "..", GetDirectoryName(dir), name);
    }

    public static string GetFilePath(RuntimeDirectory dir, string name, int flags)
    {
        return GetFilePath(dir, name);
    }

    public static string GetFilePath(RuntimeDirectory dir, string[] relativePath)
    {
        return GetFilePath(dir, string.Join(Path.DirectorySeparatorChar, relativePath));
    }

    public static string GetRuntimeOrContentFilePath(string relativePath)
    {
        return Path.Combine(Application.dataPath, "Content", relativePath);
    }

    public static string GetRuntimeOrContentFilePath(string category, string name, bool b1, bool b2)
    {
        return Path.Combine(Application.dataPath, "Content", name);
    }

    public static string GetProjectRootFilePath(string relativePath)
    {
        return Path.Combine(Application.dataPath, "..", relativePath);
    }

    public static string GetProjectRootFilePath(string a, string b, string c, string d)
    {
        return Path.Combine(Application.dataPath, "..", a, b, c, d);
    }

    public static string GetProjectRootFilePath(string[] relativePathParts)
    {
        return Path.Combine(new[] { Application.dataPath, ".." }.Concat(relativePathParts).ToArray());
    }

    public static string GetDirectoryPath(RuntimeDirectory dir)
    {
        return Path.Combine(Application.dataPath, "..", GetDirectoryName(dir));
    }

    public static string GetDirectoryPath(string directoryName)
    {
        return Path.Combine(Application.dataPath, "..", directoryName);
    }

    public static string GetFilePath(string directoryName, string[] segments)
    {
        var parts = new string[1 + segments.Length];
        parts[0] = directoryName;
        for (int i = 0; i < segments.Length; i++) parts[i + 1] = segments[i];
        return Path.Combine(parts);
    }

    public static string GetProjectRootDirectoryPath()
    {
        return Path.Combine(Application.dataPath, "..");
    }

    public static string GetProjectRelativePath(string path)
    {
        return "";
    }
    public enum RuntimeDirectory
    {
        Cdb,
        Config,
        Data,
        Deck,
        Diy,
        Expansions,
        Pack,
        Picture,
        Puzzle,
        Replay,
        Script,
        Sound,
        Texture,
        RuntimeZips,
    }

    public static string GetDirectoryName(RuntimeDirectory dir)
    {
        switch (dir)
        {
            case RuntimeDirectory.Cdb: return "cdb";
            case RuntimeDirectory.Config: return "config";
            case RuntimeDirectory.Data: return "data";
            case RuntimeDirectory.Deck: return "deck";
            case RuntimeDirectory.Diy: return "diy";
            case RuntimeDirectory.Expansions: return "expansions";
            case RuntimeDirectory.Pack: return "pack";
            case RuntimeDirectory.Picture: return "picture";
            case RuntimeDirectory.Puzzle: return "puzzle";
            case RuntimeDirectory.Replay: return "replay";
            case RuntimeDirectory.Script: return "script";
            case RuntimeDirectory.Sound: return "sound";
            case RuntimeDirectory.Texture: return "texture";
            case RuntimeDirectory.RuntimeZips: return "runtime-zips";
            default: return dir.ToString().ToLowerInvariant();
        }
    }
    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
    public sealed class FileStorageEntry
    {
        public FileStorageEntry(string fullPath, string name, DateTime lastWriteTimeUtc)
        {
            FullPath = fullPath ?? string.Empty;
            Name = name ?? string.Empty;
            LastWriteTimeUtc = lastWriteTimeUtc;
        }

        public string FullPath { get; private set; }

        public string Name { get; private set; }

        public DateTime LastWriteTimeUtc { get; private set; }
    }
    public static FileStorageEntry[] GetFiles(string directoryPath)
    {
        FileInfo[] files = new DirectoryInfo(directoryPath).GetFiles();
        FileStorageEntry[] entries = new FileStorageEntry[files.Length];
        for (int index = 0; index < files.Length; index++)
        {
            FileInfo fileInfo = files[index];
            entries[index] = new FileStorageEntry(fileInfo.FullName, fileInfo.Name, fileInfo.LastWriteTimeUtc);
        }
        return entries;
    }
    
    public static bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public static string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }

    public static void WriteAllText(string path, string contents)
    {
        File.WriteAllText(path, contents);
    }

    public static bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public static void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }


    public static void DeleteFile(string path)
    {
        File.Delete(path);
    }

    public static void CopyFile(string sourcePath, string targetPath)
    {
        File.Copy(sourcePath, targetPath);
    }

    public static void MoveFile(string sourcePath, string targetPath)
    {
        File.Move(sourcePath, targetPath);
    }
}

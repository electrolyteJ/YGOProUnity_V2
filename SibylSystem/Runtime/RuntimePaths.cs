using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using App.Core;

public static class RuntimePaths
{
    private static readonly Dictionary<RuntimeDirectory, string> DirectoryNames = new Dictionary<RuntimeDirectory, string>
    {
        { RuntimeDirectory.Cdb, "cdb" },
        { RuntimeDirectory.Config, "config" },
        { RuntimeDirectory.Data, "data" },
        { RuntimeDirectory.Deck, "deck" },
        { RuntimeDirectory.Diy, "diy" },
        { RuntimeDirectory.Expansions, "expansions" },
        { RuntimeDirectory.Pack, "pack" },
        { RuntimeDirectory.Picture, "picture" },
        { RuntimeDirectory.Puzzle, "puzzle" },
        { RuntimeDirectory.Replay, "replay" },
        { RuntimeDirectory.Script, "script" },
        { RuntimeDirectory.Sound, "sound" },
        { RuntimeDirectory.Texture, "texture" },
        { RuntimeDirectory.RuntimeZips, "runtime-zips" },
    };

    public static string ProjectRoot
    {
        get
        {
            string dataPath = Application.dataPath;
            if (!string.IsNullOrEmpty(dataPath))
            {
                string projectRoot = Path.GetDirectoryName(dataPath);
                if (!string.IsNullOrEmpty(projectRoot))
                {
                    return projectRoot;
                }
            }

            return Environment.CurrentDirectory;
        }
    }

    public static string GetDirectoryName(RuntimeDirectory directory)
    {
        return DirectoryNames[directory];
    }

    public static string GetDirectoryPath(RuntimeDirectory directory)
    {
        return Path.Combine(ProjectRoot, GetDirectoryName(directory));
    }

    public static string GetDirectoryPath(string directoryName)
    {
        return Path.Combine(ProjectRoot, directoryName);
    }

    public static string GetProjectRootFilePath(params string[] segments)
    {
        string path = ProjectRoot;
        for (int i = 0; i < segments.Length; i++)
        {
            path = Path.Combine(path, segments[i]);
        }

        return path;
    }

    public static string GetFilePath(RuntimeDirectory directory, params string[] segments)
    {
        string path = GetDirectoryPath(directory);
        for (int i = 0; i < segments.Length; i++)
        {
            path = Path.Combine(path, segments[i]);
        }

        return path;
    }

    public static string GetFilePath(string directoryName, params string[] segments)
    {
        string path = GetDirectoryPath(directoryName);
        for (int i = 0; i < segments.Length; i++)
        {
            path = Path.Combine(path, segments[i]);
        }

        return path;
    }

    public static bool DirectoryExists(RuntimeDirectory directory)
    {
        return Directory.Exists(GetDirectoryPath(directory));
    }

    public static DirectoryInfo EnsureDirectory(RuntimeDirectory directory)
    {
        return Directory.CreateDirectory(GetDirectoryPath(directory));
    }

    public static FileInfo[] GetFiles(RuntimeDirectory directory)
    {
        string path = GetDirectoryPath(directory);
        if (!Directory.Exists(path))
        {
            return new FileInfo[0];
        }

        return new DirectoryInfo(path).GetFiles();
    }

    public static FileInfo[] GetFiles(RuntimeDirectory directory, params string[] segments)
    {
        string path = GetFilePath(directory, segments);
        if (!Directory.Exists(path))
        {
            return new FileInfo[0];
        }

        return new DirectoryInfo(path).GetFiles();
    }
}

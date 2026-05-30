using System;
using System.IO;
using UnityEngine;

public static class RuntimeTextureLoader
{
    private static bool TryReadFileBytes(string path, out byte[] data)
    {
        data = null;
        if (!File.Exists(path))
        {
            return false;
        }

        data = File.ReadAllBytes(path);
        return true;
    }

    public static Texture2D Load(RuntimeDirectory directory, params string[] segments)
    {
        return Load(RuntimePaths.GetFilePath(directory, segments));
    }

    public static Texture2D Load(string path)
    {
        try
        {
            if (!TryReadFileBytes(path, out byte[] data))
            {
                return null;
            }

            Texture2D texture = new Texture2D(1024, 600);
            if (!texture.LoadImage(data))
            {
                Debug.LogError("Failed to decode texture at path: " + path);
                return null;
            }

            return texture;
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return null;
        }
    }
}

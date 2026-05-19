using System;
using System.IO;
using Ionic.Zip;
using UnityEditor;
using UnityEngine;

public static class RuntimeArchiveBootstrapBatchTest
{
    public static void Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "ygopro2-runtime-archive-test-" + Guid.NewGuid().ToString("N"));
        string archiveRoot = Path.Combine(root, "runtime-zips");
        string scriptSource = Path.Combine(root, "script-source");
        string pictureSource = Path.Combine(root, "picture-source");

        Directory.CreateDirectory(archiveRoot);
        Directory.CreateDirectory(scriptSource);
        Directory.CreateDirectory(Path.Combine(pictureSource, "card"));

        File.WriteAllText(Path.Combine(scriptSource, "constant.lua"), "-- first");
        File.WriteAllText(Path.Combine(pictureSource, "card", "1000.jpg"), "picture-one");

        using (ZipFile zip = new ZipFile())
        {
            zip.AddFile(Path.Combine(scriptSource, "constant.lua"), "");
            zip.Save(Path.Combine(archiveRoot, "script.zip"));
        }

        using (ZipFile zip = new ZipFile())
        {
            zip.AddFile(Path.Combine(pictureSource, "card", "1000.jpg"), "card");
            zip.Save(Path.Combine(archiveRoot, "picture.zip"));
        }

        RuntimeArchiveBootstrap.EnsureExtracted(root, "script");
        RuntimeArchiveBootstrap.EnsureExtracted(root, "picture");

        string scriptTarget = Path.Combine(root, "script", "constant.lua");
        string pictureTarget = Path.Combine(root, "picture", "card", "1000.jpg");

        if (!File.Exists(scriptTarget))
        {
            throw new Exception("script archive was not extracted");
        }

        if (!File.Exists(pictureTarget))
        {
            throw new Exception("picture archive was not extracted");
        }

        File.WriteAllText(Path.Combine(scriptSource, "constant.lua"), "-- updated");
        using (ZipFile zip = new ZipFile())
        {
            zip.AddFile(Path.Combine(scriptSource, "constant.lua"), "");
            zip.Save(Path.Combine(archiveRoot, "script.zip"));
        }

        RuntimeArchiveBootstrap.EnsureExtracted(root, "script");

        string content = File.ReadAllText(scriptTarget);
        if (content != "-- updated")
        {
            throw new Exception("script archive refresh did not replace old content");
        }

        Debug.Log("RuntimeArchiveBootstrapBatchTest OK");
        FileUtil.DeleteFileOrDirectory(root);
        EditorApplication.Exit(0);
    }
}

using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class Task5ContentBatchTest
{
    public static void Run()
    {
        int exitCode = 0;

        try
        {
            VerifyUiTextureLookupUsesContentRoot();
            VerifyDuelTexturesUseContentRootWhenLegacyFilesAreMissing();
            VerifyRoomBackgroundUsesContentRootWhenLegacyFileIsMissing();
            Debug.Log("Task5ContentBatchTest OK");
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

    private static void VerifyUiTextureLookupUsesContentRoot()
    {
        string contentPath = RuntimePaths.GetProjectRootFilePath("Assets", "Content", "UI", "task5-ui-only.png");
        WriteColorTexture(contentPath, new Color32(12, 34, 210, 255), true);

        try
        {
            ResetGameTextureManagerUiCache();

            Texture2D texture = GameTextureManager.get("task5-ui-only");
            if (texture == null)
            {
                throw new Exception("GameTextureManager.get did not load a UI texture from Assets/Content/UI.");
            }

            AssertPixel(texture, new Color32(12, 34, 210, 255), "Assets/Content/UI lookup");
        }
        finally
        {
            ResetGameTextureManagerUiCache();
            DeleteFileIfExists(contentPath);
        }
    }

    private static void VerifyDuelTexturesUseContentRootWhenLegacyFilesAreMissing()
    {
        string contentAttackPath = RuntimePaths.GetProjectRootFilePath("Assets", "Content", "UI", "Duel", "attack.png");
        string contentMePath = RuntimePaths.GetProjectRootFilePath("Assets", "Content", "Backgrounds", "Duel", "me.jpg");
        string legacyAttackPath = RuntimePaths.GetFilePath(RuntimeDirectory.Texture, "duel", "attack.png");
        string legacyMePath = RuntimePaths.GetFilePath(RuntimeDirectory.Texture, "duel", "me.jpg");

        WriteColorTexture(contentAttackPath, new Color32(210, 45, 12, 255), true);
        WriteColorTexture(contentMePath, new Color32(25, 150, 200, 255), false);

        string attackBackupPath = legacyAttackPath + ".task5bak";
        string meBackupPath = legacyMePath + ".task5bak";

        MoveFileIfExists(legacyAttackPath, attackBackupPath);
        MoveFileIfExists(legacyMePath, meBackupPath);

        try
        {
            GameTextureManager.attack = null;
            GameTextureManager.myBack = null;

            InvokeGameTextureManagerInitialize();

            if (GameTextureManager.attack == null)
            {
                throw new Exception("GameTextureManager.initialize did not load attack.png from Assets/Content/UI.");
            }

            if (GameTextureManager.myBack == null)
            {
                throw new Exception("GameTextureManager.initialize did not load me.jpg from Assets/Content/Backgrounds.");
            }

            AssertPixel(GameTextureManager.attack, new Color32(210, 45, 12, 255), "Assets/Content/UI/Duel attack");
            AssertPixel(GameTextureManager.myBack, new Color32(25, 150, 200, 255), "Assets/Content/Backgrounds/Duel me");
        }
        finally
        {
            RestoreMovedFile(attackBackupPath, legacyAttackPath);
            RestoreMovedFile(meBackupPath, legacyMePath);
            DeleteFileIfExists(contentAttackPath);
            DeleteFileIfExists(contentMePath);
        }
    }

    private static void VerifyRoomBackgroundUsesContentRootWhenLegacyFileIsMissing()
    {
        string contentDeskPath = RuntimePaths.GetProjectRootFilePath("Assets", "Content", "Backgrounds", "desk.jpg");
        string legacyDeskPath = RuntimePaths.GetFilePath(RuntimeDirectory.Texture, "common", "desk.jpg");
        string legacyDeskBackupPath = legacyDeskPath + ".task5bak";

        WriteColorTexture(contentDeskPath, new Color32(90, 180, 30, 255), false);
        MoveFileIfExists(legacyDeskPath, legacyDeskBackupPath);

        GameObject ownerObject = new GameObject("Task5.Program");
        GameObject rootObject = new GameObject("Task5.BackgroundRoot");
        GameObject modObject = new GameObject("Task5.BackgroundPrefab");
        Texture2D assignedTexture = null;

        try
        {
            Program owner = ownerObject.AddComponent<Program>();
            owner.mod_simple_ngui_background_texture = modObject;
            SetProgramInstance(owner);
            Program.ui_back_ground_2d = rootObject;

            modObject.AddComponent<UITexture>();

            BackGroundPic picture = new BackGroundPic();
            FieldInfo backGroundField = typeof(BackGroundPic).GetField("backGround", BindingFlags.Instance | BindingFlags.NonPublic);
            if (backGroundField == null)
            {
                throw new Exception("BackGroundPic.backGround field was not found.");
            }

            GameObject created = (GameObject)backGroundField.GetValue(picture);
            if (created == null)
            {
                throw new Exception("BackGroundPic did not create a background object.");
            }

            assignedTexture = created.GetComponent<UITexture>().mainTexture as Texture2D;
            if (assignedTexture == null)
            {
                throw new Exception("BackGroundPic did not assign a room background texture.");
            }

            AssertPixel(assignedTexture, new Color32(90, 180, 30, 255), "Assets/Content/Backgrounds desk");
        }
        finally
        {
            RestoreMovedFile(legacyDeskBackupPath, legacyDeskPath);
            DeleteFileIfExists(contentDeskPath);
            Program.ui_back_ground_2d = null;
            SetProgramInstance(null);
            UnityEngine.Object.DestroyImmediate(ownerObject);
            UnityEngine.Object.DestroyImmediate(rootObject);
            UnityEngine.Object.DestroyImmediate(modObject);
        }
    }

    private static void ResetGameTextureManagerUiCache()
    {
        GameTextureManager.uiLoaded = false;

        FieldInfo allUiField = typeof(GameTextureManager).GetField("allUI", BindingFlags.Static | BindingFlags.NonPublic);
        if (allUiField == null)
        {
            throw new Exception("GameTextureManager.allUI cache field was not found.");
        }

        allUiField.SetValue(null, Activator.CreateInstance(allUiField.FieldType));
    }

    private static void InvokeGameTextureManagerInitialize()
    {
        MethodInfo initializeMethod = typeof(GameTextureManager).GetMethod("initialize", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (initializeMethod == null)
        {
            throw new Exception("GameTextureManager.initialize method was not found.");
        }

        initializeMethod.Invoke(null, null);
    }

    private static void SetProgramInstance(Program value)
    {
        FieldInfo instanceField = typeof(Program).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        if (instanceField == null)
        {
            throw new Exception("Program.instance field was not found.");
        }

        instanceField.SetValue(null, value);
    }

    private static void WriteColorTexture(string path, Color32 color, bool png)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        byte[] bytes = png ? texture.EncodeToPNG() : texture.EncodeToJPG(100);
        File.WriteAllBytes(path, bytes);
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void AssertPixel(Texture2D texture, Color32 expected, string description)
    {
        Color32 actual = texture.GetPixel(0, 0);
        if (Mathf.Abs(actual.r - expected.r) > 8
            || Mathf.Abs(actual.g - expected.g) > 8
            || Mathf.Abs(actual.b - expected.b) > 8)
        {
            throw new Exception(description + " loaded the wrong texture content. Expected approx "
                + expected.r + "," + expected.g + "," + expected.b
                + " but got " + actual.r + "," + actual.g + "," + actual.b + ".");
        }
    }

    private static void MoveFileIfExists(string sourcePath, string destinationPath)
    {
        DeleteFileIfExists(destinationPath);
        if (File.Exists(sourcePath))
        {
            File.Move(sourcePath, destinationPath);
        }
    }

    private static void RestoreMovedFile(string sourcePath, string destinationPath)
    {
        if (File.Exists(sourcePath))
        {
            DeleteFileIfExists(destinationPath);
            File.Move(sourcePath, destinationPath);
        }
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

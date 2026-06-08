using System;
using App.Core;
using UnityEditor;
using UnityEngine;

public static class UiTextureResourceLoaderBatchTest
{
    public static void Run()
    {
        int exitCode = 0;

        try
        {
            VerifyLegacyBasenameLookup();
            VerifyLegacyHealthBarLookup();
            Debug.Log("UiTextureResourceLoaderBatchTest OK");
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

    private static void VerifyLegacyBasenameLookup()
    {
        string resolvedPath = UiTextureResourceLoader.ResolveUiTexturePath("tinyButton_Normal");
        if (string.IsNullOrEmpty(resolvedPath) || !resolvedPath.EndsWith("texture/ui/tinyButton_Normal.png"))
        {
            throw new Exception("Legacy basename lookup should resolve tinyButton_Normal from texture/ui.");
        }
    }

    private static void VerifyLegacyHealthBarLookup()
    {
        string resolvedPath = UiTextureResourceLoader.ResolveUiTexturePath("Duel/healthBar/lp.png");
        if (string.IsNullOrEmpty(resolvedPath) || !resolvedPath.EndsWith("Assets/Content/UI/Duel/healthBar/lp.png"))
        {
            throw new Exception("Legacy duel HUD lookup should resolve lp.png through the UI category.");
        }
    }
}

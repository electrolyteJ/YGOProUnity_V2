using UnityEngine;
using App.Core;

/// <summary>
/// Compatibility stub — former SibylSystem.UIHelper class.
/// UI binding has moved to App-owned screen controllers.
/// This is a thin delegate over IResourceResolver for backwards compatibility only.
/// Do not add new functionality here; use IResourceResolver directly instead.
/// </summary>
public static class UIHelper
{
    public static void registEvent() { }
    public static void trySetLableText(object label, string text) { }
    public static T getByName<T>(string name) where T : Component => null;

    /// <summary>
    /// Legacy texture lookup via the centralized resource resolver.
    /// Expects a path relative to the UI category (e.g., "Duel/attack.png").
    /// </summary>
    public static Texture2D getTexture2D(string path)
    {
        return UiTextureResourceLoader.LoadUiTexture(path);
    }

    public static Texture2D getTexture2D(string path, int width, int height) => getTexture2D(path);
    public static void setLabelText(object label, string text) { }
}

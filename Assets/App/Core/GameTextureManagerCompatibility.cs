using UnityEngine;
using App.Core;

/// <summary>
/// Compatibility stub — former SibylSystem.GameTextureManager class.
/// Texture management has moved to App.Core.ResourceResolver.
/// This is a thin delegate over IResourceResolver for backwards compatibility only.
/// Do not add new functionality here; use IResourceResolver directly instead.
/// </summary>
public static class GameTextureManager
{
    public static void initialize() { }
    public static void clearAll() { }

    /// <summary>
    /// Legacy texture lookup via the centralized resource resolver.
    /// Expects a path relative to the UI category (e.g., "Duel/attack.png").
    /// </summary>
    public static Texture2D get(string path)
    {
        return UiTextureResourceLoader.LoadUiTexture(path);
    }

    // Specific legacy getters delegate through the resolver with known categories
    public static Texture2D getBar() => get("Duel/healthBar/bg.png");
    public static Texture2D getLp() => get("Duel/healthBar/lp.png");
    public static Texture2D getTime() => get("Duel/healthBar/t.png");
    public static Texture2D getExBar() => get("Duel/healthBar/excited.png");
}

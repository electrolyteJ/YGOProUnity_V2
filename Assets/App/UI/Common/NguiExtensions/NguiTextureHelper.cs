using UnityEngine;
using App.Core;

namespace App.UI.Common.NguiExtensions
{
    /// <summary>
    /// Extension helper for NGUI UITexture that provides project-specific texture loading.
    /// This keeps the NGUI vendor code clean while adding YGOProUnity-specific functionality.
    /// Delegates all resource lookups through IResourceResolver instead of legacy GameTextureManager.
    /// </summary>
    public static class NguiTextureHelper
    {
        /// <summary>
        /// Shared UI texture loader used by App-owned NGUI helpers and compatibility shims.
        /// </summary>
        public static Texture2D LoadUiTexture(string path)
        {
            return UiTextureResourceLoader.LoadUiTexture(path);
        }

        /// <summary>
        /// Load texture using the centralized resource resolver and apply to NGUI UITexture.
        /// The resolver handles dual-root lookup (runtime preferred, fallback to Assets/Content).
        /// </summary>
        public static void LoadTextureFromPath(UITexture uiTexture, string path)
        {
            if (uiTexture == null || string.IsNullOrEmpty(path))
                return;

            Texture2D loadedTexture = LoadUiTexture(path);
            if (loadedTexture != null)
            {
                uiTexture.mainTexture = loadedTexture;
            }
        }
    }
}

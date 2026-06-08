#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace App.UI.Common.NguiExtensions
{
    /// <summary>
    /// Extension helper for NGUI UITextureInspector that provides project-specific texture path handling.
    /// This keeps the NGUI vendor code clean while adding YGOProUnity-specific functionality.
    /// Delegates all resource lookups through IResourceResolver instead of hard-coded legacy paths.
    /// </summary>
    public static class NguiTextureInspectorHelper
    {
        /// <summary>
        /// Load preview texture from project's UI category via the centralized resource resolver.
        /// The resolver handles dual-root lookup (runtime preferred, fallback to Assets/Content).
        /// </summary>
        public static void LoadPreviewTexture(UITexture uiTexture, string pathInUiFolder)
        {
            if (uiTexture == null || string.IsNullOrEmpty(pathInUiFolder))
                return;

            // Append .png extension if not present
            string relativePath = pathInUiFolder.EndsWith(".png") ? pathInUiFolder : pathInUiFolder + ".png";

            Texture2D loadedTexture = NguiTextureHelper.LoadUiTexture(relativePath);
            if (loadedTexture != null)
            {
                uiTexture.mainTexture = loadedTexture;
            }
        }
    }
}
#endif

using UnityEngine;

namespace App.UI.Common.NguiExtensions
{
    /// <summary>
    /// Extension helper for NGUI UIButton that provides project-specific texture state handling.
    /// This keeps the NGUI vendor code clean while adding YGOProUnity-specific functionality.
    /// Delegates all resource lookups through IResourceResolver instead of legacy GameTextureManager.
    /// </summary>
    public static class NguiButtonHelper
    {
        /// <summary>
        /// Apply state-based textures using the centralized resource resolver.
        /// Called from UIButton when state changes. The resolver handles dual-root lookup
        /// (runtime preferred, fallback to Assets/Content).
        /// </summary>
        public static void ApplyStateTexture(UIButton button, UITexture textureComponent, string normalPath, string pressedPath, UIButton.State currentState)
        {
            if (textureComponent == null)
                return;

            switch (currentState)
            {
                case UIButton.State.Normal:
                    if (!string.IsNullOrEmpty(normalPath))
                    {
                        textureComponent.mainTexture = LoadTexture(normalPath);
                    }
                    break;
                case UIButton.State.Hover:
                case UIButton.State.Pressed:
                    if (!string.IsNullOrEmpty(pressedPath))
                    {
                        textureComponent.mainTexture = LoadTexture(pressedPath);
                    }
                    break;
            }
        }

        private static Texture2D LoadTexture(string path)
        {
            return NguiTextureHelper.LoadUiTexture(path);
        }
    }
}

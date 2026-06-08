using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace App.Core
{
    /// <summary>
    /// Compatibility loader for legacy UI texture names stored in serialized NGUI fields.
    /// New production logic should prefer IResourceResolver directly.
    /// </summary>
    public static class UiTextureResourceLoader
    {
        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg" };
        private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

        private static readonly object indexLock = new object();

        private static App.Core.IResourceResolver resolver;
        private static Dictionary<string, string> legacyNameIndex;

        private static App.Core.IResourceResolver Resolver
        {
            get { return resolver ?? (resolver = new ResourceResolver()); }
        }

        public static Texture2D LoadUiTexture(string path)
        {
            string resolvedPath = ResolveUiTexturePath(path);
            if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
            {
                return null;
            }

            byte[] fileData = File.ReadAllBytes(resolvedPath);
            Texture2D texture = new Texture2D(2, 2);
            return texture.LoadImage(fileData) ? texture : null;
        }

        public static string ResolveUiTexturePath(string path)
        {
            string normalizedPath = Normalize(path);
            if (string.IsNullOrEmpty(normalizedPath))
            {
                return string.Empty;
            }

            string resolvedPath = TryResolveExplicitPath(normalizedPath);
            if (!string.IsNullOrEmpty(resolvedPath))
            {
                return resolvedPath;
            }

            string legacyName = Path.GetFileNameWithoutExtension(normalizedPath);
            if (string.IsNullOrEmpty(legacyName))
            {
                return string.Empty;
            }

            EnsureLegacyNameIndex();
            string indexedPath;
            return legacyNameIndex != null && legacyNameIndex.TryGetValue(legacyName, out indexedPath)
                ? indexedPath
                : string.Empty;
        }

        private static string TryResolveExplicitPath(string normalizedPath)
        {
            string[] explicitCandidates = BuildExplicitCandidates(normalizedPath);
            for (int i = 0; i < explicitCandidates.Length; i++)
            {
                string[] segments = SplitSegments(explicitCandidates[i]);
                if (segments.Length == 0)
                {
                    continue;
                }

                string resolvedPath = Resolver.ResolvePath("UI", segments);
                if (File.Exists(resolvedPath))
                {
                    return resolvedPath;
                }
            }

            return string.Empty;
        }

        private static string[] BuildExplicitCandidates(string normalizedPath)
        {
            if (Path.HasExtension(normalizedPath))
            {
                return new[] { normalizedPath };
            }

            var candidates = new List<string>(ImageExtensions.Length + 1);
            for (int i = 0; i < ImageExtensions.Length; i++)
            {
                candidates.Add(normalizedPath + ImageExtensions[i]);
            }

            candidates.Add(normalizedPath);
            return candidates.ToArray();
        }

        private static void EnsureLegacyNameIndex()
        {
            if (legacyNameIndex != null)
            {
                return;
            }

            lock (indexLock)
            {
                if (legacyNameIndex != null)
                {
                    return;
                }

                var index = new Dictionary<string, string>(NameComparer);
                string[] files = Resolver.ListFiles("UI", "*.*", true);
                for (int i = 0; i < files.Length; i++)
                {
                    string filePath = files[i];
                    string extension = Path.GetExtension(filePath);
                    if (string.IsNullOrEmpty(extension) || !IsSupportedImageExtension(extension))
                    {
                        continue;
                    }

                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    if (!index.ContainsKey(fileName))
                    {
                        index.Add(fileName, filePath);
                    }
                }

                legacyNameIndex = index;
            }
        }

        private static bool IsSupportedImageExtension(string extension)
        {
            for (int i = 0; i < ImageExtensions.Length; i++)
            {
                if (string.Equals(ImageExtensions[i], extension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Normalize(string path)
        {
            return string.IsNullOrEmpty(path)
                ? string.Empty
                : path.Replace('\\', '/').Trim();
        }

        private static string[] SplitSegments(string path)
        {
            return string.IsNullOrEmpty(path)
                ? new string[0]
                : path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}

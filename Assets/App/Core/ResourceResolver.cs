using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using RuntimeDirectory = FileUtil.RuntimeDirectory;

namespace App.Core
{
    /// <summary>
    /// Concrete implementation of IResourceResolver that enforces the resource
    /// authority rules documented in docs/content-authority-matrix.md.
    /// </summary>
    public sealed class ResourceResolver : App.Core.IResourceResolver
    {
        private static readonly StringComparer CategoryComparer = StringComparer.OrdinalIgnoreCase;

        private static readonly Dictionary<string, RuntimeDirectory> CategoryRuntimeMap = new Dictionary<string, RuntimeDirectory>(CategoryComparer)
        {
            { "UI", RuntimeDirectory.Texture },
            { "Cards", RuntimeDirectory.Picture },
            { "Fields", RuntimeDirectory.Texture },
            { "Backgrounds", RuntimeDirectory.Texture },
            { "Audio", RuntimeDirectory.Sound },
            { "Config", RuntimeDirectory.Config },
            { "CDB", RuntimeDirectory.Cdb },
            { "Script", RuntimeDirectory.Script },
            { "Deck", RuntimeDirectory.Deck },
            { "Replay", RuntimeDirectory.Replay },
            { "Faces", RuntimeDirectory.Texture },
            { "FX", RuntimeDirectory.Texture },
            { "Expansions", RuntimeDirectory.Expansions },
            { "Pack", RuntimeDirectory.Pack },
            { "DIY", RuntimeDirectory.Diy },
            { "Data", RuntimeDirectory.Data },
            { "Puzzle", RuntimeDirectory.Puzzle },
        };

        private static readonly Dictionary<string, string> CategoryContentMap = new Dictionary<string, string>(CategoryComparer)
        {
            { "UI", "UI" },
            { "Cards", "Cards" },
            { "Fields", "Fields" },
            { "Backgrounds", "Backgrounds" },
            { "Faces", "UI/Faces" },
            { "FX", "FX" },
        };

        private static readonly HashSet<string> ContentAuthoritativeCategories = new HashSet<string>(CategoryComparer)
        {
            "FX"
        };

        private static readonly HashSet<string> RuntimeFirstCategories = new HashSet<string>(CategoryComparer)
        {
            "Audio", "Config", "CDB", "Script", "Deck", "Replay",
            "Expansions", "Pack", "DIY", "Data", "Puzzle"
        };

        private static readonly HashSet<string> DualRootCategories = new HashSet<string>(CategoryComparer)
        {
            "UI", "Cards", "Fields", "Backgrounds", "Faces"
        };

        public string ResolvePath(string category, params string[] relativePath)
        {
            if (string.IsNullOrEmpty(category))
            {
                throw new ArgumentException("Category must not be null or empty.", nameof(category));
            }

            string[] normalizedRelativePath = NormalizeRelativePath(relativePath);
            if (normalizedRelativePath.Length == 0)
            {
                throw new ArgumentException("Relative path must not be null or empty.", nameof(relativePath));
            }

            if (RuntimeFirstCategories.Contains(category))
            {
                return ResolveRuntimeFirst(category, normalizedRelativePath);
            }

            if (ContentAuthoritativeCategories.Contains(category))
            {
                return ResolveContentAuthoritative(category, normalizedRelativePath);
            }

            if (DualRootCategories.Contains(category))
            {
                return ResolveDualRoot(category, normalizedRelativePath);
            }

            Debug.LogWarning($"[ResourceResolver] Unknown category '{category}'. Defaulting to dual-root resolution.");
            return ResolveDualRoot(category, normalizedRelativePath);
        }

        public bool Exists(string category, params string[] relativePath)
        {
            return File.Exists(ResolvePath(category, relativePath));
        }

        public string[] ListFiles(string category, string searchPattern = "*", bool recursive = false)
        {
            if (string.IsNullOrEmpty(category))
            {
                throw new ArgumentException("Category must not be null or empty.", nameof(category));
            }

            var results = new List<string>();
            var seenByName = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            SearchOption option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            if (DualRootCategories.Contains(category))
            {
                AddFiles(results, seenByName, EnumerateRuntimeRoots(category), searchPattern, option);
                AddFiles(results, seenByName, EnumerateContentRoots(category), searchPattern, option);
                return results.ToArray();
            }

            if (RuntimeFirstCategories.Contains(category))
            {
                AddFiles(results, null, EnumerateRuntimeRoots(category), searchPattern, option);
                return results.ToArray();
            }

            if (ContentAuthoritativeCategories.Contains(category))
            {
                AddFiles(results, null, EnumerateContentRoots(category), searchPattern, option);
            }

            return results.ToArray();
        }

        private static void AddFiles(List<string> results, HashSet<string> seenByName, IEnumerable<string> roots, string searchPattern, SearchOption option)
        {
            foreach (string root in roots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                FileInfo[] files = new DirectoryInfo(root).GetFiles(searchPattern, option);
                foreach (FileInfo file in files)
                {
                    if (seenByName == null || seenByName.Add(Path.GetFileNameWithoutExtension(file.Name)))
                    {
                        results.Add(file.FullName);
                    }
                }
            }
        }

        private string ResolveRuntimeFirst(string category, string[] relativePath)
        {
            foreach (string candidate in EnumerateRuntimeFileCandidates(category, relativePath))
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return EnumerateRuntimeFileCandidates(category, relativePath).First();
        }

        private string ResolveContentAuthoritative(string category, string[] relativePath)
        {
            foreach (string candidate in EnumerateContentFileCandidates(category, relativePath))
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return EnumerateContentFileCandidates(category, relativePath).First();
        }

        private string ResolveDualRoot(string category, string[] relativePath)
        {
            var runtimeCandidates = EnumerateRuntimeFileCandidates(category, relativePath).ToArray();
            foreach (string candidate in runtimeCandidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            var contentCandidates = EnumerateContentFileCandidates(category, relativePath).ToArray();
            foreach (string candidate in contentCandidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return runtimeCandidates.FirstOrDefault()
                ?? contentCandidates.FirstOrDefault()
                ?? BuildProjectPath(new[] { "Assets", "Content" }.Concat(relativePath).ToArray());
        }

        private IEnumerable<string> EnumerateRuntimeRoots(string category)
        {
            if (!CategoryRuntimeMap.TryGetValue(category, out RuntimeDirectory runtimeDirectory))
            {
                yield return FileUtil.GetDirectoryPath(RuntimeDirectory.Texture);
                yield break;
            }

            foreach (string relativeRoot in GetRuntimeRootCandidates(category))
            {
                yield return string.IsNullOrEmpty(relativeRoot)
                    ? FileUtil.GetDirectoryPath(runtimeDirectory)
                    : FileUtil.GetFilePath(runtimeDirectory, relativeRoot);
            }
        }

        private IEnumerable<string> EnumerateContentRoots(string category)
        {
            if (!CategoryContentMap.TryGetValue(category, out string contentRoot))
            {
                yield return BuildProjectPath("Assets", "Content");
                yield break;
            }

            yield return BuildProjectPath(SplitSegments(contentRoot));
        }

        private IEnumerable<string> EnumerateRuntimeFileCandidates(string category, string[] relativePath)
        {
            if (!CategoryRuntimeMap.TryGetValue(category, out RuntimeDirectory runtimeDirectory))
            {
                yield return FileUtil.GetFilePath(RuntimeDirectory.Texture, relativePath);
                yield break;
            }

            foreach (string[] candidateSegments in GetRuntimeRelativeCandidates(category, relativePath))
            {
                yield return FileUtil.GetFilePath(runtimeDirectory, candidateSegments);
            }
        }

        private IEnumerable<string> EnumerateContentFileCandidates(string category, string[] relativePath)
        {
            string[] contentBase = CategoryContentMap.TryGetValue(category, out string contentRoot)
                ? SplitSegments(contentRoot)
                : new[] { "Assets", "Content" };

            foreach (string[] candidateSegments in GetContentRelativeCandidates(category, relativePath))
            {
                yield return BuildProjectPath(contentBase.Concat(candidateSegments).ToArray());
            }
        }

        private static IEnumerable<string> GetRuntimeRootCandidates(string category)
        {
            if (category.Equals("UI", StringComparison.OrdinalIgnoreCase))
            {
                yield return "ui";
                yield return "duel";
                yield break;
            }

            if (category.Equals("Cards", StringComparison.OrdinalIgnoreCase))
            {
                yield return "card";
                yield return string.Empty;
                yield break;
            }

            if (category.Equals("Fields", StringComparison.OrdinalIgnoreCase))
            {
                yield return "duel";
                yield break;
            }

            if (category.Equals("Backgrounds", StringComparison.OrdinalIgnoreCase))
            {
                yield return "common";
                yield return "duel";
                yield break;
            }

            if (category.Equals("Faces", StringComparison.OrdinalIgnoreCase))
            {
                yield return "face";
                yield break;
            }

            yield return string.Empty;
        }

        private static IEnumerable<string[]> GetRuntimeRelativeCandidates(string category, string[] relativePath)
        {
            if (category.Equals("UI", StringComparison.OrdinalIgnoreCase))
            {
                if (StartsWithSegment(relativePath, "Duel"))
                {
                    yield return Combine("duel", relativePath.Skip(1).ToArray());
                }

                yield return Combine("ui", relativePath);
                yield return Combine("duel", relativePath);
                yield break;
            }

            if (category.Equals("Cards", StringComparison.OrdinalIgnoreCase))
            {
                yield return Combine("card", relativePath);
                yield return relativePath;
                yield break;
            }

            if (category.Equals("Fields", StringComparison.OrdinalIgnoreCase))
            {
                yield return Combine("duel", relativePath);
                yield break;
            }

            if (category.Equals("Backgrounds", StringComparison.OrdinalIgnoreCase))
            {
                yield return Combine("common", relativePath);
                yield return Combine("duel", relativePath);
                yield break;
            }

            if (category.Equals("Faces", StringComparison.OrdinalIgnoreCase))
            {
                yield return Combine("face", relativePath);
                yield break;
            }

            yield return relativePath;
        }

        private static IEnumerable<string[]> GetContentRelativeCandidates(string category, string[] relativePath)
        {
            if (category.Equals("UI", StringComparison.OrdinalIgnoreCase))
            {
                yield return relativePath;

                if (!StartsWithSegment(relativePath, "Duel"))
                {
                    yield return Combine("Duel", relativePath);
                }

                yield break;
            }

            yield return relativePath;
        }

        private static string[] NormalizeRelativePath(string[] relativePath)
        {
            if (relativePath == null || relativePath.Length == 0)
            {
                return Array.Empty<string>();
            }

            var segments = new List<string>();
            foreach (string pathPart in relativePath)
            {
                if (string.IsNullOrWhiteSpace(pathPart))
                {
                    continue;
                }

                string[] split = pathPart
                    .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                segments.AddRange(split);
            }

            return segments.ToArray();
        }

        private static string[] SplitSegments(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return new[] { "Assets", "Content" };
            }

            return new[] { "Assets", "Content" }
                .Concat(path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries))
                .ToArray();
        }

        private static string[] Combine(string first, string[] tail)
        {
            if (string.IsNullOrEmpty(first))
            {
                return tail;
            }

            string[] combined = new string[tail.Length + 1];
            combined[0] = first;
            Array.Copy(tail, 0, combined, 1, tail.Length);
            return combined;
        }

        private static bool StartsWithSegment(string[] segments, string expected)
        {
            return segments.Length > 0 && segments[0].Equals(expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildProjectPath(params string[] segments)
        {
            return FileUtil.GetProjectRootFilePath(segments);
        }
    }
}

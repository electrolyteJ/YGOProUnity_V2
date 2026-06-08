namespace App.Core
{
    /// <summary>
    /// Resource resolution interface that centralizes all content/resource lookups.
    /// Replaces scattered reliance on UIHelper, GameTextureManager, and hard-coded
    /// legacy paths (picture/, texture/, sound/) with a single authority layer.
    ///
    /// Authority modes (per docs/content-authority-matrix.md):
    /// - ContentAuthoritative: Assets/Content is the sole source; no fallback to runtime.
    /// - RuntimeFirst: Legacy runtime directory (cdb/, script/, config/, etc.) is authoritative.
    /// - DualRoot: Both Assets/Content and legacy runtime are valid; runtime preferred during migration.
    /// </summary>
    public interface IResourceResolver
    {
        /// <summary>
        /// Resolves a resource file path based on the configured authority mode.
        /// For dual-root categories, checks runtime first then falls back to Assets/Content.
        /// For content-authoritative categories, returns only the Assets/Content path.
        /// For runtime-first categories, returns only the legacy runtime path.
        /// </summary>
        /// <param name="category">The resource category (UI, Cards, Fields, Backgrounds, Audio, FX, Config, CDB, Script, Deck, Replay, Faces).</param>
        /// <param name="relativePath">The relative path within the category (e.g., "Duel/attack.png" for UI).</param>
        /// <returns>The resolved absolute file path.</returns>
        string ResolvePath(string category, params string[] relativePath);

        /// <summary>
        /// Checks if a resource exists at the resolved path.
        /// </summary>
        /// <param name="category">The resource category.</param>
        /// <param name="relativePath">The relative path within the category.</param>
        /// <returns>True if the resource file exists, false otherwise.</returns>
        bool Exists(string category, params string[] relativePath);

        /// <summary>
        /// Returns all files in a resource category directory.
        /// For dual-root categories, merges results from both runtime and content roots.
        /// </summary>
        /// <param name="category">The resource category.</param>
        /// <param name="searchPattern">Optional search pattern (default: "*").</param>
        /// <param name="recursive">Whether to search recursively (default: false).</param>
        /// <returns>Array of absolute file paths.</returns>
        string[] ListFiles(string category, string searchPattern = "*", bool recursive = false);
    }
}

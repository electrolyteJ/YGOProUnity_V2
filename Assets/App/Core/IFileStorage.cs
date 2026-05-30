using System;

namespace App.Core
{
    public interface IFileStorage
    {
        bool FileExists(string path);

        string ReadAllText(string path);

        void WriteAllText(string path, string contents);
    }

    public sealed class FileStorageEntry
    {
        public FileStorageEntry(string fullPath, string name, DateTime lastWriteTimeUtc)
        {
            FullPath = fullPath ?? string.Empty;
            Name = name ?? string.Empty;
            LastWriteTimeUtc = lastWriteTimeUtc;
        }

        public string FullPath { get; private set; }

        public string Name { get; private set; }

        public DateTime LastWriteTimeUtc { get; private set; }
    }

    public interface IDeckFileStorage : IFileStorage
    {
        bool DirectoryExists(string path);

        void CreateDirectory(string path);

        FileStorageEntry[] GetFiles(string directoryPath);

        void DeleteFile(string path);

        void CopyFile(string sourcePath, string targetPath);

        void MoveFile(string sourcePath, string targetPath);
    }
}

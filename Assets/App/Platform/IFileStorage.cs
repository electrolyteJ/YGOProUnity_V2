using System.IO;

namespace App.Platform
{
    public sealed class RuntimeFileStorage : App.Core.IDeckFileStorage
    {
        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public void WriteAllText(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public App.Core.FileStorageEntry[] GetFiles(string directoryPath)
        {
            FileInfo[] files = new DirectoryInfo(directoryPath).GetFiles();
            App.Core.FileStorageEntry[] entries = new App.Core.FileStorageEntry[files.Length];
            for (int index = 0; index < files.Length; index++)
            {
                FileInfo fileInfo = files[index];
                entries[index] = new App.Core.FileStorageEntry(fileInfo.FullName, fileInfo.Name, fileInfo.LastWriteTimeUtc);
            }

            return entries;
        }

        public void DeleteFile(string path)
        {
            File.Delete(path);
        }

        public void CopyFile(string sourcePath, string targetPath)
        {
            File.Copy(sourcePath, targetPath);
        }

        public void MoveFile(string sourcePath, string targetPath)
        {
            File.Move(sourcePath, targetPath);
        }
    }
}

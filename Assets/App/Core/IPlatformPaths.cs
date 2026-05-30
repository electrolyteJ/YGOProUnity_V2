namespace App.Core
{
    public interface IPlatformPaths
    {
        string ProjectRoot { get; }

        string GetDirectoryPath(string directoryName);

        string GetProjectRootFilePath(params string[] segments);

        string GetFilePath(string directoryName, params string[] segments);
    }
}

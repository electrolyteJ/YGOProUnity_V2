namespace App.Platform
{
    public sealed class RuntimePlatformPaths : App.Core.IPlatformPaths
    {
        public string ProjectRoot
        {
            get { return RuntimePaths.ProjectRoot; }
        }

        public string GetDirectoryPath(string directoryName)
        {
            return RuntimePaths.GetDirectoryPath(directoryName);
        }

        public string GetProjectRootFilePath(params string[] segments)
        {
            return RuntimePaths.GetProjectRootFilePath(segments);
        }

        public string GetFilePath(string directoryName, params string[] segments)
        {
            return RuntimePaths.GetFilePath(directoryName, segments);
        }
    }
}

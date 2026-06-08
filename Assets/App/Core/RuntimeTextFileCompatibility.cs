/// <summary>
/// Compatibility stub — former SibylSystem.RuntimeTextFile class.
/// </summary>
public static class RuntimeTextFile
{
    public static string Read(string path) => "";
    public static string ReadAllText(string path) => "";
    public static void Write(string path, string content) { }
    public static bool Exists(string path) => false;
    public static string[] ListFiles(string directory) => new string[0];
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class RuntimeTextFile
{
    public static string NormalizeLineEndings(string text)
    {
        return text.Replace("\r", "");
    }

    public static string[] SplitNormalizedLines(string text)
    {
        return NormalizeLineEndings(text).Split('\n');
    }

    public static string[] SplitNormalizedLines(string text, StringSplitOptions options)
    {
        return NormalizeLineEndings(text).Split(new string[] { "\n" }, options);
    }

    public static bool TrySplitPair(string line, string separator, out string left, out string right)
    {
        string[] parts = line.Split(new string[] { separator }, StringSplitOptions.None);
        if (parts.Length == 2)
        {
            left = parts[0];
            right = parts[1];
            return true;
        }

        left = "";
        right = "";
        return false;
    }

    public static string FormatPair(string left, string separator, string right)
    {
        return left + separator + right;
    }

    public static void EnsureFileExists(string path)
    {
        if (File.Exists(path))
        {
            return;
        }

        string directoryPath = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        using (File.Create(path))
        {
        }
    }

    public static string[] ReadNormalizedLines(string path)
    {
        return ReadNormalizedLines(path, false);
    }

    public static string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }

    public static string[] ReadNormalizedLines(string path, bool stripSpaces)
    {
        string text = NormalizeLineEndings(ReadAllText(path));
        if (stripSpaces)
        {
            text = text.Replace(" ", "");
        }

        return text.Split('\n');
    }

    public static void AppendLine(string path, string line)
    {
        File.AppendAllText(path, line + "\r\n");
    }

    public static void WriteLinesWithWindowsLineEndings(string path, IEnumerable<string> lines)
    {
        StringBuilder builder = new StringBuilder();
        foreach (string line in lines)
        {
            builder.Append(line).Append("\r\n");
        }

        File.WriteAllText(path, builder.ToString());
    }
}

using UnityEngine;

namespace WindBot
{
    public static class Logger
    {
        private static string FormatMessage(string level, string message)
        {
            return $"[WindBot][{level}] {message}";
        }

        public static void WriteLine(string message)
        {
            Debug.Log(FormatMessage("INFO", message));
        }

        public static void DebugWriteLine(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(FormatMessage("DEBUG", message));
#endif
        }

        public static void WriteErrorLine(string message)
        {
            Debug.LogError(FormatMessage("ERROR", message));
        }

        public static System.Action<string> OnLog { get; set; }
        public static System.Action<string> OnError { get; set; }
    }
}

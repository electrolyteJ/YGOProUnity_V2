using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using App.Core;
using App.Core;

namespace App.Screens.Online.Services
{
    public sealed class OnlineFlowService
    {
        private const string ConfigDirectoryName = "config";
        private const string HostsFileName = "hosts.conf";
        private const int MaxHistoryEntries = 5;

        private readonly OnlineFlowCallbacks callbacks;

        public OnlineFlowService() : this(new OnlineFlowCallbacks())
        {
        }

        public OnlineFlowService( OnlineFlowCallbacks callbacks)
        {
            this.callbacks = callbacks ?? new OnlineFlowCallbacks();
        }

        public OnlineHistoryState LoadHistory()
        {
            OnlineHistoryState state = new OnlineHistoryState();
            string[] lines = ReadHistoryLines();
            for (int index = 0; index < lines.Length; index++)
            {
                string normalizedEntry = NormalizeHistoryEntry(lines[index]);
                if (string.IsNullOrEmpty(normalizedEntry))
                {
                    continue;
                }

                state.Entries.Add(normalizedEntry);
                if (state.Entries.Count == 1)
                {
                    ApplySelection(state, ParseHistoryEntry(normalizedEntry));
                }
            }

            return state;
        }

        public OnlineHistorySelection ParseHistoryEntry(string entry)
        {
            OnlineHistorySelection selection = new OnlineHistorySelection();
            if (string.IsNullOrEmpty(entry))
            {
                return selection;
            }

            string[] addressParts = entry.Split(new[] { ':' }, 2);
            selection.Host = addressParts.Length > 0 ? addressParts[0] : string.Empty;

            string remainder = addressParts.Length > 1 ? addressParts[1] : string.Empty;
            string[] portParts = remainder.Split(new[] { ' ' }, 2);
            selection.Port = portParts.Length > 0 ? portParts[0] : string.Empty;
            selection.Password = portParts.Length > 1 ? portParts[1] : string.Empty;
            return selection;
        }

        public OnlineJoinResult PrepareJoin(OnlineJoinRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            OnlineJoinResult result = new OnlineJoinResult();
            if (!request.IsVisible)
            {
                return result;
            }

            string playerName = request.PlayerName ?? string.Empty;
            string host = request.Host ?? string.Empty;
            string port = request.Port ?? string.Empty;
            string version = request.Version ?? string.Empty;
            string password = request.Password ?? string.Empty;

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port))
            {
                result.ErrorMessage = "非法输入！请检查输入的主机名。";
                return result;
            }

            if (string.IsNullOrEmpty(playerName))
            {
                result.ErrorMessage = "昵称不能为空。";
                return result;
            }

            string historyEntry = ComposeHistoryEntry(host, port, password);
            List<string> updatedHistory = new List<string>();
            if (request.ExistingHistory != null)
            {
                for (int index = 0; index < request.ExistingHistory.Count; index++)
                {
                    string existingEntry = NormalizeHistoryEntry(request.ExistingHistory[index]);
                    if (!string.IsNullOrEmpty(existingEntry) && !string.Equals(existingEntry, historyEntry, StringComparison.Ordinal))
                    {
                        updatedHistory.Add(existingEntry);
                    }
                }
            }

            updatedHistory.Insert(0, historyEntry);
            while (updatedHistory.Count > MaxHistoryEntries)
            {
                updatedHistory.RemoveAt(updatedHistory.Count - 1);
            }

            WriteHistoryLines(updatedHistory);
            result.UpdatedHistory.AddRange(updatedHistory);
            result.Connection = new OnlineConnectRequest
            {
                PlayerName = playerName,
                Host = host,
                Port = port,
                Version = version,
                Password = password
            };
            result.ShouldConnect = true;
            return result;
        }

        public void Close(OnlineCloseActions actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            Invoke(actions.CloseConnection);
            if (actions.ExitOnReturn)
            {
                Invoke(actions.ExitApplication);
                return;
            }

            Invoke(actions.ShowMenu);
        }

        private string[] ReadHistoryLines()
        {
            if (callbacks.ReadHistoryLines != null)
            {
                return callbacks.ReadHistoryLines() ?? new string[0];
            }

            string path = FileUtil.GetFilePath(ConfigDirectoryName, HostsFileName);
            if (!FileUtil.FileExists(path))
            {
                FileUtil.WriteAllText(path, string.Empty);
                return new string[0];
            }

            return SplitLines(FileUtil.ReadAllText(path));
        }

        private void WriteHistoryLines(IList<string> entries)
        {
            if (callbacks.WriteHistoryLines != null)
            {
                callbacks.WriteHistoryLines(entries ?? new string[0]);
                return;
            }

            FileUtil.WriteAllText(FileUtil.GetFilePath(ConfigDirectoryName, HostsFileName), string.Join("\r\n", entries ?? new string[0]));
        }

        private static void ApplySelection(OnlineHistoryState state, OnlineHistorySelection selection)
        {
            if (state == null || selection == null)
            {
                return;
            }

            state.SelectedHost = selection.Host ?? string.Empty;
            state.SelectedPort = selection.Port ?? string.Empty;
            state.SelectedPassword = selection.Password ?? string.Empty;
        }

        private static string[] SplitLines(string contents)
        {
            if (string.IsNullOrEmpty(contents))
            {
                return new string[0];
            }

            return contents.Replace("\r\n", "\n").Replace('\r', '\n').Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string NormalizeHistoryEntry(string entry)
        {
            if (string.IsNullOrEmpty(entry))
            {
                return string.Empty;
            }

            return Regex.Replace(entry, "^\\(.*\\)", "");
        }

        private static string ComposeHistoryEntry(string host, string port, string password)
        {
            return string.Format("{0}:{1} {2}", host ?? string.Empty, port ?? string.Empty, password ?? string.Empty);
        }

        private static void Invoke(Action action)
        {
            if (action != null)
            {
                action();
            }
        }
    }
}

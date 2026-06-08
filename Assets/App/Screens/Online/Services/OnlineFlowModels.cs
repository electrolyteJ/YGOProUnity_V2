using System;
using System.Collections.Generic;

namespace App.Screens.Online.Services
{
    public sealed class OnlineHistoryState
    {
        public OnlineHistoryState()
        {
            Entries = new List<string>();
        }

        public List<string> Entries { get; private set; }

        public string SelectedHost { get; set; }

        public string SelectedPort { get; set; }

        public string SelectedPassword { get; set; }
    }

    public sealed class OnlineHistorySelection
    {
        public string Host { get; set; }

        public string Port { get; set; }

        public string Password { get; set; }
    }

    public sealed class OnlineJoinRequest
    {
        public bool IsVisible { get; set; }

        public string PlayerName { get; set; }

        public string Host { get; set; }

        public string Port { get; set; }

        public string Version { get; set; }

        public string Password { get; set; }

        public IList<string> ExistingHistory { get; set; }
    }

    public sealed class OnlineConnectRequest
    {
        public string PlayerName { get; set; }

        public string Host { get; set; }

        public string Port { get; set; }

        public string Version { get; set; }

        public string Password { get; set; }
    }

    public sealed class OnlineJoinResult
    {
        public OnlineJoinResult()
        {
            UpdatedHistory = new List<string>();
        }

        public bool ShouldConnect { get; set; }

        public string ErrorMessage { get; set; }

        public OnlineConnectRequest Connection { get; set; }

        public List<string> UpdatedHistory { get; private set; }
    }

    public sealed class OnlineCloseActions
    {
        public bool ExitOnReturn { get; set; }

        public Action ShowMenu { get; set; }

        public Action ExitApplication { get; set; }

        public Action CloseConnection { get; set; }
    }

    public sealed class OnlineFlowCallbacks
    {
        public Func<string[]> ReadHistoryLines { get; set; }

        public Action<IList<string>> WriteHistoryLines { get; set; }
    }
}

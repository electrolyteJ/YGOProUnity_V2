using System;

namespace App.Features.Menu.Services
{
    public enum MenuDestination
    {
        Settings,
        Deck,
        Online,
        Replay,
        Puzzle,
        AI,
        Exit,
    }

    public enum MenuShellCommandType
    {
        None,
        Online,
        EditDeck,
        Replay,
        Puzzle,
    }

    public sealed class MenuNavigationActions
    {
        public Action ShowSettings { get; set; }

        public Action ShowDeck { get; set; }

        public Action ShowOnline { get; set; }

        public Action ShowReplay { get; set; }

        public Action ShowPuzzle { get; set; }

        public Action ShowAI { get; set; }

        public Action ExitApplication { get; set; }
    }

    public sealed class MenuShellCommand
    {
        public MenuShellCommand(MenuShellCommandType commandType, string[] arguments)
        {
            CommandType = commandType;
            Arguments = arguments ?? new string[0];
        }

        public MenuShellCommandType CommandType { get; private set; }

        public string[] Arguments { get; private set; }
    }

    public sealed class MenuShellExecutionActions
    {
        public Action InitializeFaces { get; set; }

        public Action<string, string, string, string> OpenOnline { get; set; }

        public Action<string, string, string, string, string> OpenOnlineWithVersion { get; set; }

        public Action<string> EditDeck { get; set; }

        public Action<string> Replay { get; set; }

        public Action<string> Puzzle { get; set; }
    }

    public sealed class MenuCommandHandlers
    {
        public Action InitializeFaces { get; set; }

        public Action<string, string, string, string> OpenOnline { get; set; }

        public Action<string, string, string, string, string> OpenOnlineWithVersion { get; set; }

        public Action<string> EditDeck { get; set; }

        public Action<string> Replay { get; set; }

        public Action<string> Puzzle { get; set; }
    }
}

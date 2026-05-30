using System;
using App.Features.Menu.Services;

public static class MenuLegacyBindings
{
    public sealed class NavigationTargets
    {
        public Action ShowSettings { get; set; }

        public Action ShowDeck { get; set; }

        public Action ShowOnline { get; set; }

        public Action ShowReplay { get; set; }

        public Action ShowPuzzle { get; set; }

        public Action ShowAI { get; set; }

        public Action ExitApplication { get; set; }
    }

    public sealed class ShellTargets
    {
        public Action InitializeFaces { get; set; }

        public Action<string, string, string, string> OpenOnline { get; set; }

        public Action<string, string, string, string, string> OpenOnlineWithVersion { get; set; }

        public Action<string> EditDeck { get; set; }

        public Action<string> Replay { get; set; }

        public Action<string> Puzzle { get; set; }
    }

    public static MenuNavigationActions CreateNavigationActions(NavigationTargets targets)
    {
        if (targets == null)
        {
            throw new ArgumentNullException("targets");
        }

        return new MenuNavigationActions
        {
            ShowSettings = targets.ShowSettings,
            ShowDeck = targets.ShowDeck,
            ShowOnline = targets.ShowOnline,
            ShowReplay = targets.ShowReplay,
            ShowPuzzle = targets.ShowPuzzle,
            ShowAI = targets.ShowAI,
            ExitApplication = targets.ExitApplication,
        };
    }

    public static MenuShellExecutionActions CreateShellExecutionActions(ShellTargets targets)
    {
        if (targets == null)
        {
            throw new ArgumentNullException("targets");
        }

        return new MenuShellExecutionActions
        {
            InitializeFaces = targets.InitializeFaces,
            OpenOnline = targets.OpenOnline,
            OpenOnlineWithVersion = targets.OpenOnlineWithVersion,
            EditDeck = targets.EditDeck,
            Replay = targets.Replay,
            Puzzle = targets.Puzzle,
        };
    }
}

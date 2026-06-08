using System;

namespace App.Screens.Menu.Services
{
    public sealed class MenuFlowService
    {
        public sealed class Callbacks
        {
            public Action ScreenShown;
            public Action OpenSettings;
            public Action OpenDeck;
            public Action OpenOnline;
            public Action OpenReplay;
            public Action OpenPuzzle;
            public Action OpenAi;
            public Action ExitRequested;
        }

        private readonly Callbacks callbacks;

        public MenuFlowService()
            : this(new Callbacks())
        {
        }

        public MenuFlowService(Callbacks callbacks)
        {
            this.callbacks = callbacks ?? new Callbacks();
        }

        public void NotifyScreenShown()
        {
            Invoke(callbacks.ScreenShown);
        }

        public void RequestOpenSettings()
        {
            Invoke(callbacks.OpenSettings);
        }

        public void RequestOpenDeck()
        {
            Invoke(callbacks.OpenDeck);
        }

        public void RequestOpenOnline()
        {
            Invoke(callbacks.OpenOnline);
        }

        public void RequestOpenReplay()
        {
            Invoke(callbacks.OpenReplay);
        }

        public void RequestOpenPuzzle()
        {
            Invoke(callbacks.OpenPuzzle);
        }

        public void RequestOpenAi()
        {
            Invoke(callbacks.OpenAi);
        }

        public void RequestExit()
        {
            Invoke(callbacks.ExitRequested);
        }

        public void Navigate(MenuDestination destination, MenuNavigationActions actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            switch (destination)
            {
                case MenuDestination.Settings:
                    Invoke(actions.ShowSettings);
                    break;
                case MenuDestination.Deck:
                    Invoke(actions.ShowDeck);
                    break;
                case MenuDestination.Online:
                    Invoke(actions.ShowOnline);
                    break;
                case MenuDestination.Replay:
                    Invoke(actions.ShowReplay);
                    break;
                case MenuDestination.Puzzle:
                    Invoke(actions.ShowPuzzle);
                    break;
                case MenuDestination.AI:
                    Invoke(actions.ShowAI);
                    break;
                case MenuDestination.Exit:
                    Invoke(actions.ExitApplication);
                    break;
            }
        }

        public bool TryHandleShellCommand(string[] arguments, MenuCommandHandlers handlers)
        {
            MenuShellCommand command;
            if (!TryParseShellCommand(arguments, out command))
            {
                return false;
            }

            ExecuteShellCommand(command, ToExecutionActions(handlers));
            return true;
        }

        public bool TryExecuteShellCommand(string shellContents, MenuShellExecutionActions actions)
        {
            MenuShellCommand command;
            if (!TryParseShellCommand(shellContents, out command))
            {
                return false;
            }

            ExecuteShellCommand(command, actions);
            return true;
        }

        public bool TryParseShellCommand(string shellContents, out MenuShellCommand command)
        {
            return TryParseShellCommand(SplitShellArguments(shellContents), out command);
        }

        public void ExecuteShellCommand(MenuShellCommand command, MenuShellExecutionActions actions)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            switch (command.CommandType)
            {
                case MenuShellCommandType.Online:
                    Invoke(actions.InitializeFaces);
                    if (command.Arguments.Length == 4)
                    {
                        Invoke(actions.OpenOnline, command.Arguments[0], command.Arguments[1], command.Arguments[2], command.Arguments[3]);
                    }
                    else if (command.Arguments.Length == 5)
                    {
                        Invoke(actions.OpenOnlineWithVersion, command.Arguments[0], command.Arguments[1], command.Arguments[2], command.Arguments[3], command.Arguments[4]);
                    }
                    break;
                case MenuShellCommandType.EditDeck:
                    Invoke(actions.EditDeck, command.Arguments[0]);
                    break;
                case MenuShellCommandType.Replay:
                    Invoke(actions.InitializeFaces);
                    Invoke(actions.Replay, command.Arguments[0]);
                    break;
                case MenuShellCommandType.Puzzle:
                    Invoke(actions.InitializeFaces);
                    Invoke(actions.Puzzle, command.Arguments[0]);
                    break;
            }
        }

        private static bool TryParseShellCommand(string[] arguments, out MenuShellCommand command)
        {
            if (arguments == null || arguments.Length == 0)
            {
                command = new MenuShellCommand(MenuShellCommandType.None, new string[0]);
                return false;
            }

            string[] payload = Slice(arguments, 1);
            switch (arguments[0])
            {
                case "online":
                    if (payload.Length == 4 || payload.Length == 5)
                    {
                        command = new MenuShellCommand(MenuShellCommandType.Online, payload);
                        return true;
                    }
                    break;
                case "edit":
                    if (payload.Length == 1)
                    {
                        command = new MenuShellCommand(MenuShellCommandType.EditDeck, payload);
                        return true;
                    }
                    break;
                case "replay":
                    if (payload.Length == 1)
                    {
                        command = new MenuShellCommand(MenuShellCommandType.Replay, payload);
                        return true;
                    }
                    break;
                case "puzzle":
                    if (payload.Length == 1)
                    {
                        command = new MenuShellCommand(MenuShellCommandType.Puzzle, payload);
                        return true;
                    }
                    break;
            }

            command = new MenuShellCommand(MenuShellCommandType.None, arguments);
            return false;
        }

        private static string[] SplitShellArguments(string shellContents)
        {
            if (string.IsNullOrEmpty(shellContents))
            {
                return new string[0];
            }

            char[] parameterCharacters = shellContents.ToCharArray();
            bool inQuote = false;
            for (int index = 0; index < parameterCharacters.Length; index++)
            {
                if (parameterCharacters[index] == '"')
                {
                    inQuote = !inQuote;
                    parameterCharacters[index] = '\n';
                }

                if (!inQuote && parameterCharacters[index] == ' ')
                {
                    parameterCharacters[index] = '\n';
                }
            }

            return (new string(parameterCharacters)).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string[] Slice(string[] values, int startIndex)
        {
            if (values == null || values.Length <= startIndex)
            {
                return new string[0];
            }

            string[] result = new string[values.Length - startIndex];
            Array.Copy(values, startIndex, result, 0, result.Length);
            return result;
        }

        private static MenuShellExecutionActions ToExecutionActions(MenuCommandHandlers handlers)
        {
            if (handlers == null)
            {
                return null;
            }

            return new MenuShellExecutionActions
            {
                InitializeFaces = handlers.InitializeFaces,
                OpenOnline = handlers.OpenOnline,
                OpenOnlineWithVersion = handlers.OpenOnlineWithVersion,
                EditDeck = handlers.EditDeck,
                Replay = handlers.Replay,
                Puzzle = handlers.Puzzle,
            };
        }

        private static void Invoke(Action handler)
        {
            if (handler != null)
            {
                handler();
            }
        }

        private static void Invoke<T>(Action<T> handler, T value)
        {
            if (handler != null)
            {
                handler(value);
            }
        }

        private static void Invoke<T1, T2, T3, T4>(Action<T1, T2, T3, T4> handler, T1 value1, T2 value2, T3 value3, T4 value4)
        {
            if (handler != null)
            {
                handler(value1, value2, value3, value4);
            }
        }

        private static void Invoke<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> handler, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
        {
            if (handler != null)
            {
                handler(value1, value2, value3, value4, value5);
            }
        }
    }
}

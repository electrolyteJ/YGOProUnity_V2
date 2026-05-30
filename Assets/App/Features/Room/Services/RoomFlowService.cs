using System;
using App.Core;
using App.Platform;

namespace App.Features.Room.Services
{
    public sealed class RoomSeatInteractionRequest
    {
        public int SelfType { get; set; }

        public int SeatCount { get; set; }

        public bool IsPrepared { get; set; }

        public string SelectedDeckName { get; set; }
    }

    public sealed class RoomInteractionActions
    {
        public Action<string> SendChat { get; set; }

        public Action<string> SetDeckInUse { get; set; }

        public Action<string> UpdateDeck { get; set; }

        public Action SendReady { get; set; }

        public Action SendNotReady { get; set; }

        public Action MoveToDuelist { get; set; }

        public Action MoveToObserver { get; set; }

        public Action StartDuel { get; set; }

        public Action<int> KickPlayer { get; set; }
    }

    public sealed class RoomCloseActions
    {
        public Action LeaveRoom { get; set; }
    }

    public sealed class RoomAiEntryActions
    {
        public Action SetDuelReturnTarget { get; set; }

        public Action ConnectToAiRoom { get; set; }
    }

    public sealed class RoomFlowService
    {
        private const string DeckDirectoryName = "deck";
        private const string DeckFileExtension = ".ydk";

        private readonly IPlatformPaths platformPaths;

        public RoomFlowService()
            : this(new RuntimePlatformPaths())
        {
        }

        public RoomFlowService(IPlatformPaths platformPaths)
        {
            if (platformPaths == null)
            {
                throw new ArgumentNullException("platformPaths");
            }

            this.platformPaths = platformPaths;
        }

        public bool SubmitChat(string message, RoomInteractionActions actions)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            Invoke(actions != null ? actions.SendChat : null, message);
            return true;
        }

        public void HandleReadyToggle(RoomSeatInteractionRequest request, RoomInteractionActions actions)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (request.IsPrepared)
            {
                Invoke(actions != null ? actions.SendNotReady : null);
                return;
            }

            UpdateSelectedDeck(request.SelectedDeckName, actions);
            Invoke(actions != null ? actions.SendReady : null);
        }

        public void SelectDeck(string deckName, RoomSeatInteractionRequest request, RoomInteractionActions actions)
        {
            Invoke(actions != null ? actions.SetDeckInUse : null, deckName);

            if (request != null && request.IsPrepared)
            {
                Invoke(actions != null ? actions.SendNotReady : null);
                UpdateSelectedDeck(deckName, actions);
                Invoke(actions != null ? actions.SendReady : null);
            }
        }

        public void HandlePrepareChanged(bool isPrepared, string selectedDeckName, RoomInteractionActions actions)
        {
            if (isPrepared)
            {
                UpdateSelectedDeck(selectedDeckName, actions);
                Invoke(actions != null ? actions.SendReady : null);
                return;
            }

            Invoke(actions != null ? actions.SendNotReady : null);
        }

        public void MoveToDuelist(RoomInteractionActions actions)
        {
            Invoke(actions != null ? actions.MoveToDuelist : null);
        }

        public void MoveToObserver(RoomInteractionActions actions)
        {
            Invoke(actions != null ? actions.MoveToObserver : null);
        }

        public void StartDuel(RoomInteractionActions actions)
        {
            Invoke(actions != null ? actions.StartDuel : null);
        }

        public void KickPlayer(int position, RoomInteractionActions actions)
        {
            Invoke(actions != null ? actions.KickPlayer : null, position);
        }

        public void Close(RoomCloseActions actions)
        {
            Invoke(actions != null ? actions.LeaveRoom : null);
        }

        public void EnterAiRoom(RoomAiEntryActions actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            Invoke(actions.SetDuelReturnTarget);
            Invoke(actions.ConnectToAiRoom);
        }

        public string GetDeckPath(string deckName)
        {
            return platformPaths.GetFilePath(DeckDirectoryName, (deckName ?? string.Empty) + DeckFileExtension);
        }

        private void UpdateSelectedDeck(string deckName, RoomInteractionActions actions)
        {
            if (string.IsNullOrEmpty(deckName))
            {
                return;
            }

            Invoke(actions != null ? actions.UpdateDeck : null, GetDeckPath(deckName));
        }

        private static void Invoke(Action action)
        {
            if (action != null)
            {
                action();
            }
        }

        private static void Invoke(Action<string> action, string value)
        {
            if (action != null)
            {
                action(value);
            }
        }

        private static void Invoke(Action<int> action, int value)
        {
            if (action != null)
            {
                action(value);
            }
        }
    }
}

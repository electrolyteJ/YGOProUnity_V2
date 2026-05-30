using System;
using System.Collections.Generic;

namespace App.Features.Duel.Services
{
    public enum DuelCloseResult
    {
        ServerSelect = 0,
        AssignedReturnTarget = 1,
        ExitApplication = 2,
    }

    public enum DuelSessionMode
    {
        LiveDuel = 0,
        LiveWatch = 1,
        ReplayRecord = 2,
    }

    public enum DuelResultConfirmationResult
    {
        ExitDuel = 0,
        EnterSideDeck = 1,
        HideCalculator = 2,
        PromptSurrender = 3,
    }

    public sealed class DuelCloseActions
    {
        public bool ExitOnReturn { get; set; }

        public bool HasAssignedReturnTarget { get; set; }

        public bool ReturnTargetIsDeckManager { get; set; }

        public Action SaveRecord { get; set; }

        public Action ShowAssignedReturnTarget { get; set; }

        public Action ShowServerSelect { get; set; }

        public Action ExitApplication { get; set; }
    }

    public sealed class DuelLiveSessionExitActions
    {
        public bool ExitOnReturn { get; set; }

        public bool HasAssignedReturnTarget { get; set; }

        public bool ReturnTargetIsDeckManager { get; set; }

        public Action DisconnectSession { get; set; }

        public Action StopAiServer { get; set; }

        public Action SaveRecord { get; set; }

        public Action ShowAssignedReturnTarget { get; set; }

        public Action ShowServerSelect { get; set; }

        public Action ExitApplication { get; set; }
    }

    public sealed class DuelResultConfirmationActions
    {
        public bool DuelEnded { get; set; }

        public bool Surrendered { get; set; }

        public bool HasConnectedSession { get; set; }

        public bool NeedSide { get; set; }

        public bool IsDuelCondition { get; set; }

        public Action ResetReconnectState { get; set; }

        public Action ExitDuel { get; set; }

        public Action EnterSideDeck { get; set; }

        public Action HideCalculator { get; set; }

        public Action PromptSurrender { get; set; }
    }

    public sealed class DuelFlowService
    {
        public DuelCloseResult ResolveCloseResult(bool exitOnReturn, bool hasAssignedReturnTarget, bool returnTargetIsDeckManager)
        {
            if (exitOnReturn && !returnTargetIsDeckManager)
            {
                return DuelCloseResult.ExitApplication;
            }

            if (hasAssignedReturnTarget)
            {
                return DuelCloseResult.AssignedReturnTarget;
            }

            return DuelCloseResult.ServerSelect;
        }

        public DuelSessionMode ResolveSessionMode(bool isReplayRecord, bool isObserver)
        {
            if (isReplayRecord)
            {
                return DuelSessionMode.ReplayRecord;
            }

            return isObserver ? DuelSessionMode.LiveWatch : DuelSessionMode.LiveDuel;
        }

        public bool TryResolveSessionTransition(DuelSessionMode currentMode, bool isReplayRecord, bool isObserver, out DuelSessionMode nextMode)
        {
            nextMode = ResolveSessionMode(isReplayRecord, isObserver);
            return nextMode != currentMode;
        }

        public bool TryResolveSessionCondition<TCondition>(
            TCondition currentCondition,
            TCondition duelCondition,
            TCondition watchCondition,
            TCondition recordCondition,
            bool isObserver,
            out TCondition nextCondition)
        {
            DuelSessionMode nextMode = ResolveSessionMode(
                EqualityComparer<TCondition>.Default.Equals(currentCondition, recordCondition),
                isObserver);
            nextCondition = ResolveCondition(nextMode, duelCondition, watchCondition, recordCondition);
            return !EqualityComparer<TCondition>.Default.Equals(currentCondition, nextCondition);
        }

        public DuelResultConfirmationResult ResolveDuelResultConfirmation(
            bool duelEnded,
            bool surrendered,
            bool hasConnectedSession,
            bool needSide,
            bool isDuelCondition)
        {
            if (duelEnded || surrendered || !hasConnectedSession)
            {
                return DuelResultConfirmationResult.ExitDuel;
            }

            if (needSide)
            {
                return DuelResultConfirmationResult.EnterSideDeck;
            }

            return isDuelCondition
                ? DuelResultConfirmationResult.PromptSurrender
                : DuelResultConfirmationResult.HideCalculator;
        }

        public void ConfirmDuelResult(DuelResultConfirmationActions actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            Invoke(actions.ResetReconnectState);

            DuelResultConfirmationResult result = ResolveDuelResultConfirmation(
                actions.DuelEnded,
                actions.Surrendered,
                actions.HasConnectedSession,
                actions.NeedSide,
                actions.IsDuelCondition);

            switch (result)
            {
                case DuelResultConfirmationResult.ExitDuel:
                    Invoke(actions.ExitDuel);
                    break;
                case DuelResultConfirmationResult.EnterSideDeck:
                    Invoke(actions.EnterSideDeck);
                    break;
                case DuelResultConfirmationResult.HideCalculator:
                    Invoke(actions.HideCalculator);
                    break;
                default:
                    Invoke(actions.PromptSurrender);
                    break;
            }
        }

        public void Close(DuelCloseActions actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            Invoke(actions.SaveRecord);

            DuelCloseResult result = ResolveCloseResult(actions.ExitOnReturn, actions.HasAssignedReturnTarget, actions.ReturnTargetIsDeckManager);
            if (result == DuelCloseResult.ExitApplication)
            {
                Invoke(actions.ExitApplication);
                return;
            }

            if (result == DuelCloseResult.AssignedReturnTarget)
            {
                Invoke(actions.ShowAssignedReturnTarget);
                return;
            }

            Invoke(actions.ShowServerSelect);
        }

        public void ExitLiveSession(DuelLiveSessionExitActions actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            Invoke(actions.DisconnectSession);
            Invoke(actions.StopAiServer);

            Close(new DuelCloseActions
            {
                ExitOnReturn = actions.ExitOnReturn,
                HasAssignedReturnTarget = actions.HasAssignedReturnTarget,
                ReturnTargetIsDeckManager = actions.ReturnTargetIsDeckManager,
                SaveRecord = actions.SaveRecord,
                ShowAssignedReturnTarget = actions.ShowAssignedReturnTarget,
                ShowServerSelect = actions.ShowServerSelect,
                ExitApplication = actions.ExitApplication
            });
        }

        private static void Invoke(Action action)
        {
            if (action != null)
            {
                action();
            }
        }

        private static TCondition ResolveCondition<TCondition>(
            DuelSessionMode mode,
            TCondition duelCondition,
            TCondition watchCondition,
            TCondition recordCondition)
        {
            switch (mode)
            {
                case DuelSessionMode.LiveWatch:
                    return watchCondition;
                case DuelSessionMode.ReplayRecord:
                    return recordCondition;
                default:
                    return duelCondition;
            }
        }
    }
}

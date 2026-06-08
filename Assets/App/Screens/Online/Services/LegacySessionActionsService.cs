using App.Core;
namespace App.Screens.Online.Services
{
    /// <summary>
    /// Legacy session actions adapter - provides backward-compatible action implementations
    /// for OnlineSessionFlowService by delegating to legacy Program servants.
    ///
    /// This bridges the gap between App-owned session flow and legacy servant state.
    /// Controlled legacy seam - future tasks will replace with native App implementations.
    /// </summary>
    public sealed class LegacySessionActionsService
    {
        private readonly IMenuServantSource menuServants;
        private readonly IOcgcoreServantSource ocgcoreServants;
        private readonly ICardDescriptionServantSource cardDescriptionServants;
        private readonly ISelectServerServantSource selectServerServants;
        private readonly IServantTransition servantTransitions;
        private readonly IReplayRecordBuffer replayRecordBuffer;

        public LegacySessionActionsService(
            IMenuServantSource menuServants,
            IOcgcoreServantSource ocgcoreServants,
            ICardDescriptionServantSource cardDescriptionServants,
            ISelectServerServantSource selectServerServants,
            IServantTransition servantTransitions,
            IReplayRecordBuffer replayRecordBuffer)
        {
            this.menuServants = menuServants;
            this.ocgcoreServants = ocgcoreServants;
            this.cardDescriptionServants = cardDescriptionServants;
            this.selectServerServants = selectServerServants;
            this.servantTransitions = servantTransitions;
            this.replayRecordBuffer = replayRecordBuffer;
        }

        public OnlineSessionDisconnectActions CreateDisconnectActions()
        {
            return new OnlineSessionDisconnectActions
            {
                ReturnToAssignedTarget = delegate
                {
                    Ocgcore ocgcore = ocgcoreServants.ocgcore;
                    if (ocgcore != null && ocgcore.returnServant != null)
                    {
                        servantTransitions.shiftToServant(ocgcore.returnServant);
                    }
                },
                ReturnToSelectServer = delegate
                {
                    SelectServer selectServer = selectServerServants.selectServer;
                    if (selectServer != null)
                    {
                        servantTransitions.shiftToServant(selectServer);
                    }
                },
                ShowDisconnectedMessage = delegate
                {
                    CardDescription cardDescription = cardDescriptionServants.cardDescription;
                    if (cardDescription != null)
                    {
                        cardDescription.RMSshow_none(InterString.Get("连接被断开。"));
                    }
                },
                ShowOpponentLeftMessage = delegate
                {
                    CardDescription cardDescription = cardDescriptionServants.cardDescription;
                    if (cardDescription != null)
                    {
                        cardDescription.RMSshow_none(InterString.Get("对方离开游戏，您现在可以截图。"));
                    }
                },
                ClearReplayRecordBuffer = delegate
                {
                    replayRecordBuffer.clearReplayRecordBuffer();
                },
                ForceQuitDuel = delegate
                {
                    Ocgcore ocgcore = ocgcoreServants.ocgcore;
                    if (ocgcore != null)
                    {
                        ocgcore.forceMSquit();
                    }
                }
            };
        }

        public OnlineSessionDisconnectRequest CreateDisconnectRequest()
        {
            global::Menu menu = menuServants.menu;
            Ocgcore ocgcore = ocgcoreServants.ocgcore;

            return new OnlineSessionDisconnectRequest
            {
                IsDuelVisible = ocgcore != null ? ocgcore.isShowed : false,
                IsMenuVisible = menu != null ? menu.isShowed : false,
                HasAssignedReturnTarget = ocgcore != null ? ocgcore.returnServant != null : false
            };
        }
    }
}

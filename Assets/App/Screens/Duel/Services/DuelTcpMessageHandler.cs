using System.IO;
using YGOSharp.Network.Enums;
using App.Core;
namespace App.Screens.Duel.Services
{
    /// <summary>
    /// Duel TCP message handler - owns StocMessage dispatch for duel screens.
    /// Replaces the former direct duel-servant dispatch from LegacyTcpDispatchBridge.
    ///
    /// This is a controlled legacy adapter: it encapsulates duel TCP message routing
    /// within App.Screens.Duel while delegating to the legacy Ocgcore servant for backward compatibility.
    /// Future tasks will replace the legacy delegation with native App-owned handlers.
    /// </summary>
    public sealed class DuelTcpMessageHandler
    {
        private readonly IOcgcoreServantSource ocgcoreServants;

        public DuelTcpMessageHandler(IOcgcoreServantSource ocgcoreServants)
        {
            this.ocgcoreServants = ocgcoreServants;
        }

        public bool HandleMessage(StocMessage message, BinaryReader reader)
        {
            Ocgcore ocgcore = ocgcoreServants.ocgcore;
            if (ocgcore == null)
            {
                return false;
            }

            switch (message)
            {
                case StocMessage.TimeLimit:
                    ocgcore.StocMessage_TimeLimit(reader);
                    return true;
                default:
                    return false;
            }
        }
    }
}

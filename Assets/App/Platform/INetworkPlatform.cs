using UnityEngine;

namespace App.Platform
{
    public sealed class LegacyNetworkPlatform : App.Core.INetworkPlatform
    {
        public bool IsAvailable
        {
            get { return Application.internetReachability != NetworkReachability.NotReachable; }
        }
    }
}

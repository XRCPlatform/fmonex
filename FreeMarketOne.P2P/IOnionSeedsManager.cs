using FreeMarketOne.P2P.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.P2P
{
    public interface IOnionSeedsManager : IDisposable
    {
        List<OnionSeedPeer> OnionSeedPeers { get; set; }

        bool IsOnionSeedsManagerRunning();

        void Start();
    }
}

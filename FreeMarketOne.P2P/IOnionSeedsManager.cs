using FreeMarketOne.DataStructure;
using FreeMarketOne.P2P.Models;
using Libplanet.Net;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FreeMarketOne.P2P
{
    public interface IOnionSeedsManager : IDisposable
    {
        List<OnionSeedPeer> OnionSeedPeers { get; set; }

        Swarm<BaseAction> BaseSwarm { get; set; }
        Swarm<MarketAction> MarketSwarm { get; set; }

        bool IsOnionSeedsManagerRunning();

        Task Start();
    }
}

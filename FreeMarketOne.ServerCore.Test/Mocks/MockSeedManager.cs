using FreeMarketOne.DataStructure;
using FreeMarketOne.P2P;
using FreeMarketOne.P2P.Models;
using Libplanet.Net;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FreeMarketOne.BlockChain.Test.Mocks
{
    public class MockSeedManager : IOnionSeedsManager, IDisposable
    {
        public List<OnionSeedPeer> OnionSeedPeers { get; set; }
        public Swarm<BaseAction> BaseSwarm { get; set; }
        public Swarm<MarketAction> MarketSwarm { get; set; }

        public MockSeedManager ()
        {
            OnionSeedPeers = new List<OnionSeedPeer>();
        }

        public void Dispose()
        {

        }

        public bool IsOnionSeedsManagerRunning()
        {
            return true;
        }

        public Task Start()
        {
            throw new NotImplementedException();
        }
    }
}

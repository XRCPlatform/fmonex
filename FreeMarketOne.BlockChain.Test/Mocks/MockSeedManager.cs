using FreeMarketOne.P2P;
using FreeMarketOne.P2P.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.BlockChain.Test.Mocks
{
    public class MockSeedManager : IOnionSeedsManager, IDisposable
    {
        public List<OnionSeedPeer> OnionSeedPeers { get; set; }

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

        public void Start()
        {
            
        }
    }
}

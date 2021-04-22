using System.Collections.Generic;
using FreeMarketOne.P2P.Models;
using FreeMarketOne.ServerCore;
using Libplanet.Net;

namespace FreeMarketOne.WebApi.Models
{
    public class InfoModel
    {
        // public DataDir DataDir { get; set; }
        public List<OnionSeedPeer> OnionSeedPeers { get; set; }
        public string PublicKey { get; set; }
        public IEnumerable<BoundPeer> CurrentBasePeers { get; set; }
        public IEnumerable<BoundPeer> CurrentMarketPeers { get; set; }
    }
}
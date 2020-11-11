using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.P2P.Models
{
    public class OnionSeedPeer
    {
        public string UrlTor { get; set; }
        public int PortTor { get; set; }

        public string UrlBlockChain { get; set; }
        public int PortBlockChainBase { get; set; }
        public int PortBlockChainMaster { get; set; }

        public string SecretKeyHex { get; set; }
    }
}

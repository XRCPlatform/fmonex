using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.P2P.Models
{
    public class OnionSeedPeer
    {
        public enum OnionSeedStates
        {
            Unknown = 0,
            Online = 1,
            Offline = 2
        }

        public string UrlTor { get; set; }
        public int PortTor { get; set; }

        public string UrlBlockChain { get; set; }
        public int PortBlockChainBase { get; set; }
        public int PortBlockChainMaster { get; set; }

        public string SecretKeyHex { get; set; }
        public OnionSeedStates State { get; set; }
    }
}

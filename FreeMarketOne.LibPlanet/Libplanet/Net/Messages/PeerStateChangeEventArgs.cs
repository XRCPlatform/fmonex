using System;
using System.Collections.Generic;
using System.Text;

namespace Libplanet.Net.Messages
{
    public class PeerStateChangeEventArgs : EventArgs
    {
        public BoundPeer Peer { get; set; }
        public PeerStateChange Change { get; set; }
    }
}

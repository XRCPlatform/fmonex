using FreeMarketOne.Tor;
using FreeMarketOne.Tor.TorOverTcp.Models.Messages;
using System;

namespace Libplanet.Net.Messages
{
	//FMONECHANGE -  added new event args class for TOR transport
    public class ReceivedRequestEventArgs : EventArgs
    {
        public BoundPeer Peer { get; set; }
        public TotClient Client { get; set; }
        public TotRequest Request { get; set; }
        public MessageType MessageType { get; set; }
        public Envelope Envelope { get; set; }
    }
}

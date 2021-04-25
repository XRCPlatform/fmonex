using System;

namespace Libplanet.Net.Messages
{
	//FMONECHANGE -  added new event agrs class for tracking peer state changes outside of LibPlanet code
    public class PeerStateChangeEventArgs : EventArgs
    {
        public BoundPeer Peer { get; set; }
        public PeerStateChange Change { get; set; }
    }
}

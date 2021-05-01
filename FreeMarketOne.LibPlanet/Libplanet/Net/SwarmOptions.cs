using System;
using System.Threading;
using Libplanet.Blocks;
using Libplanet.Net.Messages;
using Libplanet.Net.Protocols;
using Libplanet.Tx;

namespace Libplanet.Net
{
    public class SwarmOptions
    {
        /// <summary>
        /// The maximum timeout used in <see cref="Swarm{T}"/>.
        /// </summary>
		//FMONECHANGE -  changed durations
        public TimeSpan MaxTimeout { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// The base timeout used to receive the block hashes from other peers.
        /// </summary>
		//FMONECHANGE -  changed durations
        public TimeSpan BlockHashRecvTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The base timeout used to receive <see cref="Block{T}"/> from other peers.
        /// </summary>
		//FMONECHANGE -  changed durations
        public TimeSpan BlockRecvTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The base timeout used to receive <see cref="Transaction{T}"/> from other peers.
        /// </summary>
		//FMONECHANGE -  changed durations
        public TimeSpan TxRecvTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The timeout used to receive recent states from other peers.
        /// </summary>
		//FMONECHANGE -  changed durations
        public TimeSpan RecentStateRecvTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The timeout used to block download in preloading.
        /// </summary>
        public TimeSpan BlockDownloadTimeout { get; set; } = Timeout.InfiniteTimeSpan;

        /// <summary>
        /// The lifespan of block demand.
        /// </summary>
        public TimeSpan BlockDemandLifespan { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The lifespan of message.
        /// </summary>
        public TimeSpan? MessageLifespan { get; set; } = null;

        /// <summary>
        /// The frequency of <see cref="IProtocol.RefreshTableAsync" />.
        /// </summary>
        public TimeSpan RefreshPeriod { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The lifespan of <see cref="Peer"/> in routing table.
        /// <seealso cref="IProtocol.RefreshTableAsync" />
        /// </summary>
        public TimeSpan RefreshLifespan { get; set; } = TimeSpan.FromSeconds(60);
	
        /// <summary>
        /// Use this SOCKS5 proxy
        /// </summary>
		//FMONECHANGE -  changed added Socks5Proxy
        public string Socks5Proxy { get; set; }
    }
}

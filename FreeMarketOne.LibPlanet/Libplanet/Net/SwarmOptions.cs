using System;
using System.Threading;
using Libplanet.Blocks;
using Libplanet.Tx;

namespace Libplanet.Net
{
    public class SwarmOptions
    {
        /// <summary>
        /// The maximum timeout used in <see cref="Swarm{T}"/>.
        /// </summary>
        public TimeSpan MaxTimeout { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// The base timeout used to receive the block hashes from other peers.
        /// </summary>
        public TimeSpan BlockHashRecvTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The base timeout used to receive <see cref="Block{T}"/> from other peers.
        /// </summary>
        public TimeSpan BlockRecvTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The base timeout used to receive <see cref="Transaction{T}"/> from other peers.
        /// </summary>
        public TimeSpan TxRecvTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The timeout used to receive recent states from other peers.
        /// </summary>
        public TimeSpan RecentStateRecvTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The timeout used to block download in preloading.
        /// </summary>
        public TimeSpan BlockDownloadTimeout { get; set; } = Timeout.InfiniteTimeSpan;

        /// <summary>
        /// Use this SOCKS5 proxy
        /// </summary>
        public string Socks5Proxy { get; set; }
    }
}

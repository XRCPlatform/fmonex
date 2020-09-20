using Libplanet.Action;
using Libplanet.Blockchain.Policies;
using System;
using System.Collections.Generic;
using System.Text;

namespace Libplanet.Extensions
{
    public interface IDefaultBlockPolicy<T> : IBlockPolicy<T>
        where T : IAction, new()
    {
        /// <summary>
        /// An appropriate interval between consecutive <see cref="Block{T}"/>s.
        /// </summary>
        TimeSpan BlockInterval { get; }

        /// <summary>
        /// An appropriate interval for pool check if there is new txs to start/end new mining.
        /// </summary>
        TimeSpan PoolCheckInterval { get; }

        /// <summary>
        /// If Blockchain has expiration this value cant be null
        /// </summary>
        TimeSpan? ValidBlockInterval { get; }

        /// <summary>
        /// Return approx time to mine next block
        /// </summary>
        /// <returns></returns>
        TimeSpan GetApproxTimeSpanToMineNextBlock();
    }
}

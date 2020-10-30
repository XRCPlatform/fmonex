using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.Pools
{
    public class PoolManagerStates
    {
        public enum Errors
        {
            /// <summary>
            /// There is no minimal peer amount to accept this action 
            /// </summary>
            NoMinimalPeer = 0,

            /// <summary>
            /// Hash doesnt equal to data
            /// </summary>
            NoValidContentHash = 1,

            /// <summary>
            /// Item exist in pool
            /// </summary>
            Duplication = 2,

            /// <summary>
            /// This type of content insn't valid for this blockchain
            /// </summary>
            WrontTypeOfContent = 3,

            /// <summary>
            /// You can't process this item because there is new state of this object in network pools.
            /// </summary>
            StateOfItemIsInProgress = 4,

            /// <summary>
            /// Unexpected error
            /// </summary>
            Unexpected = 5,

            /// <summary>
            /// Too much staged tx
            /// </summary>
            TooMuchStagedTx = 6,

            /// <summary>
            /// No local action items for propagation
            /// </summary>
            NoLocalActionItems = 7
        }
    }
}

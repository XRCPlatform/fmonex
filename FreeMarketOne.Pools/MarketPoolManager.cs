using FreeMarketOne.DataStructure;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Extensions;
using Libplanet.Net;
using Libplanet.Store;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.Pools
{
    public class MarketPoolManager : PoolManager<MarketAction>
    {
        public MarketPoolManager(
            IBaseConfiguration configuration,
            string memoryPoolFilePath,
            Swarm<MarketAction> swarmServer,
            PrivateKey privateKey,
            BlockChain<MarketAction> blockChain,
            IDefaultBlockPolicy<MarketAction> blockPolicy)
            : base(configuration, memoryPoolFilePath, swarmServer, privateKey, blockChain, blockPolicy)
        {
        }
    }
}

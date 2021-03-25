using FreeMarketOne.DataStructure;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Extensions;
using Libplanet.Net;
using LibPlanet.SQLite;
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
            SQLiteStore storage,
            Swarm<MarketAction> swarmServer,
            PrivateKey privateKey,
            BlockChain<MarketAction> blockChain,
            IDefaultBlockPolicy<MarketAction> blockPolicy)
            : base(configuration, memoryPoolFilePath, storage, swarmServer, privateKey, blockChain, blockPolicy)
        {
        }
    }
}

using FreeMarketOne.BlockChain.Actions;
using FreeMarketOne.DataStructure;
using Libplanet.Net;
using Libplanet.RocksDBStore;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.PoolManager
{
    public class MarketPoolManager : PoolManager<MarketBlockChainAction>
    {
        public MarketPoolManager(
            Logger serverLogger,
            string memoryPoolFilePath,
            RocksDBStore storage,
            Swarm<MarketBlockChainAction> swarmServer)
            : base(serverLogger, memoryPoolFilePath, storage, swarmServer)
        {
        }
    }
}

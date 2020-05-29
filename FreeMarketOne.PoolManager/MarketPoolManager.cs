using FreeMarketOne.DataStructure;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.RocksDBStore;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.PoolManager
{
    public class MarketPoolManager : PoolManager<MarketAction>
    {
        public MarketPoolManager(
            IBaseConfiguration configuration,
            string memoryPoolFilePath,
            RocksDBStore storage,
            Swarm<MarketAction> swarmServer,
            PrivateKey privateKey)
            : base(configuration, memoryPoolFilePath, storage, swarmServer, privateKey)
        {
        }
    }
}

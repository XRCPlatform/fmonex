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
    public class BasePoolManager : PoolManager
    {
        public BasePoolManager(
            Logger serverLogger,
            string memoryPoolFilePath,
            RocksDBStore storage,
            Swarm<BaseBlockChainAction> swarmServer) 
            : base(serverLogger, memoryPoolFilePath, storage, swarmServer)
        {
        }
    }
}

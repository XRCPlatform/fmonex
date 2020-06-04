using FreeMarketOne.BlockChain.Policy;
using FreeMarketOne.DataStructure;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Extensions;
using Libplanet.Net;
using Libplanet.RocksDBStore;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.PoolManager
{
    public class BasePoolManager : PoolManager<BaseAction>
    {
        public BasePoolManager(
            IBaseConfiguration configuration,
            string memoryPoolFilePath,
            RocksDBStore storage,
            Swarm<BaseAction> swarmServer,
            PrivateKey privateKey,
            BlockChain<BaseAction> blockChain,
            IDefaultBlockPolicy<BaseAction> blockPolicy) 
            : base(configuration, memoryPoolFilePath, storage, swarmServer, privateKey, blockChain, blockPolicy)
        {
        }
    }
}

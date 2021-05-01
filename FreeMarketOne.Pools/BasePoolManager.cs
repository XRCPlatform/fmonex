using FreeMarketOne.DataStructure;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Extensions;
using Libplanet.Net;
using Libplanet.Store;

namespace FreeMarketOne.Pools
{
    public class BasePoolManager : PoolManager<BaseAction>
    {
        public BasePoolManager(
            IBaseConfiguration configuration,
            string memoryPoolFilePath,
            Swarm<BaseAction> swarmServer,
            PrivateKey privateKey,
            BlockChain<BaseAction> blockChain,
            IDefaultBlockPolicy<BaseAction> blockPolicy) 
            : base(configuration, memoryPoolFilePath, swarmServer, privateKey, blockChain, blockPolicy)
        {
        }
    }
}

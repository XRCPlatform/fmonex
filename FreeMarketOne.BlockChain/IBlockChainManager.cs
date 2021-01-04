using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Extensions;
using Libplanet.Net;
using Libplanet.Store;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FreeMarketOne.BlockChain
{
    public interface IBlockChainManager<T> : IDisposable where T : IBaseAction, new()
    {
        BlockChain<T> BlockChain { get; }
        DefaultStore Storage { get; }
        Swarm<T> SwarmServer { get; }
        UserPrivateKey PrivateKey { get; }

        bool Start();
        bool IsBlockChainManagerRunning();
        void Stop();
        List<IBaseItem> GetActionItemsByType(Type type);
        Task ReConnectAfterNetworkLossAsync();
    }
}
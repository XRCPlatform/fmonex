using FreeMarketOne.BlockChain.Actions;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.RocksDBStore;
using System;
using System.Collections.Generic;

namespace FreeMarketOne.BlockChain
{
    public interface IBlockChainManager<T> : IDisposable where T : IBaseAction, new()
    {
        BlockChain<T> BlockChain { get; }
        RocksDBStore Storage { get; }
        Swarm<T> SwarmServer { get; }
        PrivateKey PrivateKey { get; }

        bool Start();
        bool IsBlockChainManagerRunning();
        void Stop();
        List<IBaseItem> GetActionItemsByType(Type type);
    }
}
using FreeMarketOne.DataStructure.Objects.BaseItems;
using System;
using System.Collections.Generic;

namespace FreeMarketOne.BlockChain
{
    public interface IBlockChainManager : IDisposable
    {
        bool Start();
        bool IsBlockChainManagerRunning();
        void Stop();
        List<IBaseItem> GetActionItemsByType(Type type);
    }
}
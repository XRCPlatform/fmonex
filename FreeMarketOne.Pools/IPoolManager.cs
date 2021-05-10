using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using System;
using System.Collections.Generic;

namespace FreeMarketOne.Pools
{
    public interface IPoolManager : IDisposable 
    {
        bool Start();
        bool IsPoolManagerRunning();
        void Stop();
        PoolManagerStates.Errors? AcceptActionItem(IBaseItem actionItem, bool forceIt = false);
        bool SaveActionItemsToFile();
        bool LoadActionItemsFromFile();
        PoolManagerStates.Errors? CheckActionItemInProcessing(IBaseItem actionItem);
        bool ClearActionItemsBasedOnHashes(List<string> hashs);

        IBaseItem GetActionItemLocal(string hash);
        List<IBaseItem> GetAllActionItemLocal();
        bool DeleteActionItemLocal(string hash);
        PoolManagerStates.Errors? PropagateAllActionItemLocal(bool forceIt = false);

        List<IBaseItem> GetAllActionItemStaged();
        IBaseItem GetActionItemStaged(string hash);

        List<IBaseAction> GetAllActionStaged();
    }
}

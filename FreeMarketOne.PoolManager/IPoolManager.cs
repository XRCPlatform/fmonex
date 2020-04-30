﻿using FreeMarketOne.BlockChain.Actions;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using System;
using System.Collections.Generic;

namespace FreeMarketOne.PoolManager
{
    public interface IPoolManager : IDisposable
    {
        bool Start();
        bool IsPoolManagerRunning();
        void Stop();
        bool AcceptActionItem(IBaseItem actionItem);
        bool SaveActionItemsToFile();
        bool LoadActionItemsFromFile();
        bool CheckActionItemInProcessing(IBaseItem actionItem);
        bool ClearActionItemsBasedOnHashes(List<string> hashs);

        IBaseItem GetActionItemLocal(string hash);
        List<IBaseItem> GetAllActionItemLocal();
        bool DeleteActionItemLocal(string hash);
        bool PropagateAllActionItemLocal(List<IBaseAction> extraActions);

        List<IBaseItem> GetAllActionItemStaged();
        IBaseItem GetActionItemStaged();

        List<IBaseAction> GetAllActionsStaged();
    }
}
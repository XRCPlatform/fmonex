using FreeMarketOne.DataStructure.Objects.MarketItems;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.MarketPool
{
    public interface IMarketPoolManager : IDisposable
    {
        bool Start();
        bool IsMarketPoolManagerRunning();
        void Stop();
        bool AcceptTx(IMarketItem tx);
        bool SaveTxsToFile();
        bool LoadTxsFromFile();
        bool CheckTxInProcessing(IMarketItem tx);
        bool ClearTxBasedOnHashes(List<string> hashs);
    }
}
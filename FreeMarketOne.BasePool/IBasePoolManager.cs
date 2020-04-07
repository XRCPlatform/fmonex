using FreeMarketOne.DataStructure.Objects.BaseItems;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace FreeMarketOne.BasePool
{
    public interface IBasePoolManager : IDisposable
    {
        bool Start();
        bool IsBasePoolManagerRunning();
        void Stop();
        bool AcceptTx();
        bool SaveTx();
        bool LoadTx();
        bool CheckTxInProcessing(IBaseItem tx);
       // List<TxBasePool> GetTxInNewBlock();
    }
}

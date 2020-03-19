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
    }
}
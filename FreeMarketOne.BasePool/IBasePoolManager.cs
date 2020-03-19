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
    }
}

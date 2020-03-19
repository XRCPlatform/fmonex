using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.Mining
{
    public interface IMiningProcessor : IDisposable
    {
        bool Start();
        bool IsMiningRunning();
        void Stop();
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.P2P
{
    public interface IOnionSeedsManager : IDisposable
    {
        bool IsOnionSeedsManagerRunning();

        void Start();
    }
}

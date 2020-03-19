using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.P2P
{
    public interface IOnionSeedsManager : IDisposable
    {
        void GetOnions();
        void StartPeriodicCheck();
        void StartPeriodicPeerBroadcast();
    }
}

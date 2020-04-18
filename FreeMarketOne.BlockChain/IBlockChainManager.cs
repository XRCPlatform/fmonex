using System;

namespace FreeMarketOne.BlockChain
{
    public interface IBlockChainManager : IDisposable
    {

        bool Start();
        bool IsBlockChainManagerRunning();
        void Stop();
    }
}
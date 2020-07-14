using FreeMarketOne.BlockChain.Test.Helpers;
using FreeMarketOne.DataStructure;
using FreeMarketOne.P2P;
using FreeMarketOne.PoolManager;
using Libplanet.Blockchain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace FreeMarketOne.BlockChain.Test
{
    [TestClass]
    internal class PoolManagerTest
    {
        private IBaseConfiguration _configuration;
        private ILogger _logger;
        private IOnionSeedsManager _onionSeedsManager;
        private BasePoolManager _basePoolManager;
 
        private event EventHandler _baseBlockChainLoadedEvent;
        private IBlockChainManager<BaseAction> _baseBlockChainManager;

        private void BaseBlockChainLoaded(object sender, EventArgs e)
        {
            /* Initialize Market BlockChain Manager */
            if (_baseBlockChainManager.IsBlockChainManagerRunning())
            {
                /* Initialize Base And Market Pool */
                _basePoolManager = new BasePoolManager(
                    _configuration,
                    _configuration.MemoryBasePoolPath,
                    _baseBlockChainManager.Storage,
                    _baseBlockChainManager.SwarmServer,
                    _baseBlockChainManager.PrivateKey,
                    _baseBlockChainManager.BlockChain,
                    _configuration.BlockChainBasePolicy);
                _basePoolManager.Start();

            }
            else
            {
                _logger.Error("Debug Base Chain isnt loaded!");
            }
        }

        [TestMethod]
        internal void RunPoolManagerTest()
        {
            _baseBlockChainLoadedEvent += new EventHandler(BaseBlockChainLoaded);

            DebugEnvironmentHelper.Initialize<MiningTest>(
                _configuration,
                _logger,
                _onionSeedsManager,
                _basePoolManager,
                _baseBlockChainManager,
                _baseBlockChainLoadedEvent,
                null);

            SpinWait.SpinUntil(() => _baseBlockChainManager.IsBlockChainManagerRunning());
            SpinWait.SpinUntil(() => _basePoolManager != null && _basePoolManager.IsPoolManagerRunning());


        }
    }
}

﻿using FreeMarketOne.BlockChain.Test.Helpers;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.P2P;
using FreeMarketOne.PoolManager;
using Libplanet;
using Libplanet.Blockchain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FreeMarketOne.BlockChain.Test
{
    [TestClass]
    public class PoolManagerTest
    {
        private IBaseConfiguration _configuration;
        private ILogger _logger;
        private IOnionSeedsManager _onionSeedsManager;
        private BasePoolManager _basePoolManager;
        private event EventHandler _baseBlockChainLoadedEvent;
        private event EventHandler<BlockChain<BaseAction>.TipChangedEventArgs> _baseBlockChainChangedEvent;
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

            }
            else
            {
                _logger.Error("Debug Base Chain isnt loaded!");
            }
        }

        [TestMethod]
        public void RunPoolManagerTest()
        {
            var debugEnvironment = new DebugEnvironmentHelper();
            _baseBlockChainLoadedEvent += new EventHandler(BaseBlockChainLoaded);
            _baseBlockChainChangedEvent = null;

            debugEnvironment.Initialize<MiningTest>(
                ref _configuration,
                ref _logger,
                ref _onionSeedsManager,
                ref _basePoolManager,
                ref _baseBlockChainManager,
                ref _baseBlockChainLoadedEvent,
                ref _baseBlockChainChangedEvent);

            SpinWait.SpinUntil(() => _baseBlockChainManager.IsBlockChainManagerRunning());
            SpinWait.SpinUntil(() => _basePoolManager != null);

            //generate new test action
            var testActionItem1 = new CheckPointMarketDataV1();
            var genesisBlock = BlockChain<MarketAction>.MakeGenesisBlock();
            testActionItem1.Block = ByteUtil.Hex(genesisBlock.Serialize());

            var testActionItem2 = new ReviewUserDataV1();
            testActionItem2.ReviewDateTime = DateTime.UtcNow.AddMinutes(-1);
            testActionItem2.Message = "This is a test message";
            testActionItem2.Hash = testActionItem2.GenerateHash();

            Assert.IsFalse(_basePoolManager.CheckActionItemInProcessing(testActionItem1));
            testActionItem1.Hash = testActionItem1.GenerateHash();
            Assert.IsTrue(_basePoolManager.CheckActionItemInProcessing(testActionItem1));

            Assert.IsTrue(_basePoolManager.AcceptActionItem(testActionItem1));
            Assert.IsTrue(_basePoolManager.AcceptActionItem(testActionItem2));

            Assert.IsTrue(_basePoolManager.SaveActionItemsToFile());
            Assert.AreEqual(2, _basePoolManager.GetAllActionItemLocal().Count());

            //clear pool
            Assert.IsTrue(_basePoolManager.ClearActionItemsBasedOnHashes(new[] { testActionItem1.Hash, testActionItem2.Hash }.ToList()));
            Assert.AreEqual(0, _basePoolManager.GetAllActionItemLocal().Count());

            //load them from file
            Assert.IsTrue(_basePoolManager.LoadActionItemsFromFile());
            Assert.AreEqual(2, _basePoolManager.GetAllActionItemLocal().Count());

            //try to delete and delete again first action
            Assert.IsTrue(_basePoolManager.DeleteActionItemLocal(testActionItem1.Hash));
            Assert.IsFalse(_basePoolManager.DeleteActionItemLocal(testActionItem1.Hash));

            //again add it and add again the same
            Assert.IsTrue(_basePoolManager.AcceptActionItem(testActionItem1));
            Assert.IsFalse(_basePoolManager.AcceptActionItem(testActionItem1));

            Assert.IsNull(_basePoolManager.GetActionItemStaged("UNKNOWN HASH"));
            Assert.IsNotNull(_basePoolManager.GetActionItemLocal(testActionItem1.Hash));

            //complete tx and send it to network
            SpinWait.SpinUntil(() => _baseBlockChainManager.SwarmServer.Running);
            _basePoolManager.PropagateAllActionItemLocal();

        }
    }
}
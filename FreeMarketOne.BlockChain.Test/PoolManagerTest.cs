using FreeMarketOne.BlockChain.Test.Helpers;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.P2P;
using FreeMarketOne.Pools;
using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.Extensions;
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
        private event EventHandler<(Block<BaseAction> OldTip, Block<BaseAction> NewTip)> _baseBlockChainChangedEvent;
        private IBlockChainManager<BaseAction> _baseBlockChainManager;
        private UserPrivateKey _userPrivateKey;
        private bool _newBlock = false;

        private void BaseBlockChainLoaded(object sender, EventArgs e)
        {
            /* Initialize Market BlockChain Manager */
            if (_baseBlockChainManager.IsBlockChainManagerRunning())
            {
                /* Initialize Base And Market Pool */
                _basePoolManager = new BasePoolManager(
                    _configuration,
                    _configuration.MemoryBasePoolPath,
                    _baseBlockChainManager.SwarmServer,
                    _baseBlockChainManager.PrivateKey,
                    _baseBlockChainManager.BlockChain,
                    ((ExtendedConfiguration)_configuration).BlockChainBasePolicy);

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
                ref _userPrivateKey,
                ref _baseBlockChainLoadedEvent,
                ref _baseBlockChainChangedEvent);

            SpinWait.SpinUntil(() => _baseBlockChainManager.IsBlockChainManagerRunning());
            SpinWait.SpinUntil(() => _basePoolManager != null);

            //generate new test action
            var testActionItem1 = new CheckPointMarketDataV1();
            var genesisBlock = BlockChain<MarketAction>.MakeGenesisBlock();
            testActionItem1.Block = ByteUtil.Hex(genesisBlock.Serialize());

            var testActionItem2 = new UserDataV1();
            testActionItem2.UserName = "LoginName";
            testActionItem2.Description = "This is a test message";
            var bytesToSign = testActionItem2.ToByteArrayForSign();
            testActionItem2.Signature = Convert.ToBase64String(_userPrivateKey.Sign(bytesToSign));
            testActionItem2.Hash = testActionItem2.GenerateHash();

            Assert.IsNotNull(_basePoolManager.CheckActionItemInProcessing(testActionItem1));
            testActionItem1.Hash = testActionItem1.GenerateHash();
            Assert.IsNull(_basePoolManager.CheckActionItemInProcessing(testActionItem1));

            Assert.IsNull(_basePoolManager.AcceptActionItem(testActionItem1));
            Assert.IsNull(_basePoolManager.AcceptActionItem(testActionItem2));

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
            Assert.IsNull(_basePoolManager.AcceptActionItem(testActionItem1));
            Assert.IsNotNull(_basePoolManager.AcceptActionItem(testActionItem1));

            Assert.IsNull(_basePoolManager.GetActionItemStaged("UNKNOWN HASH"));
            Assert.IsNotNull(_basePoolManager.GetActionItemLocal(testActionItem1.Hash));

            //complete tx and send it to network
            SpinWait.SpinUntil(() => _baseBlockChainManager.SwarmServer.Running);
            _basePoolManager.PropagateAllActionItemLocal(true);

            Assert.AreEqual(0, _basePoolManager.GetAllActionItemLocalCount());
        }

        private void BaseBlockChainChanged(object sender, (Block<BaseAction> OldTip, Block<BaseAction> NewTip) e)
        {
            //we have a new block
            _newBlock = true;
        }

        [TestMethod]
        public void RunPoolManager12ItemsTest()
        {
            var debugEnvironment = new DebugEnvironmentHelper();
            _baseBlockChainLoadedEvent += new EventHandler(BaseBlockChainLoaded);
            _baseBlockChainChangedEvent += new EventHandler<(Block<BaseAction> OldTip, Block<BaseAction> NewTip)>(BaseBlockChainChanged);

            debugEnvironment.Initialize<MiningTest>(
                ref _configuration,
                ref _logger,
                ref _onionSeedsManager,
                ref _basePoolManager,
                ref _baseBlockChainManager,
                ref _userPrivateKey,
                ref _baseBlockChainLoadedEvent,
                ref _baseBlockChainChangedEvent);

            SpinWait.SpinUntil(() => _baseBlockChainManager.IsBlockChainManagerRunning());
            SpinWait.SpinUntil(() => _basePoolManager != null);

            //start loop process
            _basePoolManager.Start();
           
            //complete tx and send it to network
            SpinWait.SpinUntil(() => _baseBlockChainManager.SwarmServer.Running);
            SpinWait.SpinUntil(() => _basePoolManager.IsRunning);

            //generate new test action
            for (int i = 0; i < 12; i++)
            {
                var testActionItem1 = new CheckPointMarketDataV1();
                var genesisBlock = BlockChain<MarketAction>.MakeGenesisBlock();
                testActionItem1.Block = ByteUtil.Hex(genesisBlock.Serialize());
                testActionItem1.Hash = testActionItem1.GenerateHash();

                var testActionItem2 = new UserDataV1();
                testActionItem2.Description = "This is description.";
                testActionItem2.UserName = "LoginName";
                var bytesToSign = testActionItem2.ToByteArrayForSign();
                testActionItem2.Signature = Convert.ToBase64String(_userPrivateKey.Sign(bytesToSign));
                testActionItem2.Hash = testActionItem2.GenerateHash();

                Assert.IsNull(_basePoolManager.AcceptActionItem(testActionItem1));
                Assert.IsNull(_basePoolManager.AcceptActionItem(testActionItem2));
            }

            Assert.AreEqual(24, _basePoolManager.GetAllActionItemLocalCount());

            //now wait until we havent a new block
            SpinWait.SpinUntil((() => _newBlock == true));
            Assert.IsTrue(_newBlock);
            var acCount = _basePoolManager.GetAllActionItemLocalCount();
            _newBlock = false;

            SpinWait.SpinUntil(() => _basePoolManager.GetAllActionItemStagedCount() == 1);
            Assert.AreEqual(1, _basePoolManager.GetAllActionItemStagedCount());

            //now wait until we havent a new block
            SpinWait.SpinUntil((() => _newBlock == true));
            Assert.IsTrue(_newBlock);
            acCount = _basePoolManager.GetAllActionItemLocalCount();
            _newBlock = false;

            SpinWait.SpinUntil(() => _basePoolManager.GetAllActionItemStagedCount() == 1);
            Assert.AreEqual(1, _basePoolManager.GetAllActionItemStagedCount());

            //now wait until we havent a new block
            SpinWait.SpinUntil((() => _newBlock == true));
            Assert.IsTrue(_newBlock);
            acCount = _basePoolManager.GetAllActionItemLocalCount();
            _newBlock = false;

            SpinWait.SpinUntil(() => _basePoolManager.GetAllActionItemStagedCount() == 1);
            Assert.AreEqual(1, _basePoolManager.GetAllActionItemStagedCount());

            //now wait until we havent a new block
            SpinWait.SpinUntil((() => _newBlock == true));
            Assert.IsTrue(_newBlock);
            acCount = _basePoolManager.GetAllActionItemLocalCount();
            _newBlock = false;

            SpinWait.SpinUntil(() => _basePoolManager.GetAllActionItemStagedCount() == 1);
            Assert.AreEqual(1, _basePoolManager.GetAllActionItemStagedCount());

            //now wait until we havent a new block
            SpinWait.SpinUntil((() => _newBlock == true));
            Assert.IsTrue(_newBlock);
            acCount = _basePoolManager.GetAllActionItemLocalCount();
            _newBlock = false;

            SpinWait.SpinUntil(() => _basePoolManager.GetAllActionItemLocalCount() == 0);
            Assert.AreEqual(0, _basePoolManager.GetAllActionItemLocalCount());
        }
    }
}

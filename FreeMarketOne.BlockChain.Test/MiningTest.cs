using FreeMarketOne.BlockChain.Test.Helpers;
using FreeMarketOne.BlockChain.Test.Mocks;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.GenesisBlock;
using FreeMarketOne.P2P;
using FreeMarketOne.PoolManager;
using FreeMarketOne.Tor;
using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using Serilog.Core;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace FreeMarketOne.BlockChain.Test
{
    [TestClass]
    public class MiningTest
    {
        private IBaseConfiguration _configuration;
        private ILogger _logger;
        private IOnionSeedsManager _onionSeedsManager;
        private BasePoolManager _basePoolManager;
        private bool _newBlock = false;

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
                _basePoolManager.Start();

            }
            else
            {
                _logger.Error("Debug Base Chain isnt loaded!");
            }
        }

        private void BaseBlockChainChanged(object sender, EventArgs e)
        {
            //we have a new block
            _newBlock = true;
        }

        [TestMethod]
        public void RunMiningTest()
        {
            var debugEnvironment = new DebugEnvironmentHelper();
            _baseBlockChainLoadedEvent += new EventHandler(BaseBlockChainLoaded);
            _baseBlockChainChangedEvent += new EventHandler<BlockChain<BaseAction>.TipChangedEventArgs>(BaseBlockChainChanged);

            debugEnvironment.Initialize<MiningTest>(
                ref _configuration,
                ref _logger,
                ref _onionSeedsManager,
                ref _basePoolManager,
                ref _baseBlockChainManager,
                ref _baseBlockChainLoadedEvent,
                ref _baseBlockChainChangedEvent);

            SpinWait.SpinUntil(() => _baseBlockChainManager.IsBlockChainManagerRunning());
            SpinWait.SpinUntil(() => _basePoolManager != null && _basePoolManager.IsPoolManagerRunning());

            Assert.IsNotNull(_basePoolManager);
            Assert.IsTrue(_basePoolManager.IsPoolManagerRunning());

            //generate new test action
            var testActionItem1 = new CheckPointMarketDataV1();
            var genesisBlock = BlockChain<MarketAction>.MakeGenesisBlock();
            testActionItem1.Block = ByteUtil.Hex(genesisBlock.Serialize());  
            testActionItem1.Hash = testActionItem1.GenerateHash();

            var testActionItem2 = new ReviewUserDataV1();
            testActionItem2.ReviewDateTime = DateTime.UtcNow.AddMinutes(-1);
            testActionItem2.Message = "This is a test message";
            testActionItem2.Hash = testActionItem2.GenerateHash();

            _basePoolManager.AcceptActionItem(testActionItem1);
            _basePoolManager.AcceptActionItem(testActionItem2);

            //complete tx and send it to network
            SpinWait.SpinUntil(() => _baseBlockChainManager.SwarmServer.Running);
            _basePoolManager.PropagateAllActionItemLocal();

            //now waiting for mining
            SpinWait.SpinUntil((() => _basePoolManager.GetAllActionItemLocal().Count == 0));
            Assert.AreEqual(0, _basePoolManager.GetAllActionItemLocal().Count);

            //now wait until mining will start
            SpinWait.SpinUntil(() => _basePoolManager.IsMiningWorkerRunning());
            Assert.IsTrue(_basePoolManager.IsMiningWorkerRunning());

            //now wait until we havent a new block
            SpinWait.SpinUntil((() => _newBlock == true));
            Assert.IsTrue(_newBlock);

            //wait until end of mining
            SpinWait.SpinUntil(() => !_basePoolManager.IsMiningWorkerRunning());
            Assert.IsFalse(_basePoolManager.IsMiningWorkerRunning());

            //now we will check if blockchain contain new block with our action
            Assert.IsNotNull(_baseBlockChainManager.Storage.IterateBlockHashes().ToHashSet());
            Assert.AreEqual(2, _baseBlockChainManager.Storage.IterateBlockHashes().ToHashSet().Count());

            var chainId = _baseBlockChainManager.Storage.GetCanonicalChainId();
            var block0HashId = _baseBlockChainManager.Storage.IndexBlockHash(chainId.Value, 0);
            var block1HashId = _baseBlockChainManager.Storage.IndexBlockHash(chainId.Value, 1);

            var blockO = _baseBlockChainManager.Storage.GetBlock<BaseAction>(block0HashId.Value);
            var block1 = _baseBlockChainManager.Storage.GetBlock<BaseAction>(block1HashId.Value);

            Assert.IsNotNull(blockO.Transactions);
            Assert.AreEqual(1, blockO.Transactions.Count());
            Assert.IsNotNull(blockO.Transactions.First().Actions);
            Assert.AreEqual(1, blockO.Transactions.First().Actions.Count());
            Assert.IsNotNull(blockO.Transactions.First().Actions.First().BaseItems);
            Assert.AreEqual(1, blockO.Transactions.First().Actions.First().BaseItems.Count());

            Assert.IsNotNull(block1.Transactions);
            Assert.AreEqual(1, block1.Transactions.Count());
            Assert.IsNotNull(block1.Transactions.First().Actions);
            Assert.AreEqual(1, block1.Transactions.First().Actions.Count());
            Assert.IsNotNull(block1.Transactions.First().Actions.First().BaseItems);
            Assert.AreEqual(2, block1.Transactions.First().Actions.First().BaseItems.Count());
        }
    }
}

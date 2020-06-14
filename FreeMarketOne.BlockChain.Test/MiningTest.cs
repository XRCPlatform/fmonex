using FreeMarketOne.BlockChain.Test.Mocks;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.GenesisBlock;
using FreeMarketOne.P2P;
using FreeMarketOne.PoolManager;
using FreeMarketOne.Tor;
using Libplanet.Blockchain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using Serilog.Core;
using System;
using System.Data;
using System.IO;
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

        [TestMethod]
        private void InitializeDefaultEnvironment()
        {
            _configuration = new DebugConfiguration();
            _configuration.FullBaseDirectory = InitializeFullBaseDirectory();

            /* Clear all debug old data */
            ClearDefaultEnvironment(_configuration);

            /* Initialize Logger */
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(Path.Combine(_configuration.FullBaseDirectory, _configuration.LogFilePath),
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{Exception}{NewLine}",
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();
            _logger = Log.Logger.ForContext<MiningTest>();
            _logger.Information("Debug Start");

            /* Initialize Mock OnionSeeds */
            _onionSeedsManager = new MockSeedManager();

            /* Initialize genesis blocks */
            var generator = new GenesisGenerator();
            generator.GenerateIt(_configuration);

            /* Initialize Base BlockChain Manager */
            _baseBlockChainLoadedEvent += new EventHandler(BaseBlockChainLoaded);
            _baseBlockChainChangedEvent += new EventHandler<BlockChain<BaseAction>.TipChangedEventArgs>(BaseBlockChainChanged);

            _baseBlockChainManager = new BlockChainManager<BaseAction>(
                _configuration,
                _configuration.BlockChainBasePath,
                _configuration.BlockChainSecretPath,
                _configuration.BlockChainBasePolicy,
                _configuration.ListenerBaseEndPoint,
                _onionSeedsManager,
                preloadEnded: _baseBlockChainLoadedEvent,
                blockChainChanged: _baseBlockChainChangedEvent);
            _baseBlockChainManager.Start();
        }

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

        private string InitializeFullBaseDirectory()
        {
            var fullBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!fullBaseDirectory.StartsWith('/'))
                {
                    fullBaseDirectory.Insert(0, "/");
                }
            }

            return fullBaseDirectory;
        }

        private void ClearDefaultEnvironment(IBaseConfiguration configuration)
        {
            var folderPathBase = Path.Combine(configuration.FullBaseDirectory, configuration.BlockChainBasePath);
            var folderPathMarket = Path.Combine(configuration.FullBaseDirectory, configuration.BlockChainMarketPath);
            var folderLog = Path.Combine(configuration.FullBaseDirectory, configuration.LogFilePath);

            var keyFile = Path.Combine(configuration.FullBaseDirectory, configuration.BlockChainSecretPath);

            if (Directory.Exists(folderPathBase)) Directory.Delete(folderPathBase, true);
            if (Directory.Exists(folderPathMarket)) Directory.Delete(folderPathMarket, true);
            if (Directory.Exists(folderLog)) Directory.Delete(folderPathMarket, true);

            if (File.Exists(keyFile)) File.Delete(keyFile);
        }

        [TestMethod]
        public void RunMiningTest()
        {
            InitializeDefaultEnvironment();

            SpinWait.SpinUntil((() => _basePoolManager != null && _basePoolManager.IsPoolManagerRunning()), 4000);

            Assert.IsNotNull(_basePoolManager);
            Assert.IsTrue(_basePoolManager.IsPoolManagerRunning());

            //generate new test action
            var testActionItem1 = new CheckPointMarketDataV1();
            testActionItem1.BlockDateTime = new DateTime();
            testActionItem1.BlockHash = "asd8sdkoaf086xsc98n2oi92dh9c9ncfihrf2neicoacno";
            testActionItem1.Hash = testActionItem1.GenerateHash();

            var testActionItem2 = new ReviewUserDataV1();
            testActionItem2.ReviewDateTime = new DateTime().AddMinutes(-1);
            testActionItem2.Hash = testActionItem2.GenerateHash();

            _basePoolManager.AcceptActionItem(testActionItem1);
            _basePoolManager.AcceptActionItem(testActionItem2);

            //complete tx and send it to network
            _basePoolManager.PropagateAllActionItemLocal();

            //now waiting for mining
            SpinWait.SpinUntil((() => _basePoolManager.GetAllActionItemLocal().Count == 0), 4000);
            Assert.AreEqual(_basePoolManager.GetAllActionItemLocal(), 0);

            //now wait until mining will start
            SpinWait.SpinUntil((() => _basePoolManager.IsMiningWorkerRunning() == true), 4000);
            Assert.IsTrue(_basePoolManager.IsMiningWorkerRunning());

            //now wait until we havent a new block
            SpinWait.SpinUntil((() => _newBlock == true), 4000);
            Assert.IsTrue(_newBlock);

            //wait until end of mining
            SpinWait.SpinUntil((() => _basePoolManager.IsMiningWorkerRunning() == false), 4000);
            Assert.IsFalse(_basePoolManager.IsMiningWorkerRunning());
        }
    }
}

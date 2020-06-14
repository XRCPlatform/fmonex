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
        private IBaseConfiguration Configuration;
        private ILogger _logger;
        private IOnionSeedsManager OnionSeedsManager;
        private BasePoolManager BasePoolManager;

        private event EventHandler BaseBlockChainLoadedEvent;
        private event EventHandler<BlockChain<BaseAction>.TipChangedEventArgs> BaseBlockChainChangedEvent;
        private IBlockChainManager<BaseAction> BaseBlockChainManager;

        public TorProcessManager TorProcessManager { get; private set; }

        [TestMethod]
        private void InitializeDefaultEnvironment()
        {
            Configuration = new DebugConfiguration();
            Configuration.FullBaseDirectory = InitializeFullBaseDirectory();

            /* Clear all debug old data */
            ClearDefaultEnvironment(Configuration);

            /* Initialize Logger */
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(Path.Combine(Configuration.FullBaseDirectory, Configuration.LogFilePath),
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{Exception}{NewLine}",
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();
            _logger = Log.Logger.ForContext<MiningTest>();
            _logger.Information("Debug Start");

            /* Initialize Mock OnionSeeds */
            OnionSeedsManager = new MockSeedManager();

            /* Initialize genesis blocks */
            var generator = new GenesisGenerator();
            generator.GenerateIt(Configuration);

            /* Initialize Base BlockChain Manager */
            BaseBlockChainLoadedEvent += new EventHandler(BaseBlockChainLoaded);

            BaseBlockChainManager = new BlockChainManager<BaseAction>(
                Configuration,
                Configuration.BlockChainBasePath,
                Configuration.BlockChainSecretPath,
                Configuration.BlockChainBasePolicy,
                Configuration.ListenerBaseEndPoint,
                OnionSeedsManager,
                preloadEnded: BaseBlockChainLoadedEvent,
                blockChainChanged: BaseBlockChainChangedEvent);
            BaseBlockChainManager.Start();
        }

        private void BaseBlockChainLoaded(object sender, EventArgs e)
        {
            /* Initialize Market BlockChain Manager */
            if (BaseBlockChainManager.IsBlockChainManagerRunning())
            {
                /* Initialize Base And Market Pool */
                BasePoolManager = new BasePoolManager(
                    Configuration,
                    Configuration.MemoryBasePoolPath,
                    BaseBlockChainManager.Storage,
                    BaseBlockChainManager.SwarmServer,
                    BaseBlockChainManager.PrivateKey,
                    BaseBlockChainManager.BlockChain,
                    Configuration.BlockChainBasePolicy);
                BasePoolManager.Start();

            }
            else
            {
                _logger.Error("Debug Base Chain isnt loaded!");
            }
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

            SpinWait.SpinUntil((() => BasePoolManager != null && BasePoolManager.IsPoolManagerRunning()), 4000);

            Assert.IsNotNull(BasePoolManager);
            Assert.AreEqual(BasePoolManager.IsPoolManagerRunning(), true);

            //generate new test action
            var testActionItem1 = new CheckPointMarketDataV1();
            testActionItem1.BlockDateTime = new DateTime();
            testActionItem1.BlockHash = "asd8sdkoaf086xsc98n2oi92dh9c9ncfihrf2neicoacno";
            testActionItem1.Hash = testActionItem1.GenerateHash();

            var testActionItem2 = new ReviewUserDataV1();
            testActionItem2.ReviewDateTime = new DateTime().AddMinutes(-1);
            testActionItem2.Hash = testActionItem2.GenerateHash();

            BasePoolManager.AcceptActionItem(testActionItem1);
            BasePoolManager.AcceptActionItem(testActionItem2);

            //complete tx and send it to network
            BasePoolManager.PropagateAllActionItemLocal();

            //now waiting for mining
            SpinWait.SpinUntil((() => BasePoolManager.GetAllActionItemLocal().Count == 0), 4000);
            
            //check if all is propagated
            Assert.AreEqual(BasePoolManager.GetAllActionItemLocal(), 0);

            //now wait until mining will start
            SpinWait.SpinUntil((() => BasePoolManager.GetAllActionItemLocal().Count == 0), 4000);

            
        }
    }
}

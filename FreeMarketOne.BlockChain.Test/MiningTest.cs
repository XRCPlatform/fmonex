using FreeMarketOne.BlockChain.Test.Mocks;
using FreeMarketOne.DataStructure;
using FreeMarketOne.GenesisBlock;
using FreeMarketOne.P2P;
using FreeMarketOne.PoolManager;
using FreeMarketOne.Tor;
using Libplanet.Blockchain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using Serilog.Core;
using System;
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

        [TestMethod]
        public void RunMiningTest()
        {
            InitializeDefaultEnvironment();

            SpinWait.SpinUntil((() => BasePoolManager != null && BasePoolManager.IsPoolManagerRunning()), 4000);

            Assert.IsNotNull(BasePoolManager);
            Assert.AreEqual(BasePoolManager.IsPoolManagerRunning(), true);

        }
    }
}

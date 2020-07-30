using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Tor;
using Serilog;
using Serilog.Core;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using FreeMarketOne.P2P;
using FreeMarketOne.DataStructure;
using static FreeMarketOne.DataStructure.BaseConfiguration;
using FreeMarketOne.BlockChain;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.GenesisBlock;
using FreeMarketOne.PoolManager;
using Libplanet.Blockchain;
using System.Runtime.InteropServices;
using System.Threading;
using FreeMarketOne.ServerCore.Helpers;
using System.Threading.Tasks;

namespace FreeMarketOne.ServerCore
{
    public class FreeMarketOneServer
    {
        static FreeMarketOneServer()
        {
            Current = new FreeMarketOneServer();
        }

        public static FreeMarketOneServer Current { get; private set; }

        public Logger Logger;
        private ILogger _logger;

        public ServiceManager ServiceManager;

        public IBaseConfiguration Configuration;
        public TorProcessManager TorProcessManager;

        public IOnionSeedsManager OnionSeedsManager;

        public BasePoolManager BasePoolManager;
        public MarketPoolManager MarketPoolManager;

        public IBlockChainManager<BaseAction> BaseBlockChainManager;
        public IBlockChainManager<MarketAction> MarketBlockChainManager;

        public event EventHandler BaseBlockChainLoadEndedEvent;
        public event EventHandler MarketBlockChainLoadEndedEvent;

        public event EventHandler<BlockChain<BaseAction>.TipChangedEventArgs> BaseBlockChainChangedEvent;
        public event EventHandler<BlockChain<MarketAction>.TipChangedEventArgs> MarketBlockChainChangedEvent;

        public event EventHandler FreeMarketOneServerLoadedEvent;

        public void Initialize()
        {
            var fullBaseDirectory = InitConfigurationHelper.InitializeFullBaseDirectory();

            //Configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(fullBaseDirectory)
                .AddJsonFile("appsettings.json", true, false);
            var configFile = builder.Build();

            //Environment
            Configuration = InitConfigurationHelper.InitializeEnvironment(configFile);

            //Config
            Configuration.FullBaseDirectory = fullBaseDirectory;
            InitConfigurationHelper.InitializeBaseOnionSeedsEndPoint(Configuration, configFile);
            InitConfigurationHelper.InitializeBaseTorEndPoint(Configuration, configFile);
            InitConfigurationHelper.InitializeLogFilePath(Configuration, configFile);
            InitConfigurationHelper.InitializeMemoryPoolPaths(Configuration, configFile);
            InitConfigurationHelper.InitializeBlockChainPaths(Configuration, configFile);
            InitConfigurationHelper.InitializeListenerEndPoints(Configuration, configFile);

            //Initialize Logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(Path.Combine(Configuration.FullBaseDirectory, Configuration.LogFilePath),
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{Exception}{NewLine}",
                    rollingInterval: RollingInterval.Day, shared: true)
                .CreateLogger();
            _logger = Log.Logger.ForContext<FreeMarketOneServer>();
            _logger.Information("Application Start");
            _logger.Information(Configuration.ListenerBaseEndPoint.ToString());
            _logger.Information(Configuration.ListenerMarketEndPoint.ToString());

            //Service manager
            ServiceManager = new ServiceManager(Configuration);
            ServiceManager.Start();

            //Initialize Tor
            TorProcessManager = new TorProcessManager(Configuration);
            var torInitialized = TorProcessManager.Start();

            SpinWait.SpinUntil(() => torInitialized, 10000);
            if (torInitialized)
            {
                //Initialize OnionSeeds
                OnionSeedsManager = new OnionSeedsManager(Configuration, TorProcessManager);
                OnionSeedsManager.Start();

                //Initialize Base BlockChain Manager
                BaseBlockChainLoadEndedEvent += new EventHandler(Current.BaseBlockChainLoaded);

                BaseBlockChainManager = new BlockChainManager<BaseAction>(
                    Configuration,
                    Configuration.BlockChainBasePath,
                    Configuration.BlockChainSecretPath,
                    Configuration.BlockChainBaseGenesis,
                    Configuration.BlockChainBasePolicy,
                    Configuration.ListenerBaseEndPoint,
                    OnionSeedsManager,
                    preloadEnded: BaseBlockChainLoadEndedEvent,
                    blockChainChanged: BaseBlockChainChangedEvent);
                BaseBlockChainManager.Start();
            } 
            else
            {
                _logger.Error("Unexpected error. Could not automatically start Tor. Try running Tor manually.");
            }
        }

        private void BaseBlockChainLoaded(object sender, EventArgs e)
        {
            //Initialize Base Pool Manager
            if (BaseBlockChainManager.IsBlockChainManagerRunning())
            {
                //Initialize Base Pool
                BasePoolManager = new BasePoolManager(
                    Configuration,
                    Configuration.MemoryBasePoolPath,
                    BaseBlockChainManager.Storage,
                    BaseBlockChainManager.SwarmServer,
                    BaseBlockChainManager.PrivateKey,
                    BaseBlockChainManager.BlockChain,
                    Configuration.BlockChainBasePolicy);
                BasePoolManager.Start();

                //Initialize Market Blockchain Manager
                MarketBlockChainLoadEndedEvent += new EventHandler(Current.MarketBlockChainLoaded);

                var hashCheckPoints = BaseBlockChainManager.GetActionItemsByType(typeof(CheckPointMarketDataV1));
                var genesisBlock = BlockHelper.GetGenesisMarketBlockByHash(hashCheckPoints, Configuration.BlockChainMarketPolicy);

                MarketBlockChainManager = new BlockChainManager<MarketAction>(
                    Configuration,
                    Configuration.BlockChainMarketPath,
                    Configuration.BlockChainSecretPath,
                    Configuration.BlockChainMarketGenesis,
                    Configuration.BlockChainMarketPolicy,
                    Configuration.ListenerMarketEndPoint,
                    OnionSeedsManager,
                    hashCheckPoints,
                    genesisBlock,
                    preloadEnded: MarketBlockChainLoadEndedEvent,
                    blockChainChanged: MarketBlockChainChangedEvent);
                MarketBlockChainManager.Start();
            } 
            else
            {
                _logger.Error("Base Chain isnt loaded!");
                Stop();
            }
        }

        private void MarketBlockChainLoaded(object sender, EventArgs e)
        {
            //Initialize Market Pool Manager
            if (MarketBlockChainManager.IsBlockChainManagerRunning())
            {
                MarketPoolManager = new MarketPoolManager(
                    Configuration,
                    Configuration.MemoryBasePoolPath,
                    MarketBlockChainManager.Storage,
                    MarketBlockChainManager.SwarmServer,
                    MarketBlockChainManager.PrivateKey,
                    MarketBlockChainManager.BlockChain,
                    Configuration.BlockChainMarketPolicy);
                MarketPoolManager.Start();

                //Event that server is loaded
                RaiseAsyncServerLoadedEvent();
            }
            else
            {
                _logger.Error("Market Chain isnt loaded!");
                Stop();
            }
        }

        async void RaiseAsyncServerLoadedEvent()
        {
            await Task.Delay(1000);
            await Task.Run(() =>
            {
                FreeMarketOneServerLoadedEvent?.Invoke(this, null);
            }).ConfigureAwait(true);
        }

        public void Stop()
        {
            _logger.Information("Ending Service Manager...");
            ServiceManager?.Dispose();

            _logger.Information("Ending BlockChain Managers...");
            BaseBlockChainManager?.Dispose();
            MarketBlockChainManager?.Dispose();

            _logger.Information("Ending Onion Seeds ...");
            OnionSeedsManager?.Dispose();

            _logger.Information("Ending Base Pool Manager...");
            BasePoolManager?.Dispose();

            _logger.Information("Ending Market Pool Manager...");
            MarketPoolManager?.Dispose();

            _logger.Information("Ending Tor...");
            TorProcessManager?.Dispose();

            _logger.Information("Application End");
        }
    }
}
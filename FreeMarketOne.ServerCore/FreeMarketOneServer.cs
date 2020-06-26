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
using Avalonia.Controls.ApplicationLifetimes;
using System.Threading.Tasks;
using Avalonia.Controls;

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

        public void Initialize(EventHandler splashMessageEvent)
        {
            splashMessageEvent?.Invoke("Kernel Inicialization...", EventArgs.Empty);

            var fullBaseDirectory = InitializeFullBaseDirectory();

            //Configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(fullBaseDirectory)
                .AddJsonFile("appsettings.json", true, false);
            var configFile = builder.Build();

            //Environment
            Configuration = InitializeEnvironment(configFile);

            //Config
            Configuration.FullBaseDirectory = fullBaseDirectory;
            InitializeBaseOnionSeedsEndPoint(Configuration, configFile);
            InitializeBaseTorEndPoint(Configuration, configFile);
            InitializeLogFilePath(Configuration, configFile);
            InitializeMemoryPoolPaths(Configuration, configFile);
            InitializeBlockChainPaths(Configuration, configFile);
            InitializeListenerEndPoints(Configuration, configFile);

            //Initialize Logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(Path.Combine(Configuration.FullBaseDirectory, Configuration.LogFilePath),
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{Exception}{NewLine}",
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();
            _logger = Log.Logger.ForContext<FreeMarketOneServer>();
            _logger.Information("Application Start");

            //Initialize Tor
            splashMessageEvent?.Invoke("Tor Inicialization...", EventArgs.Empty);
            TorProcessManager = new TorProcessManager(Configuration);
            var torInitialized = TorProcessManager.Start();

            SpinWait.SpinUntil(() => torInitialized, 10000);
            if (torInitialized)
            {
                //Initialize OnionSeeds
                splashMessageEvent?.Invoke("OnionSeed Inicialization...", EventArgs.Empty);
                OnionSeedsManager = new OnionSeedsManager(Configuration, TorProcessManager);
                OnionSeedsManager.Start();

                //Initialize Base BlockChain Manager
                splashMessageEvent?.Invoke("Base BlockChain Inicialization...", EventArgs.Empty);
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
                FreeMarketOneServerLoadedEvent?.Invoke(this, null);
            }
            else
            {
                _logger.Error("Market Chain isnt loaded!");
                Stop();
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

        private void InitializeLogFilePath(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var logFilePath = configFile.GetSection("FreeMarketOneConfiguration")["LogFilePath"];

            if (!string.IsNullOrEmpty(logFilePath)) configuration.LogFilePath = logFilePath;
        }

        private static IBaseConfiguration InitializeEnvironment(IConfigurationRoot configFile)
        {
            var settings = configFile.GetSection("FreeMarketOneConfiguration")["ServerEnvironment"];

            var environment = EnvironmentTypes.Test;
            Enum.TryParse(settings, out environment);

            if (environment == EnvironmentTypes.Main)
            {
                return new MainConfiguration();
            } 
            else
            {
                return new TestConfiguration();
            }
        }

        private static void InitializeBaseOnionSeedsEndPoint(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var seedsEndPoint = configFile.GetSection("FreeMarketOneConfiguration")["OnionSeedsEndPoint"];

            if (!string.IsNullOrEmpty(seedsEndPoint)) configuration.OnionSeedsEndPoint = seedsEndPoint;
        }

        private static void InitializeBaseTorEndPoint(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var torEndPoint = configFile.GetSection("FreeMarketOneConfiguration")["TorEndPoint"];

            if (!string.IsNullOrEmpty(torEndPoint)) configuration.TorEndPoint = EndPointHelper.ParseIPEndPoint(torEndPoint);
        }

        private static void InitializeMemoryPoolPaths(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var memoryBasePoolPath = configFile.GetSection("FreeMarketOneConfiguration")["MemoryBasePoolPath"];
            var memoryMarketPoolPath = configFile.GetSection("FreeMarketOneConfiguration")["MemoryMarketPoolPath"];

            if (!string.IsNullOrEmpty(memoryBasePoolPath)) configuration.MemoryBasePoolPath = memoryBasePoolPath;
            if (!string.IsNullOrEmpty(memoryMarketPoolPath)) configuration.MemoryMarketPoolPath = memoryMarketPoolPath;
        }

        private static void InitializeBlockChainPaths(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var blockChainBasePath = configFile.GetSection("FreeMarketOneConfiguration")["BlockChainBasePath"];
            var blockChainMarketPath = configFile.GetSection("FreeMarketOneConfiguration")["BlockChainMarketPath"];
            var blockChainSecretPath = configFile.GetSection("FreeMarketOneConfiguration")["BlockChainSecretPath"];

            if (!string.IsNullOrEmpty(blockChainBasePath)) configuration.BlockChainBasePath = blockChainBasePath;
            if (!string.IsNullOrEmpty(blockChainMarketPath)) configuration.BlockChainMarketPath = blockChainMarketPath;
            if (!string.IsNullOrEmpty(blockChainSecretPath)) configuration.BlockChainSecretPath = blockChainSecretPath;
        }

        private static void InitializeListenerEndPoints(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var baseEndPoint = configFile.GetSection("FreeMarketOneConfiguration")["ListenerBaseEndPoint"];
            var marketEndPoint = configFile.GetSection("FreeMarketOneConfiguration")["ListenerMarketEndPoint"];

            if (!string.IsNullOrEmpty(baseEndPoint)) EndPointHelper.ParseIPEndPoint(baseEndPoint);
            if (!string.IsNullOrEmpty(marketEndPoint)) EndPointHelper.ParseIPEndPoint(marketEndPoint);
        }

        public void Stop()
        {
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
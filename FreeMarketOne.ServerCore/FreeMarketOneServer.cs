using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Tor;
using Serilog;
using Serilog.Core;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using FreeMarketOne.P2P;
using FreeMarketOne.Mining;
using FreeMarketOne.BasePool;
using FreeMarketOne.MarketPool;
using FreeMarketOne.DataStructure;
using static FreeMarketOne.DataStructure.BaseConfiguration;
using FreeMarketOne.BlockChain;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.GenesisBlock;

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
        private ILogger logger;

        public IBaseConfiguration Configuration;
        public TorProcessManager TorProcessManager;

        public IOnionSeedsManager OnionSeedsManager;
        public IMiningProcessor MiningProcessor;

        public IBasePoolManager BasePoolManager;
        public IMarketPoolManager MarketPoolManager;

        public IBlockChainManager BaseBlockChainManager;
        public IBlockChainManager MarketBlockChainManager;

        public event EventHandler BaseBlockChainLoadedEvent;

        public void Initialize()
        {
            /* Configuration */
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, false);
            var configFile = builder.Build();

            /* Environment */
            Configuration = InitializeEnvironment(configFile);

            /* Config */
            InitializeBaseOnionSeedsEndPoint(Configuration, configFile);
            InitializeBaseTorEndPoint(Configuration, configFile);
            InitializeLogFilePath(Configuration, configFile);
            InitializeMemoryPoolPaths(Configuration, configFile);
            InitializeBlockChainPaths(Configuration, configFile);
            InitializeListenerEndPoints(Configuration, configFile);

            /* Initialize Logger */
            Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Configuration.LogFilePath,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{Exception}{NewLine}",
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();
            logger = Logger.ForContext<FreeMarketOneServer>();
            logger.Information("Application Start");

            /* Initialize Tor */
            TorProcessManager = new TorProcessManager(Logger, Configuration);
            var torInitialized = TorProcessManager.Start();

            if (torInitialized)
            {
            /* Initialize OnionSeeds */
                OnionSeedsManager = new OnionSeedsManager(Logger, Configuration, TorProcessManager);
                OnionSeedsManager.Start();
            }

            /* Initialize genesis blocks */
            var generator = new GenesisGenerator();
            generator.GenerateIt(Configuration.BlockChainBasePath, Configuration.BlockChainMarketPath);

            /* Initialize Base And Market Pool */
            BasePoolManager = new BasePoolManager(Logger, Configuration);
            MarketPoolManager = new MarketPoolManager(Logger, Configuration);

            /* Initialize Base BlockChain Manager */
            BaseBlockChainLoadedEvent += new EventHandler(Current.BaseBlockChainLoaded);

            BaseBlockChainManager = new BlockChainManager<BaseBlockChainAction>(
                Logger,
                Configuration.BlockChainBasePath,
                Configuration.BlockChainSecretPath,
                Configuration.ListenerBaseEndPoint,
                OnionSeedsManager, 
                preloadEnded: BaseBlockChainLoadedEvent);
            BaseBlockChainManager.Start();
        }

        private void BaseBlockChainLoaded(object sender, EventArgs e)
        {
            /* Initialize Market BlockChain Manager */
            if (BaseBlockChainManager.IsBlockChainManagerRunning())
            {
                var hashCheckPoints = BaseBlockChainManager.GetActionItemsByType(typeof(CheckPointMarketDataV1));
                MarketBlockChainManager = new BlockChainManager<MarketBlockChainAction>(
                    Logger,
                    Configuration.BlockChainMarketPath,
                    Configuration.BlockChainSecretPath,
                    Configuration.ListenerMarketEndPoint,
                    OnionSeedsManager,
                    hashCheckPoints);
                MarketBlockChainManager.Start();
            }
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
            logger.Information("Ending BlockChain Managers...");
            BaseBlockChainManager?.Dispose();
            MarketBlockChainManager?.Dispose();

            logger.Information("Ending Onion Seeds ...");
            OnionSeedsManager?.Dispose();

            logger.Information("Ending Base Pool Manager...");
            BasePoolManager?.Dispose();

            logger.Information("Ending Market Pool Manager...");
            MarketPoolManager?.Dispose();

            logger.Information("Ending Tor...");
            TorProcessManager?.Dispose();

            logger.Information("Application End");
        }
    }
}
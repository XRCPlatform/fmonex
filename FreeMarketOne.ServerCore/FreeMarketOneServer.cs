using FreeMarketOne.BlockChain;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.P2P;
using FreeMarketOne.PoolManager;
using FreeMarketOne.Search;
using FreeMarketOne.ServerCore.Helpers;
using FreeMarketOne.Tor;
using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Extensions;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace FreeMarketOne.ServerCore
{
    public class FreeMarketOneServer
    {
        static FreeMarketOneServer()
        {
            Current = new FreeMarketOneServer();
        }

        public enum FreeMarketOneServerStates
        {
            NotReady = 0,
            Offline = 1,
            Online = 2
        }

        public static FreeMarketOneServer Current { get; private set; }

        public Logger Logger;
        private ILogger _logger;

        public ServiceManager ServiceManager;
        public UserManager UserManager;
        public MarketManager MarketManager;
        public SearchIndexer SearchIndexer;
        public SearchEngine SearchEngine;

        public IBaseConfiguration Configuration;
        public TorProcessManager TorProcessManager;

        public IOnionSeedsManager OnionSeedsManager;

        public BasePoolManager BasePoolManager;
        public MarketPoolManager MarketPoolManager;
        public ChatManager ChatManager;
        public IpHelper ServerPublicAddress;

        public IBlockChainManager<BaseAction> BaseBlockChainManager;
        public IBlockChainManager<MarketAction> MarketBlockChainManager;

        public event EventHandler BaseBlockChainLoadEndedEvent;
        public event EventHandler MarketBlockChainLoadEndedEvent;

        public event EventHandler<BlockChain<BaseAction>.TipChangedEventArgs> BaseBlockChainChangedEvent;
        public event EventHandler<BlockChain<MarketAction>.TipChangedEventArgs> MarketBlockChainChangedEvent;
        public event EventHandler<List<HashDigest<SHA256>>> MarketBlockClearedOldersEvent;

        public event EventHandler FreeMarketOneServerLoadedEvent;

        public void Initialize(string password = null, UserDataV1 firstUserData = null)
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
            InitConfigurationHelper.InitializeTorUsage(Configuration, configFile);
            InitConfigurationHelper.InitializeChatPaths(Configuration, configFile);
            InitConfigurationHelper.InitializeSearchEnginePaths(Configuration, configFile);

            //IP Helper
            ServerPublicAddress = new IpHelper(Configuration);
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

            
            //User manager
            UserManager = new UserManager(Configuration);
            if (UserManager.Initialize(password, firstUserData) == UserManager.PrivateKeyStates.Valid)
            {
                //Service manager
                ServiceManager = new ServiceManager(Configuration);
                ServiceManager.Start();

                //Market Manager
                MarketManager = new MarketManager(Configuration);

                //Initialize Tor
                TorProcessManager = new TorProcessManager(Configuration);
                var torInitialized = TorProcessManager.Start();

                SpinWait.SpinUntil(() => torInitialized, 10000);
                if (torInitialized)
                {
                    //Search indexer
                    SearchIndexer = new SearchIndexer(MarketManager, SearchHelper.GetDataFolder(Configuration));
                    SearchIndexer.Initialize();

                    SearchEngine = new SearchEngine(MarketManager, SearchHelper.GetDataFolder(Configuration));

                    //Loading 
                    ServerPublicAddress.GetMyTorExitIP();

                    //Chat Manager
                    ChatManager = new ChatManager(Configuration);
                    ChatManager.Start();

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
                        UserManager.PrivateKey,
                        preloadEnded: BaseBlockChainLoadEndedEvent,
                        blockChainChanged: BaseBlockChainChangedEvent);
                    BaseBlockChainManager.Start();
                }
                else
                {
                    _logger.Error("Unexpected error. Could not automatically start Tor. Try running Tor manually.");
                }
            }
            else
            {
                _logger.Warning("No user account is necessary to create one.");
            }
        }


        private void BaseBlockChainLoaded(object sender, EventArgs e)
        {
            //Initialize Base Pool Manager
            if (BaseBlockChainManager.IsBlockChainManagerRunning())
            {
                //Add Swarm server to seed manager
                OnionSeedsManager.BaseSwarm = BaseBlockChainManager.SwarmServer;

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
                MarketBlockChainChangedEvent += new EventHandler<BlockChain<MarketAction>.TipChangedEventArgs>(Current.MarketBlockChainChanged);

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
                    UserManager.PrivateKey,
                    hashCheckPoints,
                    genesisBlock,
                    preloadEnded: MarketBlockChainLoadEndedEvent,
                    blockChainChanged: MarketBlockChainChangedEvent,
                    clearedOlderBlocks: MarketBlockClearedOldersEvent);
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
                //Add Swarm server to seed manager
                OnionSeedsManager.MarketSwarm = MarketBlockChainManager.SwarmServer;

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
                if (UserManager != null)
                {
                    if ((UserManager.UsedDataForceToPropagate) && (UserManager.UserData != null))
                    {
                        BasePoolManager.AcceptActionItem(UserManager.UserData);
                        BasePoolManager.PropagateAllActionItemLocal();
                    }
                    else
                    {
                        //loading actual user data from pool or blockchain
                        var userData = UserManager.GetActualUserData();
                        UserManager.SaveUserData(userData, Configuration.FullBaseDirectory, Configuration.BlockChainUserPath);
                    }
                }

                FreeMarketOneServerLoadedEvent?.Invoke(this, null);
            }).ConfigureAwait(true);
        }

        /// <summary>
        /// Processing event Market BlockChain Changed for Chat
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MarketBlockChainChanged(object sender, BlockChain<MarketAction>.TipChangedEventArgs e)
        {
            _logger.Information(string.Format("New block processing for chat - item hash {0}.", e.Hash));

            var marketBlockChain = Current.MarketBlockChainManager.Storage;
            var block = marketBlockChain.GetBlock<MarketAction>(e.Hash);

            Current.ChatManager.ProcessNewBlock(block);
        }

        public FreeMarketOneServerStates GetServerState()
        {
            if (ServiceManager == null)
            {
                return FreeMarketOneServerStates.NotReady;
            } 
            else
            {
                return ServiceManager.GetServerState();
            }
        }

        public void Stop()
        {
            _logger?.Information("Ending Service Manager...");
            ServiceManager?.Dispose();

            _logger?.Information("Ending Base Pool Manager...");
            BasePoolManager?.Dispose();

            _logger?.Information("Ending Market Pool Manager...");
            MarketPoolManager?.Dispose();

            _logger?.Information("Ending BlockChain Managers...");
            BaseBlockChainManager?.Dispose();
            MarketBlockChainManager?.Dispose();

            _logger?.Information("Ending Search Indexer...");
            SearchIndexer?.Dispose();

            _logger?.Information("Ending Onion Seeds ...");
            OnionSeedsManager?.Dispose();

            _logger?.Information("Ending Tor...");
            TorProcessManager?.Dispose();

            _logger?.Information("Ending Chat Manager...");
            ChatManager?.Dispose();

            _logger?.Information("Ending User Manager...");
            UserManager = null;

            _logger?.Information("Ending Market Manager...");
            MarketManager = null;

            _logger?.Information("Application End");
        }
    }
}
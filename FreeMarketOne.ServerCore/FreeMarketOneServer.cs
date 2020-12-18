using FreeMarketOne.BlockChain;
using FreeMarketOne.Chats;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.DataStructure.ProtocolVersions;
using FreeMarketOne.Markets;
using FreeMarketOne.P2P;
using FreeMarketOne.Pools;
using FreeMarketOne.Search;
using FreeMarketOne.ServerCore.Helpers;
using FreeMarketOne.Tor;
using FreeMarketOne.Users;
using Libplanet;
using Libplanet.Blocks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
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
        

        public Logger Logger => (Logger)_logger;
        private ILogger _logger;

        public ServiceManager Services;
        public IUserManager Users;
        public IMarketManager Markets;
        public IChatManager Chats;
        public IBaseConfiguration Configuration;

        public SearchIndexer SearchIndexer;
        public SearchEngine SearchEngine;

        public TorProcessManager TorProcessManager;
        public IOnionSeedsManager OnionSeedsManager;
        public BasePoolManager BasePoolManager;
        public MarketPoolManager MarketPoolManager;
        public IpHelper ServerPublicAddress;
        public IBlockChainManager<BaseAction> BaseBlockChainManager;
        public IBlockChainManager<MarketAction> MarketBlockChainManager;

        public event EventHandler BaseBlockChainLoadEndedEvent;
        public event EventHandler MarketBlockChainLoadEndedEvent;

        public event EventHandler<(Block<BaseAction> OldTip, Block<BaseAction> NewTip)> BaseBlockChainChangedEvent;
        public event EventHandler<(Block<MarketAction> OldTip, Block<MarketAction> NewTip)> MarketBlockChainChangedEvent;

        public event EventHandler<List<HashDigest<SHA256>>> MarketBlockClearedOldersEvent;
        public event EventHandler<NetworkHeartbeatArgs> NetworkHeartbeatEvent;
        public event EventHandler FreeMarketOneServerLoadedEvent;
        public event EventHandler<string> LoadingEvent;
        private BackgroundQueue backgroundQueue = new BackgroundQueue();

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
            InitConfigurationHelper.InitializeMinimalPeerAmount(Configuration, configFile);

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
            Users = new UserManager(Configuration);
            if (Users.Initialize(password, firstUserData) == UserManager.PrivateKeyStates.Valid)
            {
                //Service manager
                LoadingEvent?.Invoke(this, "Loading Service Manager...");
                Services = new ServiceManager(Configuration, NetworkHeartbeatEvent);
                Services.Start();

                //Market Manager
                LoadingEvent?.Invoke(this, "Loading Market Manager...");
                Markets = new MarketManager(Configuration);

                //Initialize Tor
                LoadingEvent?.Invoke(this, "Loading Tor Manager...");
                TorProcessManager = new TorProcessManager(Configuration);
                var torInitialized = TorProcessManager.Start();

                SpinWait.SpinUntil(() => torInitialized, 10000);
                if (torInitialized)
                {
                    //Loading 
                    LoadingEvent?.Invoke(this, "Loading Tor Circles Info...");
                    ServerPublicAddress.GetMyTorExitIP();

                    //Chat Manager
                    LoadingEvent?.Invoke(this, "Loading Chat Manager...");
                    Chats = new ChatManager(Configuration, Users.PrivateKey, Users, ServerPublicAddress.PublicIP);
                    Chats.Start();

                    //Initialize OnionSeeds
                    LoadingEvent?.Invoke(this, "Loading Onion Seed Manager...");
                    OnionSeedsManager = new OnionSeedsManager(Configuration, TorProcessManager, ServerPublicAddress.PublicIP);
                    OnionSeedsManager.Start();

                    //Initialize Base BlockChain Manager
                    LoadingEvent?.Invoke(this, "Loading Base BlockChain Manager...");
                    BaseBlockChainLoadEndedEvent += new EventHandler(Current.BaseBlockChainLoaded);
                    BaseBlockChainChangedEvent += new EventHandler<(Block<BaseAction> OldTip, Block<BaseAction> NewTip)>(Current.BaseBlockChainChanged);

                    BaseBlockChainManager = new BlockChainManager<BaseAction>(
                        Configuration,
                        Configuration.BlockChainBasePath,
                        Configuration.BlockChainSecretPath,
                        Configuration.BlockChainBaseGenesis,
                        Configuration.BlockChainBasePolicy,
                        Configuration.ListenerBaseEndPoint,
                        OnionSeedsManager,
                        Users.PrivateKey,
                        new BaseChainProtocolVersion(),
                        preloadEnded: BaseBlockChainLoadEndedEvent,
                        blockChainChanged: BaseBlockChainChangedEvent);

                    LoadingEvent?.Invoke(this, "Starting BaseChain Initial Block Download...");
                    BaseBlockChainManager.Start();

                    //Initialize Base Pool
                    LoadingEvent?.Invoke(this, "Loading Base Pool Manager...");
                    BasePoolManager = new BasePoolManager(
                        Configuration,
                        Configuration.MemoryBasePoolPath,
                        BaseBlockChainManager.Storage,
                        BaseBlockChainManager.SwarmServer,
                        BaseBlockChainManager.PrivateKey,
                        BaseBlockChainManager.BlockChain,
                        Configuration.BlockChainBasePolicy);

                    LoadingEvent?.Invoke(this, "Starting Base PoolManager...");
                    BasePoolManager.Start();

                    //Search indexer
                    LoadingEvent?.Invoke(this, "Loading Local Search Engine...");
                    XRCDaemonClient client = new XRCDaemonClient(new JsonSerializerSettings(), Configuration, _logger);
                    SearchIndexer = new SearchIndexer(Markets, Configuration, new XRCHelper(client), Users, BasePoolManager, BaseBlockChainManager);
                    SearchIndexer.Initialize();
                    SearchEngine = new SearchEngine(Markets, SearchHelper.GetDataFolder(Configuration));
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

                //Initialize Market Blockchain Manager
                LoadingEvent?.Invoke(this, "Loading Market BlockChain Manager...");
                MarketBlockChainLoadEndedEvent += new EventHandler(Current.MarketBlockChainLoaded);
                MarketBlockChainChangedEvent += new EventHandler<(Block<MarketAction> OldTip, Block<MarketAction> NewTip)>(Current.MarketBlockChainChanged);

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
                    Users.PrivateKey,
                    new MarketChainProtocolVersion(),
                    hashCheckPoints,
                    genesisBlock,
                    preloadEnded: MarketBlockChainLoadEndedEvent,
                    blockChainChanged: MarketBlockChainChangedEvent,
                    clearedOlderBlocks: MarketBlockClearedOldersEvent);

                LoadingEvent?.Invoke(this, "Starting MarketChain Initial Block Download...");
                MarketBlockChainManager.Start();
            }
            else
            {
                _logger.Error("Base Chain isnt loaded!");
                Stop();
            }
        }

        /// <summary>
        /// Processing event Base BlockChain Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BaseBlockChainChanged(object sender, (Block<BaseAction> OldTip, Block<BaseAction> NewTip) e)
        {
            if (e.NewTip != null)
            {
                _logger.Information($"Recieved base block downloaded notification {e.NewTip.Hash}");

                _logger.Information($"New block SearchIndexing");
                SearchIndexer.IndexBlock(e.NewTip);
            }
            else if ((e.NewTip == null) && (e.OldTip != null))
            {
                _logger.Information($"Recieved base orphaned block notification {e.OldTip.Hash}");

                _logger.Information($"Clearing block SearchIndexing");
                SearchIndexer.UnIndexBlock(e.OldTip);
            }
        }

        private void MarketBlockChainLoaded(object sender, EventArgs e)
        {
            //Initialize Market Pool Manager
            if (MarketBlockChainManager.IsBlockChainManagerRunning())
            {
                LoadingEvent?.Invoke(this, "Loading Market Pool Manager...");
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

                LoadingEvent?.Invoke(this, "Starting Market PoolManager...");
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
                if (Users != null)
                {
                    if (Users.UsedDataForceToPropagate && (Users.UserData != null))
                    {
                        if (BasePoolManager.AcceptActionItem(Users.UserData) == null)
                        {
                            BasePoolManager.PropagateAllActionItemLocal(true);
                        }
                    }
                    else
                    {
                        //loading actual user data from pool or blockchain
                        var userData = Users.GetActualUserData(Current.BasePoolManager, Current.BaseBlockChainManager);
                        if ((userData != null) && (userData != Users.UserData))
                        {
                            Users.SaveUserData(userData, Configuration.FullBaseDirectory, Configuration.BlockChainUserPath);
                        }
                    }
                }

                FreeMarketOneServerLoadedEvent?.Invoke(this, null);
            }).ConfigureAwait(true);
        }

        /// <summary>
        /// Processing event Market BlockChain Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MarketBlockChainChanged(object sender, (Block<MarketAction> OldTip, Block<MarketAction> NewTip) e)
        {
            if (e.NewTip != null)
            {
                _logger.Information($"Recieved market block downloaded notification {e.NewTip.Hash}");

                _logger.Information($"New block SearchIndexing");
                backgroundQueue.QueueTask(() => SearchIndexer.IndexBlock(e.NewTip));

                _logger.Information("New block processing for chat - item hash");
                Current.Chats.ProcessNewBlock(e.NewTip);
            }
            else if ((e.NewTip == null) && (e.OldTip != null))
            {
                _logger.Information($"Recieved market orphaned block notification {e.OldTip.Hash}");

                _logger.Information($"Clearing block SearchIndexing");
                SearchIndexer.UnIndexBlock(e.OldTip);

                //TODO: CHAT?????
            }
        }

        /// <summary>
        /// Get information about state of server
        /// </summary>
        /// <returns></returns>
        public FreeMarketOneServerStates GetServerState()
        {
            if (Services == null)
            {
                return FreeMarketOneServerStates.NotReady;
            } 
            else
            {
                return Services.GetServerState();
            }
        }

        public void Stop()
        {
            _logger?.Information("Ending Service Manager...");
            Services?.Dispose();

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
            Chats?.Dispose();

            _logger?.Information("Ending User Manager...");
            Users = null;

            _logger?.Information("Ending Market Manager...");
            Markets = null;

            _logger?.Information("Application End");
        }
    }
}
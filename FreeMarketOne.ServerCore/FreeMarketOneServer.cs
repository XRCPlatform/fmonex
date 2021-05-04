using FreeMarketOne.BlockChain;
using FreeMarketOne.Chats;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.DataStructure.ProtocolVersions;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.GenesisBlock;
using FreeMarketOne.Markets;
using FreeMarketOne.P2P;
using FreeMarketOne.Pools;
using FreeMarketOne.Search;
using FreeMarketOne.ServerCore.Helpers;
using FreeMarketOne.Tor;
using FreeMarketOne.Users;
using Libplanet;
using Libplanet.Blocks;
using Libplanet.Extensions;
using Libplanet.Net;
using Libplanet.Net.Messages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using static FreeMarketOne.Users.UserManager;

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
            Online = 2,
        }

        public static FreeMarketOneServer Current { get; private set; }
        
        public Logger Logger => (Logger)_logger;
        private ILogger _logger;

        public ServiceManager ServiceManager;
        public IUserManager UserManager;
        public IMarketManager MarketManager;
        public IChatManager ChatManager;
        public IBaseConfiguration Configuration;
        private IDefaultBlockPolicy<BaseAction> blockChainBasePolicy;
        private IDefaultBlockPolicy<MarketAction> blockChainMarketPolicy;
        public SearchIndexer SearchIndexer;
        public SearchEngine SearchEngine;

        public TorProcessManager TorProcessManager;
        public IOnionSeedsManager OnionSeedsManager;
        public BasePoolManager BasePoolManager;
        public MarketPoolManager MarketPoolManager;
        public IpHelper ServerPublicAddress;
        public IBlockChainManager<BaseAction> BaseBlockChainManager;
        public IBlockChainManager<MarketAction> MarketBlockChainManager;

        public DataDir DataDir;

        public event EventHandler BaseBlockChainLoadEndedEvent;
        public event EventHandler MarketBlockChainLoadEndedEvent;

        public event EventHandler<(Block<BaseAction> OldTip, Block<BaseAction> NewTip)> BaseBlockChainChangedEvent;
        public event EventHandler<(Block<MarketAction> OldTip, Block<MarketAction> NewTip)> MarketBlockChainChangedEvent;

        public event EventHandler<List<HashDigest<SHA256>>> MarketBlockClearedOldersEvent;
        public event EventHandler<NetworkHeartbeatArgs> NetworkHeartbeatEvent;
        public event EventHandler<PeerStateChangeEventArgs> PeerStateChanged;
        public event EventHandler FreeMarketOneServerLoadedEvent;
        public event EventHandler FreeMarketOneServerLoggedInEvent;
        public event EventHandler<string> LoadingEvent;
        private BackgroundQueue backgroundQueue = new BackgroundQueue();
        public bool IsShuttingDown = false;

        public IBaseConfiguration MakeConfiguration()
        {

            // use an existing datadir controlling directory locations
            // or default to just the app context directory
            if (DataDir == null)
            {
                DataDir = new DataDir();
            }
            Console.WriteLine($"Data dir is: {DataDir.DataDirPath}");
            Console.WriteLine($"Config file name is: {DataDir.ConfigFileName}");
            
            //Configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(DataDir.DataDirPath)
                .AddJsonFile(DataDir.ConfigFile, true, false);
            var configFile = builder.Build();

            var configuration = InitConfigurationHelper.InitializeEnvironment(configFile);

            //Config
            configuration.FullBaseDirectory = DataDir.DataDirPath;
            InitConfigurationHelper.InitializeBaseOnionSeedsEndPoint(configuration, configFile);
            InitConfigurationHelper.InitializeBaseTorEndPoint(configuration, configFile);
            InitConfigurationHelper.InitializeLogFilePath(configuration, configFile);
            InitConfigurationHelper.InitializeMemoryPoolPaths(configuration, configFile);
            InitConfigurationHelper.InitializeBlockChainPaths(configuration, configFile);
            InitConfigurationHelper.InitializeTorUsage(configuration, configFile);
            InitConfigurationHelper.InitializeChatPaths(configuration, configFile);
            InitConfigurationHelper.InitializeSearchEnginePaths(configuration, configFile);
            InitConfigurationHelper.InitializeMinimalPeerAmount(configuration, configFile);
            InitConfigurationHelper.InitializeLocalOnionSeeds(configuration, configFile);

            //IP Helper
            ServerPublicAddress = new IpHelper(configuration);
            InitConfigurationHelper.InitializeListenerEndPoints(configuration, configFile);

            return configuration;
        }

        public async Task<PrivateKeyStates> InitializeAsync(string password = null, UserDataV1 firstUseData = null)
        {
            return await Task.Run(() => this.Initialize(password, firstUseData));
        }
        
        public PrivateKeyStates Initialize(string password = null, UserDataV1 firstUserData = null, string configFilePath = null)
        {
            PrivateKeyStates initResult = PrivateKeyStates.NoPassword;

            //Environment
            Configuration = MakeConfiguration();
            blockChainBasePolicy = ((ExtendedConfiguration)Configuration).BlockChainBasePolicy;
            blockChainMarketPolicy = ((ExtendedConfiguration)Configuration).BlockChainMarketPolicy;
            //Initialize Logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(Path.Combine(Configuration.FullBaseDirectory, Configuration.LogFilePath),
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{Exception}{NewLine}",
                    rollingInterval: RollingInterval.Day, shared: true)
                .CreateLogger();
            _logger = Log.Logger.ForContext<FreeMarketOneServer>();
            _logger.Information("Application Start");

            try
            {
                
                //Initialize Tor as soon as possible, will gain tor startup, circuit build time while user is loging in.
                LoadingEvent?.Invoke(this, "Loading Tor Manager...");
                TorProcessManager = new TorProcessManager(Configuration);
                var torInitialized = TorProcessManager.Start();

                //User manager
                UserManager = new UserManager(Configuration);
                
                initResult = UserManager.Initialize(password, firstUserData);
                
                if (initResult == PrivateKeyStates.Valid)
                {
                    FreeMarketOneServerLoggedInEvent?.Invoke(this, null);
                    Console.WriteLine(ByteUtil.Hex(UserManager.GetCurrentUserPublicKey()));

                    //Service manager
                    LoadingEvent?.Invoke(this, "Loading Service Manager...");
                    ServiceManager = new ServiceManager(Configuration, NetworkHeartbeatEvent);
                    ServiceManager.Start();

                    //Market Manager
                    LoadingEvent?.Invoke(this, "Loading Market Manager...");
                    MarketManager = new MarketManager(Configuration);
                    
                    SpinWait.SpinUntil(() => torInitialized, 10000);
                    if (torInitialized)
                    {
                        //Chat Manager
                        LoadingEvent?.Invoke(this, "Loading Chat Manager...");
                        ChatManager = new ChatManager(Configuration, UserManager.PrivateKey, UserManager, TorProcessManager.TorOnionEndPoint, Configuration.ListenersUseTor ? "127.0.0.1:9050" : null);
                        ChatManager.Start();

                        //Initialize OnionSeeds
                        LoadingEvent?.Invoke(this, "Loading Onion Seed Manager...");
                        OnionSeedsManager = new OnionSeedsManager(Configuration, TorProcessManager, ServerPublicAddress.PublicIP);
                        OnionSeedsManager.Start();

                        //Initialize XRC Daemon
                        LoadingEvent?.Invoke(this, "Loading XRC Daemon...");
                        XRCDaemonClient client = new XRCDaemonClient(new JsonSerializerSettings(), Configuration, _logger);

                        //Initializing Search Indexer
                        LoadingEvent?.Invoke(this, "Loading Local Search Engine...");
                        SearchIndexer = new SearchIndexer(MarketManager, Configuration, new XRCHelper(client), UserManager);
                        SpinWait.SpinUntil(() => SearchIndexer.IsSearchIndexerRunning());

                        //Initialize Base BlockChain Manager
                        LoadingEvent?.Invoke(this, "Loading Base BlockChain Manager...");
                        BaseBlockChainLoadEndedEvent += new EventHandler(Current.BaseBlockChainLoaded);
                        BaseBlockChainChangedEvent += new EventHandler<(Block<BaseAction> OldTip, Block<BaseAction> NewTip)>(Current.BaseBlockChainChanged);
                        PeerStateChanged += new EventHandler<PeerStateChangeEventArgs>(Current.PeerStateChangedHandlerAsync);
                        BaseBlockChainManager = new BlockChainManager<BaseAction>(
                            Configuration,
                            Configuration.BlockChainBasePath,
                            Configuration.BlockChainSecretPath,
                            Configuration.BlockChainBaseGenesis,
                            blockChainBasePolicy,
                            GetPublicIpEndpoint(TorProcessManager.TorOnionEndPoint, ServerPublicAddress.PublicIP, Configuration.ListenerBaseEndPoint),
                            OnionSeedsManager,
                            UserManager.PrivateKey,
                            new NetworkProtocolVersion(),
                            torProcessManager: TorProcessManager,
                            preloadEnded: BaseBlockChainLoadEndedEvent,
                            blockChainChanged: BaseBlockChainChangedEvent,
                            peerStateChangeHandler: PeerStateChanged);

                        LoadingEvent?.Invoke(this, "Starting BaseChain Initial Block Download...");
                        BaseBlockChainManager.Start();

                        //Loading 
                        LoadingEvent?.Invoke(this, "Acquiring Tor Circuit...");
                        BaseBlockChainManager.SwarmServer.WaitForRunningAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                        //ServerPublicAddress.GetMyTorExitIP();

                        //Initialize Base Pool
                        LoadingEvent?.Invoke(this, "Loading Base Pool Manager...");
                        BasePoolManager = new BasePoolManager(
                            Configuration,
                            Configuration.MemoryBasePoolPath,
                            BaseBlockChainManager.SwarmServer,
                            BaseBlockChainManager.PrivateKey,
                            BaseBlockChainManager.BlockChain,
                            blockChainBasePolicy);

                        LoadingEvent?.Invoke(this, "Starting Base PoolManager...");
                        BasePoolManager.Start();

                        //Add Swarm server to seed manager
                        OnionSeedsManager.BaseSwarm = BaseBlockChainManager.SwarmServer;

                        //Initialize Market Blockchain Manager
                        LoadingEvent?.Invoke(this, "Loading Market BlockChain Manager...");
                        //triggering market chain run without waiting base chain to complete
                        MarketBlockChainLoadEndedEvent += new EventHandler(Current.MarketBlockChainLoaded);
                        MarketBlockChainChangedEvent += new EventHandler<(Block<MarketAction> OldTip, Block<MarketAction> NewTip)>(Current.MarketBlockChainChanged);

                        var hashCheckPoints = BaseBlockChainManager.GetActionItemsByType(typeof(CheckPointMarketDataV1));
                        
                        var genesisBlock = BlockHelper.GetGenesisMarketBlockByHash(hashCheckPoints, blockChainMarketPolicy);

                        MarketBlockChainManager = new BlockChainManager<MarketAction>(
                            Configuration,
                            Configuration.BlockChainMarketPath,
                            Configuration.BlockChainSecretPath,
                            Configuration.BlockChainMarketGenesis,
                            blockChainMarketPolicy,
                            GetPublicIpEndpoint(TorProcessManager.TorOnionEndPoint, ServerPublicAddress.PublicIP, Configuration.ListenerMarketEndPoint),
                            OnionSeedsManager,
                            UserManager.PrivateKey,
                            new NetworkProtocolVersion(),
                            torProcessManager: TorProcessManager,
                            hashCheckPoints,
                            genesisBlock,
                            preloadEnded: MarketBlockChainLoadEndedEvent,
                            blockChainChanged: MarketBlockChainChangedEvent,
                            clearedOlderBlocks: MarketBlockClearedOldersEvent,
                            peerStateChangeHandler: PeerStateChanged);
                        LoadingEvent?.Invoke(this, "Starting MarketChain Initial Block Download...");
                        MarketBlockChainManager.Start();

                        //Initialize Market Pool Manager
                        if (MarketBlockChainManager.IsBlockChainManagerRunning())
                        {
                            LoadingEvent?.Invoke(this, "Loading Market Pool Manager...");
                            //Add Swarm server to seed manager
                            OnionSeedsManager.MarketSwarm = MarketBlockChainManager.SwarmServer;

                            MarketPoolManager = new MarketPoolManager(
                                Configuration,
                                Configuration.MemoryBasePoolPath,
                                MarketBlockChainManager.SwarmServer,
                                MarketBlockChainManager.PrivateKey,
                                MarketBlockChainManager.BlockChain,
                                blockChainMarketPolicy);

                            LoadingEvent?.Invoke(this, "Starting Market PoolManager...");
                            MarketPoolManager.Start();
                            LoadingEvent?.Invoke(this, "");

                        }
                        else
                        {
                            _logger.Error("Market Chain isnt loaded!");
                            Stop();
                        }

                        //Initializing Search Engine
                        LoadingEvent?.Invoke(this, "Loading Local Search Engine...");
                        SearchIndexer.Initialize(BasePoolManager, BaseBlockChainManager);
                        SearchEngine = new SearchEngine(MarketManager, SearchHelper.GetDataFolder(Configuration));
                    }
                    else
                    {
                        _logger.Error("Unexpected error. Could not automatically start Tor. Try running Tor manually.");
                    }
                }
                else if(initResult == PrivateKeyStates.WrongPassword)
                {
                    LoadingEvent?.Invoke(this, "Login failed...");
                    _logger.Error("Wrong password supplied.");
                    //throw new BadPasswordException();
                }
                else
                {
                    _logger.Warning("No user account is necessary to create one.");
                }               
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                _logger.Error(ex.StackTrace);
            }
            return initResult;
        }

        private async void PeerStateChangedHandlerAsync(object sender, PeerStateChangeEventArgs e)
        {
            if (e.Peer.EndPoint != null)
            {
                var endPoint = e.Peer.EndPoint;
                if (endPoint.Port == 9114 && (e.Change.Equals(PeerStateChange.Joined) || e.Change.Equals(PeerStateChange.TwoWayDialogConfirmed)))
                {
                    var sibling = new BoundPeer(e.Peer.PublicKey, new DnsEndPoint(e.Peer.EndPoint.Host, 9113));
                    var peers = new List<Peer>
                    {
                        sibling
                    };
                    if (BaseBlockChainManager != null 
                        && BaseBlockChainManager.SwarmServer != null 
                        && !BaseBlockChainManager.SwarmServer.Peers.Where(
                            p => p.EndPoint.Host.Equals(e.Peer.EndPoint.Host)).Any())
                    {
                        try
                        {
                            await BaseBlockChainManager.SwarmServer.AddPeersAsync(peers, TimeSpans.HalfMinute);
                        }
                        catch (Exception)
                        {
                            //swallow
                        }
                        
                    }
                }
                if (endPoint.Port == 9113 && (e.Change.Equals(PeerStateChange.Joined) ||e.Change.Equals(PeerStateChange.TwoWayDialogConfirmed)))
                {
                    var sibling = new BoundPeer(e.Peer.PublicKey, new DnsEndPoint(e.Peer.EndPoint.Host, 9114));
                    var peers = new List<Peer>
                    {
                        sibling
                    };
                    //if peer not exists is peers list
                    if (MarketBlockChainManager != null 
                        && MarketBlockChainManager.SwarmServer != null 
                        && !MarketBlockChainManager.SwarmServer.Peers.Where(
                            p => p.EndPoint.Host.Equals(e.Peer.EndPoint.Host)).Any()){
                        try
                        {
                            await MarketBlockChainManager.SwarmServer.AddPeersAsync(peers, TimeSpans.HalfMinute);
                        }
                        catch (Exception)
                        {
                            //swallow not very important just a helper
                        }                        
                    }
                    
                }
            }
        }

        private EndPoint GetPublicIpEndpoint(String torOnionAddress, IPAddress  ipAddress,  int port)
        {
            if (Configuration.ListenersUseTor)
            {
                return new DnsEndPoint(torOnionAddress, port);
            }
            else
            {
                return new IPEndPoint(EndPointHelper.ParseIPEndPoint("127.0.0.1").Address, port);
            }
        }

        private void BaseBlockChainLoaded(object sender, EventArgs e)
        {
            try
            {
                //Initialize Base Pool Manager
                if (BaseBlockChainManager.IsBlockChainManagerRunning())
                {
                    //Add Swarm server to seed manager
                    OnionSeedsManager.BaseSwarm = BaseBlockChainManager.SwarmServer;

                   
                }
                else
                {
                    _logger.Error("Base Chain isnt loaded!");
                    Stop();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                _logger.Error(ex.StackTrace);
            }
        }

        private void MarketBlockChainLoaded(object sender, EventArgs e)
        {
            //Event that server is loaded
            RaiseAsyncServerLoadedEvent();
            return;
            try
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
                        MarketBlockChainManager.SwarmServer,
                        MarketBlockChainManager.PrivateKey,
                        MarketBlockChainManager.BlockChain,
                        blockChainMarketPolicy);

                    LoadingEvent?.Invoke(this, "Starting Market PoolManager...");
                    MarketPoolManager.Start();
                    LoadingEvent?.Invoke(this, "");

                    //Event that server is loaded
                    RaiseAsyncServerLoadedEvent();
                }
                else
                {
                    _logger.Error("Market Chain isnt loaded!");
                    Stop();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                _logger.Error(ex.StackTrace);
            }
        }

        async void RaiseAsyncServerLoadedEvent()
        {
            await Task.Delay(1000);
            await Task.Run(() =>
            {
                try
                {
                    if (UserManager != null)
                    {
                        if (UserManager.UsedDataForceToPropagate && (UserManager.UserData != null))
                        {
                            if (BasePoolManager.AcceptActionItem(UserManager.UserData) == null)
                            {
                                BasePoolManager.PropagateAllActionItemLocal(true);
                            }
                        }
                        else
                        {
                            //loading actual user data from pool or blockchain
                            var userData = UserManager.GetActualUserData(Current.BasePoolManager, Current.BaseBlockChainManager);
                            if ((userData != null) && (userData != UserManager.UserData))
                            {
                                UserManager.SaveUserData(userData, Configuration.FullBaseDirectory, Configuration.BlockChainUserPath);
                            }
                        }
                    }

                    FreeMarketOneServerLoadedEvent?.Invoke(this, null);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message);
                    _logger.Error(ex.StackTrace);
                }
            }).ConfigureAwait(true);
        }

        /// <summary>
        /// Processing event Base BlockChain Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BaseBlockChainChanged(object sender, (Block<BaseAction> OldTip, Block<BaseAction> NewTip) e)
        {
            try
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
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                _logger.Error(ex.StackTrace);
            }
        }

        /// <summary>
        /// Processing event Market BlockChain Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MarketBlockChainChanged(object sender, (Block<MarketAction> OldTip, Block<MarketAction> NewTip) e)
        {
            try
            {
                if (e.NewTip != null)
                {
                    _logger.Information($"Recieved market block downloaded notification {e.NewTip.Hash}");

                    _logger.Information($"New block SearchIndexing");
                    backgroundQueue.QueueTask(() => SearchIndexer.IndexBlock(e.NewTip));

                    _logger.Information("New block processing for chat - item hash");
                    Current.ChatManager.ProcessNewBlock(e.NewTip);
                }
                else if ((e.NewTip == null) && (e.OldTip != null))
                {
                    _logger.Information($"Recieved market orphaned block notification {e.OldTip.Hash}");

                    _logger.Information($"Clearing block SearchIndexing");
                    SearchIndexer.UnIndexBlock(e.OldTip);

                    //TODO: CHAT?????
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                _logger.Error(ex.StackTrace);
            }
        }

        /// <summary>
        /// Get information about state of server
        /// </summary>
        /// <returns></returns>
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

            IsShuttingDown = true;
            _logger?.Information("Application End");
        }
    }
}

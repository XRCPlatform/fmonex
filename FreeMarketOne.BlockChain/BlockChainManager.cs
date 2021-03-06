using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.Net;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Serilog;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.P2P;
using FreeMarketOne.DataStructure;
using FreeMarketOne.BlockChain.Policy;
using FreeMarketOne.Extensions.Helpers;
using Libplanet.Extensions.Helpers;
using Libplanet.Extensions;
using FreeMarketOne.GenesisBlock;
using System.Security.Cryptography;
using FreeMarketOne.DataStructure.ProtocolVersions;
using FreeMarketOne.Extensions.Common;
using static FreeMarketOne.Extensions.Common.ServiceHelper;
using Libplanet.Store;
using Libplanet.Net.Messages;
using FreeMarketOne.Tor;
using Libplanet.Store.Trie;

namespace FreeMarketOne.BlockChain
{
    public class BlockChainManager<T> : IBlockChainManager<T>, IDisposable where T : IBaseAction, new()
    {
        private ILogger _logger { get; set; }

        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped
        /// </summary>
        private CommonStates _running;

        public bool IsRunning => _running == CommonStates.Running;
        private CancellationTokenSource _cancellationToken { get; set; }

        private string _blockChainFilePath { get; set; }
        private string _blockChainGenesisName { get; set; }
        private EndPoint _endPoint { get; set; }

        private UserPrivateKey _privateKey { get; set; }
        private BlockChain<T> _blockChain;
        private DefaultStore _storage;
        private IStateStore _storageState;
        private Swarm<T> _swarmServer;
        private ImmutableList<Peer> _seedPeers;
        private IImmutableSet<Address> _trustedPeers;

        private IOnionSeedsManager _onionSeedManager;
        private IProtocolVersion _protocolVersion;
        private PeerBootstrapWorker<T> _peerBootstrapWorker { get; set; }
        private List<CheckPointMarketDataV1> _hashCheckPoints { get; set; }
        private EventHandler _bootstrapStarted { get; set; }
        private EventHandler _preloadStarted { get; set; }
        private EventHandler<PreloadState> _preloadProcessed { get; set; }
        private EventHandler _preloadEnded { get; set; }
        private EventHandler<(Block<T> OldTip, Block<T> NewTip)> _blockChainChanged { get; set; }
        private EventHandler<PeerStateChangeEventArgs> _peerStateChangeHandler { get; set; }

        private EventHandler<List<HashDigest<SHA256>>> _clearedOlderBlocks { get; set; }
        private IBaseConfiguration _configuration { get; }
        private IDefaultBlockPolicy<T> _blockChainPolicy { get; }
        private Block<T> _genesisBlock { get; }

        public BlockChain<T> BlockChain { get => _blockChain; }
        public DefaultStore Storage { get => _storage; }
        public IStateStore StorageState { get => _storageState; }
        public Swarm<T> SwarmServer { get => _swarmServer; }
        public UserPrivateKey PrivateKey { get => _privateKey; }
        private TorProcessManager _torProcessManager;
        /// <summary>
        /// BlockChain Manager which operate specified blockchain data
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="blockChainPath"></param>
        /// <param name="blockChainSecretPath"></param>
        /// <param name="blockChainGenesisName"></param>
        /// <param name="blockChainPolicy"></param>
        /// <param name="endPoint"></param>
        /// <param name="seedsManager"></param>
        /// <param name="listHashCheckPoints"></param>
        /// <param name="bootstrapStarted"></param>
        /// <param name="preloadStarted"></param>
        /// <param name="preloadProcessed"></param>
        /// <param name="preloadEnded"></param>
        public BlockChainManager(
            IBaseConfiguration configuration,
            string blockChainBasePath,
            string blockChainSecretPath,
            string blockChainGenesisName,
            IDefaultBlockPolicy<T> blockChainPolicy,
            EndPoint endPoint,
            IOnionSeedsManager seedsManager,
            UserPrivateKey userPrivateKey,
            IProtocolVersion protocolVersion,
            TorProcessManager torProcessManager,
            List<IBaseItem> listHashCheckPoints = null,
            Block<T> genesisBlock = null,
            EventHandler bootstrapStarted = null,
            EventHandler preloadStarted = null,
            EventHandler<PreloadState> preloadProcessed = null,
            EventHandler preloadEnded = null,
            EventHandler<(Block<T> OldTip, Block<T> NewTip)> blockChainChanged = null,
            EventHandler<List<HashDigest<SHA256>>> clearedOlderBlocks = null,
            EventHandler<PeerStateChangeEventArgs> peerStateChangeHandler = null)
        {
            _logger = Log.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                string.Format("{0}.{1}.{2}", typeof(BlockChainManager<T>).Namespace, typeof(BlockChainManager<T>).Name.Replace("`1", string.Empty), typeof(T).Name));

            _configuration = configuration;
            _blockChainFilePath = blockChainBasePath;
            _blockChainPolicy = blockChainPolicy;
            _blockChainGenesisName = blockChainGenesisName;
            _endPoint = endPoint;
            _torProcessManager = torProcessManager;
            _privateKey = userPrivateKey;

            var path = Path.Combine(_configuration.FullBaseDirectory, _blockChainFilePath);
            _storage = new DefaultStore(path);

            IKeyValueStore stateKeyValueStore = new DefaultKeyValueStore(Path.Combine(path, "states")),
            stateHashKeyValueStore = new DefaultKeyValueStore(Path.Combine(path, "state_hashes"));
            _storageState = new TrieStateStore(stateKeyValueStore, stateHashKeyValueStore);

            _onionSeedManager = seedsManager;

            if (genesisBlock == null)
            {
                _genesisBlock = GetGenesisBlock();
            }
            else
            {
                _genesisBlock = genesisBlock;
                ClearOlderBlocksAfterNewGenesis();
            }

            _bootstrapStarted = bootstrapStarted;
            _preloadStarted = preloadStarted;
            _preloadProcessed = preloadProcessed;
            _preloadEnded = preloadEnded;
            _blockChainChanged = blockChainChanged;
            _clearedOlderBlocks = clearedOlderBlocks;
            _protocolVersion = protocolVersion;
            _peerStateChangeHandler = peerStateChangeHandler;

            _logger.Information(string.Format("Initializing BlockChain Manager for : {0}", typeof(T).Name));
        }

        public bool Start()
        {
            _cancellationToken = new CancellationTokenSource();
            var host = _endPoint.GetHostOrDefault();
            int? port = _endPoint.GetPortOrDefault();

            _blockChain = new BlockChain<T>(
                _blockChainPolicy,
                new VolatileStagePolicy<T>(),
                _storage,
                _storageState,
                _genesisBlock
            );

            //remove obsolete data from storage
            RemoveObsoleteData();

            //event for new block accepted
            if (_blockChainChanged != null)
                _blockChain.TipChanged += _blockChainChanged;

            if (host != null)
            {
                _swarmServer = new Swarm<T>(
                    blockChain: _blockChain,
                    privateKey: _privateKey,
                    appProtocolVersion: _protocolVersion.GetProtocolVersion(),
                    torProcessManager: _torProcessManager,
                    host: host,
                    listenPort: port,
                    iceServers: null,
                    differentAppProtocolVersionEncountered: _protocolVersion.DifferentAppProtocolVersionEncountered,
                    trustedAppProtocolVersionSigners: _protocolVersion.GetProtocolSigners(),
                    options: new SwarmOptions {
                        Socks5Proxy = _configuration.ListenersUseTor ? "127.0.0.1:9050" : null
                    },
                    peerStateChangeHandler: _peerStateChangeHandler
                );

                var peers = GetPeersFromOnionManager(typeof(T));
                _seedPeers = peers.Where(peer => peer.PublicKey != _privateKey.PublicKey).ToImmutableList();
                _trustedPeers = _seedPeers.Select(peer => peer.Address).ToImmutableHashSet();

                _running = CommonStates.Running;

                //init Peer Bootstrap Worker
                _peerBootstrapWorker = new PeerBootstrapWorker<T>(
                    _logger,
                    _swarmServer,
                    _blockChain,
                    _seedPeers,
                    _trustedPeers,
                    _privateKey,
                    _bootstrapStarted,
                    _preloadStarted,
                    _preloadProcessed,
                    _preloadEnded);

                var coBoostrapRunner = new CoroutineManager();
                coBoostrapRunner.RegisterCoroutine(_peerBootstrapWorker.GetEnumerator());
                coBoostrapRunner.Start();

            }
            else
            {
                _logger.Error(string.Format("No host information"));
                Stop();
            }

            return true;
        }

        private List<Peer> GetPeersFromOnionManager(Type typeOfT)
        {
            var peers = new List<Peer>();

            while (!_onionSeedManager.IsOnionSeedsManagerRunning())
            {
                Thread.Sleep(100);
            }

            //ignore me as peer
            var pubKey = ByteUtil.Hex(_privateKey.PublicKey.Format(true));

            foreach (var itemPeer in _onionSeedManager.OnionSeedPeers)
            {
                var port = itemPeer.PortBlockChainBase;

                if (itemPeer.PublicKeyHex == pubKey) continue;
                if (typeOfT != typeof(BaseAction)) port = itemPeer.PortBlockChainMaster;

                var publicKey = new PublicKey(ByteUtil.ParseHex(itemPeer.PublicKeyHex));
                var boundPeer = new BoundPeer(publicKey, new DnsEndPoint(itemPeer.UrlBlockChain, port));
                peers.Add(boundPeer);
            }

            return peers;
        }

        public bool IsBlockChainManagerRunning()
        {
            if (_running == CommonStates.Running)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public List<IBaseItem> GetActionItemsByType(Type type)
        {
            var result = new List<IBaseItem>();
            var hashs = _storage.IterateBlockHashes();

            if (hashs.Any())
            {
                hashs = hashs.Reverse().ToHashSet();
                var i = 1;

                foreach (var itemHash in hashs)
                {
                    if (i > 100) break;

                    var block = _storage.GetBlock<T>(itemHash);
                    if (block.Transactions.Any()) {
                        foreach (var itemTx in block.Transactions)
                        {
                            if (itemTx.Actions.Any())
                            {
                                foreach (var itemAction in itemTx.Actions)
                                {
                                    if (itemAction.BaseItems.Any())
                                    {
                                        foreach (var itemBase in itemAction.BaseItems)
                                        {
                                            if (itemBase.GetType() == type)
                                            {
                                                i++;
                                                result.Add(itemBase);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        public Block<T> GetGenesisBlock()
        {
            var genesisHelper = new GenesisHelper();
            var genesisBytes = genesisHelper.GetGenesis(_blockChainGenesisName);

            return new Block<T>().Deserialize(genesisBytes);
        }

        public void Stop()
        {
            _running = CommonStates.Stopping;

            _peerBootstrapWorker?.Dispose();
            _peerBootstrapWorker = null;

            _cancellationToken?.Cancel();
            _cancellationToken?.Dispose();
            _cancellationToken = null;

            Task.Run(async () => await _swarmServer.StopAsync()).ContinueWith(_ =>
            {
                _storage?.Dispose();
            }).Wait(2000);

            _logger.Information(string.Format("BlockChain {0} Manager stopped.", typeof(T).Name));
        }

        /// <summary>
        /// Clear older blocks until reach actual genesis
        /// </summary>
        public List<HashDigest<SHA256>> ClearOlderBlocksAfterNewGenesis()
        {
            var chainId = _storage.GetCanonicalChainId();
            var clearedBlocks = new List<HashDigest<SHA256>>();

            if (chainId.HasValue)
            {
                var hashs = _storage.IterateIndexes(chainId.Value, 0, null);
                if (hashs.Any())
                {
                    foreach (var itemHash in hashs)
                    {
                        var block = _storage.GetBlock<T>(itemHash);
                        if (block == _genesisBlock)
                        {
                            if (_clearedOlderBlocks != null) RaiseAsyncClearedOlderBlocksEvent(clearedBlocks);
                            break;
                        }
                        else
                        {
                            _storage.DeleteBlock(itemHash);
                            clearedBlocks.Add(itemHash);
                        }
                    }
                }
            }

            return clearedBlocks;
        }

        async void RaiseAsyncClearedOlderBlocksEvent(List<HashDigest<SHA256>> clearedBlocks)
        {
            await Task.Delay(1000);
            await Task.Run(() =>
            {
                _clearedOlderBlocks?.Invoke(this, clearedBlocks);
            }).ConfigureAwait(true);
        }

        public void Dispose()
        {
            Stop();

            _running = CommonStates.Stopped;
        }

        public async Task ReConnectAfterNetworkLossAsync()
        {
            if (!_swarmServer.Peers.Any())
            {
                var peers = GetPeersFromOnionManager(typeof(T));
                _seedPeers = peers.Where(peer => peer.PublicKey != _privateKey.PublicKey).ToImmutableList();

                //blocking calls deliberately as we don't want to create a DDOS attack here when newtrok re-connected
                await _swarmServer.AddPeersAsync(_seedPeers, TimeSpan.FromSeconds(30));
                await _swarmServer.CheckAllPeersAsync();

                //same script is executed at service manager but this could kick in earlier than service manager poll period
                var diff = await ValidateChainAgainstNetwork();
                if (diff.Any())
                {
                    await PullRemoteChainDifferences();
                }
            }
        }

        public async Task<IEnumerable<PeerChainState>> ValidateChainAgainstNetwork()
        {
            var states = await _swarmServer.GetPeerChainStateAsync(TimeSpan.FromMinutes(1), new CancellationToken());            
            return states.Where(peerToCheck => peerToCheck.TipIndex> BlockChain.Tip.Index && peerToCheck.TotalDifficulty > BlockChain.Tip.TotalDifficulty);
        }

        public async Task PullRemoteChainDifferences()
        {
            DateTimeOffset started = DateTimeOffset.UtcNow;
            long existingBlocks = _blockChain?.Tip?.Index ?? 0;
            _logger.Information("Preloading [resync] starts");

            try
            {
                await _swarmServer.PreloadAsync(null, null, cancellationToken: _cancellationToken.Token);
            }
            catch (AggregateException e)
            {
                if (e.InnerException is InvalidGenesisBlockException)
                {
                    _logger.Error(string.Format("Preloading [resync] terminated with silence exception: {0}", e));
                }
                else
                {
                    _logger.Error(string.Format("Preloading [resync] terminated with an exception: {0}", e));
                    throw e;
                }
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("Preloading [resync] terminated with an exception: {0}", e));
                throw e;
            }

            DateTimeOffset ended = DateTimeOffset.UtcNow;
            var index = _blockChain?.Tip?.Index ?? 0;
            _logger.Information("Preloading [resync] finished; elapsed time: {0}; blocks: {1}",
                ended - started,
                index - existingBlocks
            );
        }

        /// <summary>
        /// Method remove absolete data from storage
        /// </summary>
        private void RemoveObsoleteData()
        {
            var pendingObsoleteTxs = _storage.IterateStagedTransactionIds().ToImmutableHashSet();
            _storage.UnstageTransactionIds(pendingObsoleteTxs);

            var chainIds = _storage.ListChainIds().ToList();
            var obsoletedChainIds = chainIds.Where(chainId => chainId != _blockChain.Id).ToList();

            foreach (Guid chainId in obsoletedChainIds)
            {
                _storage.DeleteChainId(chainId);
            }
        }
    }
}

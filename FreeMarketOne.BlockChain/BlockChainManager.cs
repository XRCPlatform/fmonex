﻿using System;
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
using Libplanet.RocksDBStore;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.P2P;
using FreeMarketOne.BlockChain.Helpers;
using FreeMarketOne.BlockChain.Actions;

namespace FreeMarketOne.BlockChain
{
    public class BlockChainManager<T> : IBlockChainManager<T>, IDisposable where T : IBaseAction, new()
    {
        private ILogger _logger { get; set; }

        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped
        /// </summary>
        private long _running;

        public bool IsRunning => Interlocked.Read(ref _running) == 1;
        private CancellationTokenSource _cancellationToken { get; set; }

        private string _blockChainFilePath { get; set; }
        private EndPoint _endPoint { get; set; }

        private static readonly TimeSpan blockInterval = TimeSpan.FromSeconds(10);
        private PrivateKey _privateKey { get; set; }
        private BlockChain<T> _blockChain;
        private RocksDBStore _storage;
        private Swarm<T> _swarmServer;
        private ImmutableList<Peer> _seedPeers;
        private IImmutableSet<Address> _trustedPeers;

        private OnionSeedsManager _onionSeedManager;
        private PeerBootstrapWorker<T> _peerBootstrapWorker { get; set; }
        private ProofOfWorkWorker<T> _proofOfWorkWorker { get; set; }
        private List<CheckPointMarketDataV1> _hashCheckPoints { get; set; }
        private EventHandler _bootstrapStarted { get; set; }
        private EventHandler _preloadStarted { get; set; }
        private EventHandler<PreloadState> _preloadProcessed { get; set; }
        private EventHandler _preloadEnded { get; set; }
        private EventHandler<BlockChain<T>.TipChangedEventArgs> _blockChainChanged { get; set; }
        
        public BlockChain<T> BlockChain { get => _blockChain; }
        public RocksDBStore Storage { get => _storage; }
        public Swarm<T> SwarmServer { get => _swarmServer; }

        /// <summary>
        /// BlockChain Manager which operate specified blockchain data
        /// </summary>
        /// <param name="serverLogger"></param>
        /// <param name="blockChainPath"></param>
        /// <param name="blockChainSecretPath"></param>
        /// <param name="endPoint"></param>
        /// <param name="seedsManager"></param>
        /// <param name="listHashCheckPoints"></param>
        /// <param name="bootstrapStarted"></param>
        /// <param name="preloadStarted"></param>
        /// <param name="preloadProcessed"></param>
        /// <param name="preloadEnded"></param>
        /// <param name="blockChainChanged"></param>
        public BlockChainManager(ILogger serverLogger,
            string blockChainPath,
            string blockChainSecretPath,
            EndPoint endPoint,
            IOnionSeedsManager seedsManager,
            List<IBaseItem> listHashCheckPoints = null,
            EventHandler bootstrapStarted = null,
            EventHandler preloadStarted = null,
            EventHandler<PreloadState> preloadProcessed = null,
            EventHandler preloadEnded = null,
            EventHandler<BlockChain<T>.TipChangedEventArgs> blockChainChanged = null)
        {
            _logger = serverLogger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, typeof(T).FullName);
            _blockChainFilePath = blockChainPath;
            _endPoint = endPoint;

            _privateKey = GetSecret(blockChainSecretPath);
            _storage = new RocksDBStore(_blockChainFilePath);
            _onionSeedManager = (OnionSeedsManager)seedsManager;

            if (listHashCheckPoints != null)
            {
                _hashCheckPoints = listHashCheckPoints.Select(a => (CheckPointMarketDataV1)a).ToList();
            }

            _bootstrapStarted = bootstrapStarted;
            _preloadStarted = preloadStarted;
            _preloadProcessed = preloadProcessed;
            _preloadEnded = preloadEnded;
            _blockChainChanged = blockChainChanged;

            _logger.Information(string.Format("Initializing BlockChain Manager for : {0}", typeof(T).Name));
        }

        private PrivateKey GetSecret(string path)
        {
            if (File.Exists(path))
            {
                var keyBytes = File.ReadAllBytes(path);
                return new PrivateKey(keyBytes);
            } 
            else
            {
                var newKey = new PrivateKey();
                File.WriteAllBytes(path, newKey.ByteArray);

                return newKey;
            }
        }

        private bool DifferentAppProtocolVersionEncountered(
            Peer peer,
            AppProtocolVersion peerVersion,
            AppProtocolVersion localVersion)
        {
            return false;
        }

        public bool Start()
        {
            _cancellationToken = new CancellationTokenSource();
            Block<T> genesis = GetGenesisBlock();
            var host = _endPoint.GetHostOrDefault();
            int? port = _endPoint.GetPortOrDefault();

            var appProtocolVersion = default(AppProtocolVersion);
            var policy = new BlockPolicy<T>(
                    null,
                    blockInterval,
                    100000,
                    2048);

            _blockChain = new BlockChain<T>(
                policy,
                _storage,
                genesis
            );

            //event for new block accepted
            if (_blockChainChanged != null)
                _blockChain.TipChanged += _blockChainChanged;

            if (host != null)
            {
                _swarmServer = new Swarm<T>(
                    _blockChain,
                    _privateKey,
                    appProtocolVersion: appProtocolVersion,
                    host: host,
                    listenPort: port,
                    iceServers: null,
                    differentAppProtocolVersionEncountered: DifferentAppProtocolVersionEncountered,
                    trustedAppProtocolVersionSigners: null);

                var peers = GetPeersFromOnionManager();
                //new List<Peer>(); // 
                _seedPeers = peers.Where(peer => peer.PublicKey != _privateKey.PublicKey).ToImmutableList();
                _trustedPeers = _seedPeers.Select(peer => peer.Address).ToImmutableHashSet();

                Interlocked.Exchange(ref _running, 1);

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

                _proofOfWorkWorker = new ProofOfWorkWorker<T>(
                    _logger,
                    _swarmServer,
                    _blockChain,
                    _privateKey.ToAddress(),
                    _storage,
                    _privateKey
                    );

                //var coProofOfWorkRunner = new CoroutineManager();
                //coProofOfWorkRunner.RegisterCoroutine(proofOfWorkWorker.GetEnumerator());
                //coProofOfWorkRunner.Start();
            } 
            else
            {
                _logger.Error(string.Format("No host information"));
                Stop();
            }

            return true;
        }

        private List<Peer> GetPeersFromOnionManager()
        {
            var peers = new List<Peer>();

            while (!_onionSeedManager.IsOnionSeedsManagerRunning())
            {
                Thread.Sleep(100);
            }

            foreach (var itemPeer in _onionSeedManager.OnionSeedPeers)
            {
                //REMOVE: TEST - HACK
                //itemPeer.SecretKeyHex = ByteUtil.Hex(new PrivateKey().PublicKey.Format(true));

                var publicKey = new PublicKey(ByteUtil.ParseHex(itemPeer.SecretKeyHex));
                var boundPeer = new BoundPeer(publicKey, new DnsEndPoint(itemPeer.Url, itemPeer.Port), default(AppProtocolVersion));
                peers.Add(boundPeer);
            }

            return peers;
        }

        public bool IsBlockChainManagerRunning()
        {
            if (Interlocked.Read(ref _running) == 1)
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
                hashs = hashs.Reverse();
                var i = 1;

                foreach (var itemHash in hashs)
                {
                    if (i > 10) break;

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
            var genesisPath = Path.Combine(_blockChainFilePath, "genesis.dat");

            if (File.Exists(genesisPath))
            {
                var genesisBytes = File.ReadAllBytes(genesisPath);
                return Block<T>.Deserialize(genesisBytes);
            } 
            else
            {
                _logger.Error("Genesis block doesn't exist.");
                return null;
            }
        }

        public void Stop()
        {
            Interlocked.Exchange(ref _running, 2);

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

        public void Dispose()
        {
            Stop();

            Interlocked.Exchange(ref _running, 3);
        }
    }
}
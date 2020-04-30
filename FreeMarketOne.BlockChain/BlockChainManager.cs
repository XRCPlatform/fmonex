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
using Libplanet.RocksDBStore;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.P2P;
using FreeMarketOne.BlockChain.Helpers;
using FreeMarketOne.BlockChain.Actions;

namespace FreeMarketOne.BlockChain
{
    public class BlockChainManager<T> : IBlockChainManager, IDisposable where T : IBaseAction, new()
    {
        private ILogger logger { get; set; }

        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped
        /// </summary>
        private long running;

        public bool IsRunning => Interlocked.Read(ref running) == 1;
        private CancellationTokenSource cancellationToken { get; set; }

        private string blockChainFilePath { get; set; }
        private EndPoint endPoint { get; set; }

        private static readonly TimeSpan blockInterval = TimeSpan.FromSeconds(10);
        private PrivateKey privateKey { get; set; }
        private BlockChain<T> blockChain;
        private RocksDBStore storage;
        private Swarm<T> swarmServer;
        private ImmutableList<Peer> seedPeers;
        private IImmutableSet<Address> trustedPeers;

        private OnionSeedsManager onionSeedManager;
        private PeerBootstrapWorker<T> peerBootstrapWorker { get; set; }
        private ProofOfWorkWorker<T> proofOfWorkWorker { get; set; }
        private List<CheckPointMarketDataV1> hashCheckPoints { get; set; }
        private EventHandler bootstrapStarted { get; set; }
        private EventHandler preloadStarted { get; set; }
        private EventHandler<PreloadState> preloadProcessed { get; set; }
        private EventHandler preloadEnded { get; set; }
        private EventHandler<BlockChain<T>.TipChangedEventArgs> blockChainChanged { get; set; }

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
            this.logger = serverLogger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, typeof(T).FullName);
            this.blockChainFilePath = blockChainPath;
            this.endPoint = endPoint;

            this.privateKey = GetSecret(blockChainSecretPath);

            this.storage = new RocksDBStore(this.blockChainFilePath);

            this.onionSeedManager = (OnionSeedsManager)seedsManager;

            if (listHashCheckPoints != null)
            {
                this.hashCheckPoints = listHashCheckPoints.Select(a => (CheckPointMarketDataV1)a).ToList();
            }

            this.bootstrapStarted = bootstrapStarted;
            this.preloadStarted = preloadStarted;
            this.preloadProcessed = preloadProcessed;
            this.preloadEnded = preloadEnded;
            this.blockChainChanged = blockChainChanged;

            logger.Information(string.Format("Initializing BlockChain Manager for : {0}",  typeof(T).Name));
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
            this.cancellationToken = new CancellationTokenSource();
            Block<T> genesis = GetGenesisBlock();
            var host = this.endPoint.GetHostOrDefault();
            int? port = this.endPoint.GetPortOrDefault();

            var appProtocolVersion = default(AppProtocolVersion);
            var policy = new BlockPolicy<T>(
                    null,
                    blockInterval,
                    100000,
                    2048);

            this.blockChain = new BlockChain<T>(
                policy,
                this.storage,
                genesis
            );

            //event for new block accepted
            if (blockChainChanged != null)
                this.blockChain.TipChanged += blockChainChanged;

            if (host != null)
            {
                this.swarmServer = new Swarm<T>(
                    this.blockChain,
                    this.privateKey,
                    appProtocolVersion: appProtocolVersion,
                    host: host,
                    listenPort: port,
                    iceServers: null,
                    differentAppProtocolVersionEncountered: DifferentAppProtocolVersionEncountered,
                    trustedAppProtocolVersionSigners: null);

                var peers = GetPeersFromOnionManager();
                //new List<Peer>(); // 
                this.seedPeers = peers.Where(peer => peer.PublicKey != this.privateKey.PublicKey).ToImmutableList();
                this.trustedPeers = seedPeers.Select(peer => peer.Address).ToImmutableHashSet();

                Interlocked.Exchange(ref running, 1);

                //init Peer Bootstrap Worker
                this.peerBootstrapWorker = new PeerBootstrapWorker<T>(
                    this.logger,
                    this.swarmServer,
                    this.blockChain,
                    this.seedPeers,
                    this.trustedPeers,
                    this.privateKey,
                    this.bootstrapStarted,
                    this.preloadStarted,
                    this.preloadProcessed,
                    this.preloadEnded);

                var coBoostrapRunner = new CoroutineManager();
                coBoostrapRunner.RegisterCoroutine(peerBootstrapWorker.GetEnumerator());
                coBoostrapRunner.Start();

                this.proofOfWorkWorker = new ProofOfWorkWorker<T>(
                    this.logger,
                    this.swarmServer,
                    this.blockChain,
                    this.privateKey.ToAddress(),
                    this.storage,
                    this.privateKey
                    );

                //var coProofOfWorkRunner = new CoroutineManager();
                //coProofOfWorkRunner.RegisterCoroutine(proofOfWorkWorker.GetEnumerator());
                //coProofOfWorkRunner.Start();
            } 
            else
            {
                logger.Error(string.Format("No host information"));
                Stop();
            }

            return true;
        }

        private List<Peer> GetPeersFromOnionManager()
        {
            var peers = new List<Peer>();

            while (!this.onionSeedManager.IsOnionSeedsManagerRunning())
            {
                Thread.Sleep(100);
            }

            foreach (var itemPeer in onionSeedManager.OnionSeedPeers)
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
            if (Interlocked.Read(ref running) == 1)
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
            var hashs = this.storage.IterateBlockHashes();

            if (hashs.Any())
            {
                hashs = hashs.Reverse();
                var i = 1;

                foreach (var itemHash in hashs)
                {
                    if (i > 10) break;

                    var block = this.storage.GetBlock<T>(itemHash);
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
            var genesisPath = Path.Combine(this.blockChainFilePath, "genesis.dat");

            if (File.Exists(genesisPath))
            {
                var genesisBytes = File.ReadAllBytes(genesisPath);
                return Block<T>.Deserialize(genesisBytes);
            } 
            else
            {
                this.logger.Error("Genesis block doesn't exist.");
                return null;
            }
        }

        public void Stop()
        {
            Interlocked.Exchange(ref running, 2);

            this.peerBootstrapWorker?.Dispose();
            this.peerBootstrapWorker = null;

            this.cancellationToken?.Cancel();
            this.cancellationToken?.Dispose();
            this.cancellationToken = null;

            Task.Run(async () => await this.swarmServer.StopAsync()).ContinueWith(_ =>
            {
                this.storage?.Dispose();
            }).Wait(2000);

            logger.Information(string.Format("BlockChain {0} Manager stopped.", typeof(T).Name));
        }

        public void Dispose()
        {
            Stop();

            Interlocked.Exchange(ref running, 3);
        }
    }
}

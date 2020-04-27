using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.Net;
using NetMQ;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Store;
using Libplanet.Tx;
using Libplanet.Blocks;
using Serilog;
using FreeMarketOne.DataStructure;
using Libplanet.Tools;
using Libplanet.RocksDBStore;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using Newtonsoft.Json;
using FreeMarketOne.Extensions.Helpers;
using System.Text;
using FreeMarketOne.P2P;

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

        private readonly object basePollLock;
        private string blockChainFilePath { get; set; }
        private EndPoint endPoint { get; set; }

        private static readonly TimeSpan blockInterval = TimeSpan.FromSeconds(10);
        private PrivateKey privateKey { get; set; }
        private BlockChain<T> blocks;
        private RocksDBStore store;
        private Swarm<T> swarm;
        private ImmutableList<Peer> seedPeers;
        private IImmutableSet<Address> trustedPeers;

        private OnionSeedsManager onionSeedManager;
        private PeerBootstrapWorker<T> peerBootstrapWorker { get; set; }
        private ProofOfWorkWorker<T> proofOfWorkWorker { get; set; }

        /// <summary>
        /// BlockChain Manager which operate specified blockchain data
        /// </summary>
        /// <param name="serverLogger"></param>
        /// <param name="blockChainPath"></param>
        /// <param name="endPoint"></param>
        /// <param name="listHashCheckPoints"></param>
        public BlockChainManager(ILogger serverLogger, 
            string blockChainPath,
            string blockChainSecretPath,
            EndPoint endPoint,
            IOnionSeedsManager seedsManager,
            List<CheckPointMarketDataV1> listHashCheckPoints = null)
        {
            this.logger = serverLogger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, typeof(T).FullName);
            this.blockChainFilePath = blockChainPath;
            this.endPoint = endPoint;

            this.privateKey = GetSecret(blockChainSecretPath);
            this.store = new RocksDBStore(this.blockChainFilePath);

            this.onionSeedManager = (OnionSeedsManager)seedsManager;

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
            Interlocked.Exchange(ref running, 1);

            this.cancellationToken = new CancellationTokenSource();

            //REMOVE: temporary solution
            Block<T> genesis = CreateGenesisBlock();

            var host = this.endPoint.GetHostOrDefault();
            int? port = this.endPoint.GetPortOrDefault();

            var appProtocolVersion = default(AppProtocolVersion);
            var policy = new BlockPolicy<T>(
                    null,
                    blockInterval,
                    100000,
                    2048);

            this.blocks = new BlockChain<T>(
                policy,
                this.store,
                genesis
            );

            if (host != null)
            {
                this.swarm = new Swarm<T>(
                    this.blocks,
                    this.privateKey,
                    appProtocolVersion: appProtocolVersion,
                    host: host,
                    listenPort: port,
                    iceServers: null,
                    differentAppProtocolVersionEncountered: DifferentAppProtocolVersionEncountered,
                    trustedAppProtocolVersionSigners: null);

                var peers = GetPeers();
                this.seedPeers = peers.Where(peer => peer.PublicKey != this.privateKey.PublicKey).ToImmutableList();
                this.trustedPeers = seedPeers.Select(peer => peer.Address).ToImmutableHashSet();

                this.peerBootstrapWorker = new PeerBootstrapWorker<T>(
                    this.logger,
                    this.swarm, 
                    this.blocks,
                    this.seedPeers,
                    this.trustedPeers,
                    this.privateKey);

                this.proofOfWorkWorker = new ProofOfWorkWorker<T>(
                    this.logger,
                    this.swarm,
                    this.blocks,
                    this.privateKey.ToAddress(),
                    this.store,
                    null
                    );
            } 
            else
            {
                logger.Error(string.Format("No host information"));
                Stop();
            }

            //_miner = options.NoMiner ? null : CoMiner();

            //StartNullableCoroutine(_miner);

            return true;
        }

        private List<Peer> GetPeers()
        {
            var peers = new List<Peer>();

            while (!this.onionSeedManager.IsOnionSeedsManagerRunning())
            {
                Thread.Sleep(100);
            }

            foreach (var itemPeer in onionSeedManager.OnionSeedPeers)
            {
                //TOREMOVEAFTER TEST - HACK
                itemPeer.SecretKeyHex = new PrivateKey().PublicKey.ToAddress().ToHex();

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

        public void Stop()
        {
            Interlocked.Exchange(ref running, 2);

            this.peerBootstrapWorker?.Dispose();
            this.peerBootstrapWorker = null;

            this.cancellationToken?.Cancel();
            this.cancellationToken?.Dispose();
            this.cancellationToken = null;

            logger.Information(string.Format("BlockChain {0} Manager stopped.", typeof(T).Name));
        }

        public void Dispose()
        {
            Stop();

            Interlocked.Exchange(ref running, 3);
        }

        ///TEMPORARY
        public Block<T> CreateGenesisBlock(IEnumerable<T> actions = null)
        {
            List<T> actionsTest = new List<T>();
            var action = new T();
            var test1 = new CheckPointMarketDataV1();
            var test2 = new ReviewUserDataV1();

            action.AddBaseItem(test1);
            action.AddBaseItem(test2);
            actionsTest.Add(action);

            Block<T> genesis =
                BlockChain<T>.MakeGenesisBlock(actionsTest);
            // File.WriteAllBytes(this.blockChainFilePath + "/genesis.dat", genesis.Serialize());

            return genesis;
        }
    }
}

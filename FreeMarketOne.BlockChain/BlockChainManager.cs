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

namespace FreeMarketOne.BlockChain
{
    public class BlockChainManager<T> : IBlockChainManager, IDisposable where T : IAction
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

        private static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(10);

        /// <param name="serverLogger">Base server logger.</param>
        /// <param name="configuration">Base configuration.</param>
        public BlockChainManager(ILogger serverLogger, string blockChainPath, EndPoint endPoint)
        {
            this.logger = serverLogger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, typeof(T).FullName);
            this.blockChainFilePath = blockChainPath;
            this.endPoint = endPoint;

            logger.Information(string.Format("Initializing BlockChain Manager for : {0}",  typeof(T).Name));
        }

        public Block<BaseBlockChainAction> CreateGenesisBlock(IEnumerable<BaseBlockChainAction> actions = null)
        {
            List<BaseBlockChainAction> actionsTest = new List<BaseBlockChainAction>();
            var action = new BaseBlockChainAction();
            var test1 = new CheckPointMarketDataV1();
            var test2 = new ReviewUserDataV1();

            action.AddBaseItem(test1);
            action.AddBaseItem(test2);
            actionsTest.Add(action);

            Block<BaseBlockChainAction> genesis =
                BlockChain<BaseBlockChainAction>.MakeGenesisBlock(actionsTest);
            // File.WriteAllBytes(this.blockChainFilePath + "/genesis.dat", genesis.Serialize());

            return genesis;
        }

        private PrivateKey PrivateKey { get; set; }
        public Address Address { get; private set; }

        private BlockChain<BaseBlockChainAction> _blocks;

        private RocksDBStore _store;
        private Swarm<BaseBlockChainAction> _swarm;

        private ImmutableList<Peer> _seedPeers;

        private IImmutableSet<Address> _trustedPeers;

        private void Init(
    PrivateKey privateKey,
    string path,
    IEnumerable<Peer> peers,
    IEnumerable<IceServer> iceServers,
    string host,
    int? port,
    AppProtocolVersion appProtocolVersion,
    IEnumerable<PublicKey> trustedAppProtocolVersionSigners)
        {
            var policy = new BlockPolicy<BaseBlockChainAction>(
                null,
                BlockInterval,
                100000,
                2048);

            PrivateKey = privateKey;
            Address = privateKey.PublicKey.ToAddress();
            _store = new RocksDBStore(path);
            Block<BaseBlockChainAction> genesis = CreateGenesisBlock();

            _blocks = new BlockChain<BaseBlockChainAction>(
                policy,
                _store,
                genesis
            );

            if (!(host is null) || iceServers.Any())
            {
                _swarm = new Swarm<BaseBlockChainAction>(
                    _blocks,
                    privateKey,
                    appProtocolVersion: appProtocolVersion,
                    host: host,
                    listenPort: port,
                    iceServers: iceServers,
                    differentAppProtocolVersionEncountered: DifferentAppProtocolVersionEncountered,
                    trustedAppProtocolVersionSigners: trustedAppProtocolVersionSigners);

                _seedPeers = peers.Where(peer => peer.PublicKey != privateKey.PublicKey).ToImmutableList();
                _trustedPeers = _seedPeers.Select(peer => peer.Address).ToImmutableHashSet();
            }

            var s = _store.IterateBlockHashes();
            var s1 = s.FirstOrDefault();

            var readed = _store.GetBlock<BaseBlockChainAction>(s1);

            var xs = true;
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

            CreateGenesisBlock();

            Interlocked.Exchange(ref running, 0);
            cancellationToken = new CancellationTokenSource();

            var privateKey = new PrivateKey();
            var privateSignerKey = new PrivateKey();
            //var peers = options.Peers.Select(LoadPeer).ToImmutableList();
            //var iceServers = options.IceServers.Select(LoadIceServer).ToImmutableList();
            var host = this.endPoint.GetHostOrDefault();
            int? port = this.endPoint.GetPortOrDefault();
            var storagePath = this.blockChainFilePath;

            var appProtocolVersion =
    AppProtocolVersion.Sign(privateSignerKey, 123, (Bencodex.Types.Text)"foo");
            IEnumerable<PublicKey> trustedAppProtocolVersionSigners = null;

            //if (options.Logging)
            //{
            //    Log.Logger = new LoggerConfiguration()
            //        .MinimumLevel.Debug()
            //        .WriteTo.Console()
            //        .CreateLogger();
            //}

            Init(
                privateKey,
                storagePath,
                new List<Peer>(),
                new List<IceServer>(),
                host,
                port,
                appProtocolVersion,
                trustedAppProtocolVersionSigners
                );

            //_miner = options.NoMiner ? null : CoMiner();

            //StartSystemCoroutines();
            //StartNullableCoroutine(_miner);

            return true;
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

            cancellationToken?.Cancel();
            cancellationToken?.Dispose();
            cancellationToken = null;

            logger.Information(string.Format("BlockChain {0} Manager stopped.", typeof(T).Name));
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref running, 3);
            Stop();
        }
    }
}

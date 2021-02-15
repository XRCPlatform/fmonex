using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AsyncIO;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Renderers;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Net.Messages;
using Libplanet.Net.Protocols;
using Libplanet.Store;
using Libplanet.Tx;
using Nito.AsyncEx;
using Serilog;

namespace Libplanet.Net
{
    public partial class Swarm<T> : IDisposable
        where T : IAction, new()
    {
        private const int InitialBlockDownloadWindow = 100;
        private readonly PrivateKey _privateKey;
        private readonly AppProtocolVersion _appProtocolVersion;

        private readonly AsyncLock _blockSyncMutex;
        private readonly AsyncLock _runningMutex;

        private readonly ILogger _logger;
        private readonly IStore _store;

        private CancellationTokenSource _workerCancellationTokenSource;
        private CancellationToken _cancellationToken;

        private BlockHashDemand? _demandBlockHash;
        private ConcurrentDictionary<TxId, BoundPeer> _demandTxIds;
        private List<TxId> broadcastedTransactions = new List<TxId>();

        static Swarm()
        {
            if (!(Type.GetType("Mono.Runtime") is null))
            {
                ForceDotNet.Force();
            }
        }

        /// <summary>
        /// Creates a <see cref="Swarm{T}"/>.  This constructor in only itself does not start
        /// any communication with the network.
        /// </summary>
        /// <param name="blockChain">A blockchain to publicize on the network.</param>
        /// <param name="privateKey">A private key to sign messages.  The public part of
        /// this key become a part of its end address for being pointed by peers.</param>
        /// <param name="appProtocolVersion">An app protocol to comply.</param>
        /// <param name="workers">The number of background workers (i.e., threads).</param>
        /// <param name="host">A hostname to be a part of a public endpoint, that peers use when
        /// they connect to this node.  Note that this is not a hostname to listen to;
        /// <see cref="Swarm{T}"/> always listens to 0.0.0.0 &amp; ::/0.</param>
        /// <param name="listenPort">A port number to listen to.</param>
        /// <param name="iceServers">
        /// <a href="https://en.wikipedia.org/wiki/Interactive_Connectivity_Establishment">ICE</a>
        /// servers to use for TURN/STUN.  Purposes to traverse NAT.</param>
        /// <param name="differentAppProtocolVersionEncountered">A delegate called back when a peer
        /// with one different from <paramref name="appProtocolVersion"/>, and their version is
        /// signed by a trusted party (i.e., <paramref name="trustedAppProtocolVersionSigners"/>).
        /// If this callback returns <c>false</c> an encountered peer is ignored.  If this callback
        /// is omitted all peers with different <see cref="AppProtocolVersion"/>s are ignored.
        /// </param>
        /// <param name="trustedAppProtocolVersionSigners"><see cref="PublicKey"/>s of parties
        /// to trust <see cref="AppProtocolVersion"/>s they signed.  To trust any party, pass
        /// <c>null</c>, which is default.</param>
        /// <param name="options">Options for <see cref="Swarm{T}"/>.</param>
        public Swarm(
            BlockChain<T> blockChain,
            PrivateKey privateKey,
            AppProtocolVersion appProtocolVersion,
            int workers = 5,
            string host = null,
            int? listenPort = null,
            IEnumerable<IceServer> iceServers = null,
            DifferentAppProtocolVersionEncountered differentAppProtocolVersionEncountered = null,
            IEnumerable<PublicKey> trustedAppProtocolVersionSigners = null,
            SwarmOptions options = null)
            : this(
                blockChain,
                privateKey,
                appProtocolVersion,
                null,
                null,
                workers,
                host,
                listenPort,
                null,
                iceServers,
                differentAppProtocolVersionEncountered,
                trustedAppProtocolVersionSigners,
                options)
        {
        }

        internal Swarm(
            BlockChain<T> blockChain,
            PrivateKey privateKey,
            AppProtocolVersion appProtocolVersion,
            int? tableSize,
            int? bucketSize,
            int workers = 5,
            string host = null,
            int? listenPort = null,
            DateTimeOffset? createdAt = null,
            IEnumerable<IceServer> iceServers = null,
            DifferentAppProtocolVersionEncountered differentAppProtocolVersionEncountered = null,
            IEnumerable<PublicKey> trustedAppProtocolVersionSigners = null,
            SwarmOptions options = null)
        {
            BlockChain = blockChain ?? throw new ArgumentNullException(nameof(blockChain));
            _store = BlockChain.Store;
            _privateKey = privateKey ?? throw new ArgumentNullException(nameof(privateKey));
            LastSeenTimestamps =
                new ConcurrentDictionary<Peer, DateTimeOffset>();

            DateTimeOffset now = createdAt.GetValueOrDefault(
                DateTimeOffset.UtcNow);
            TxReceived = new AsyncAutoResetEvent();
            BlockHeaderReceived = new AsyncAutoResetEvent();
            BlockAppended = new AsyncAutoResetEvent();
            BlockReceived = new AsyncAutoResetEvent();

            _blockSyncMutex = new AsyncLock();
            _runningMutex = new AsyncLock();

            _appProtocolVersion = appProtocolVersion;
            TrustedAppProtocolVersionSigners =
                trustedAppProtocolVersionSigners?.ToImmutableHashSet();

            string loggerId = _privateKey.ToAddress().ToHex();
            _logger = Log.ForContext<Swarm<T>>()
                .ForContext("SwarmId", loggerId);

            Transport = new NetMQTransport(
                _privateKey,
                _appProtocolVersion,
                TrustedAppProtocolVersionSigners,
                tableSize,
                bucketSize,
                workers,
                host,
                listenPort,
                iceServers,
                differentAppProtocolVersionEncountered,
                ProcessMessageHandler,
                _logger,
                options.Socks5Proxy
            );

            Options = options ?? new SwarmOptions();
        }

        ~Swarm()
        {
            // FIXME If possible, we should stop Swarm appropriately here.
            if (Running)
            {
                _logger.Warning(
                    "Swarm is scheduled to destruct, but NetMQTransport progress is still running."
                );
            }
        }

        public bool Running => Transport is NetMQTransport p && p.Running;

        public DnsEndPoint EndPoint => AsPeer is BoundPeer boundPeer ? boundPeer.EndPoint : null;

        public Address Address => _privateKey.ToAddress();

        public Peer AsPeer => Transport.AsPeer;

        /// <summary>
        /// The last time when any message was arrived.
        /// It can be <c>null</c> if no message has been arrived yet.
        /// </summary>
        public DateTimeOffset? LastMessageTimestamp =>
            Running ? Transport.LastMessageTimestamp : (DateTimeOffset?)null;

        public IDictionary<Peer, DateTimeOffset> LastSeenTimestamps
        {
            get;
            private set;
        }

        public IEnumerable<BoundPeer> Peers => Transport.Peers;

        /// <summary>
        /// The <see cref="BlockChain{T}"/> instance this <see cref="Swarm{T}"/> instance
        /// synchronizes with.
        /// </summary>
        public BlockChain<T> BlockChain { get; private set; }

        public IImmutableSet<PublicKey> TrustedAppProtocolVersionSigners { get; }

        public AppProtocolVersion AppProtocolVersion => _appProtocolVersion;

        internal ITransport Transport { get; private set; }

        internal IProtocol Protocol => (Transport as NetMQTransport)?.Protocol;

        internal AsyncAutoResetEvent TxReceived { get; }

        internal AsyncAutoResetEvent BlockHeaderReceived { get; }

        internal AsyncAutoResetEvent BlockReceived { get; }

        // FIXME: Should have a unit test.
        internal AsyncAutoResetEvent BlockAppended { get; }

        // FIXME: We need some sort of configuration method for it.
        internal int FindNextHashesChunkSize { get; set; } = 500;

        internal int FindNextStatesChunkSize { get; set; } = 2500;

        internal AsyncAutoResetEvent PreloadStarted { get; } = new AsyncAutoResetEvent();

        internal AsyncAutoResetEvent BlockDownloadStarted { get; } = new AsyncAutoResetEvent();

        internal AsyncAutoResetEvent FillBlocksAsyncStarted { get; } = new AsyncAutoResetEvent();

        internal AsyncAutoResetEvent FillBlocksAsyncFailed { get; } = new AsyncAutoResetEvent();

        internal SwarmOptions Options { get; }

        /// <summary>
        /// Waits until this <see cref="Swarm{T}"/> instance gets started to run.
        /// </summary>
        /// <returns>A <see cref="Task"/> completed when <see cref="NetMQTransport.Running"/>
        /// property becomes <c>true</c>.</returns>
        public Task WaitForRunningAsync() => (Transport as NetMQTransport)?.WaitForRunningAsync();

        public void Dispose()
        {
            _workerCancellationTokenSource?.Cancel();
            Transport.Dispose();
        }

        public async Task StopAsync(
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            await StopAsync(TimeSpan.FromSeconds(1), cancellationToken);
        }

        public async Task StopAsync(
            TimeSpan waitFor,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            _logger.Debug($"Stopping {nameof(Swarm<T>)}...");
            using (await _runningMutex.LockAsync())
            {
                await Transport.StopAsync(waitFor, cancellationToken);
            }

            _logger.Debug($"{nameof(Swarm<T>)} stopped.");
        }

        /// <summary>
        /// Starts to periodically synchronize the <see cref="BlockChain"/>.
        /// </summary>
        /// <param name="millisecondsDialTimeout">
        /// When the <see cref="Swarm{T}"/> tries to dial each peer in <see cref="Peers"/>,
        /// the dial-up is cancelled after this timeout, and it tries another peer.
        /// If <c>null</c> is given it never gives up dial-ups.
        /// </param>
        /// <param name="millisecondsBroadcastTxInterval">
        /// The time period of exchange of staged transactions.
        /// </param>
        /// <param name="trustedStateValidators">
        /// If any peer in this set is reachable, <see cref="Swarm{T}"/> receives the latest
        /// states of the major blockchain from that trusted peer, which is also calculated by
        /// that peer, instead of autonomously calculating the states from scratch.
        /// Note that this option is intended to be exposed to end users through a feasible user
        /// interface so that they can decide whom to trust for themselves.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token used to propagate notification that this
        /// operation should be canceled.
        /// </param>
        /// <returns>An awaitable task without value.</returns>
        /// <exception cref="SwarmException">Thrown when this <see cref="Swarm{T}"/> instance is
        /// already <see cref="Running"/>.</exception>
        /// <remarks>If the <see cref="BlockChain"/> has no blocks at all or there are long behind
        /// blocks to caught in the network this method could lead to unexpected behaviors, because
        /// this tries to render <em>all</em> actions in the behind blocks so that there are
        /// a lot of calls to methods of <see cref="BlockChain{T}.Renderers"/> in a short
        /// period of time.  This can lead a game startup slow.  If you want to omit rendering of
        /// these actions in the behind blocks use <see cref=
        /// "PreloadAsync(TimeSpan?, IProgress{PreloadState}, IImmutableSet{Address},
        /// CancellationToken)"
        /// /> method too.</remarks>
        public async Task StartAsync(
            int millisecondsDialTimeout = 15000,//240000
            int millisecondsBroadcastTxInterval = 5000,
            IImmutableSet<Address> trustedStateValidators = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await StartAsync(
                TimeSpan.FromMilliseconds(millisecondsDialTimeout),
                TimeSpan.FromMilliseconds(millisecondsBroadcastTxInterval),
                trustedStateValidators,
                cancellationToken
            );
        }

        /// <summary>
        /// Starts to periodically synchronize the <see cref="BlockChain"/>.
        /// </summary>
        /// <param name="dialTimeout">
        /// When the <see cref="Swarm{T}"/> tries to dial each peer in <see cref="Peers"/>,
        /// the dial-up is cancelled after this timeout, and it tries another peer.
        /// If <c>null</c> is given it never gives up dial-ups.
        /// </param>
        /// <param name="broadcastTxInterval">The time period of exchange of staged transactions.
        /// </param>
        /// <param name="trustedStateValidators">
        /// If any peer in this set is reachable, <see cref="Swarm{T}"/> receives the latest
        /// states of the major blockchain from that trusted peer, which is also calculated by
        /// that peer, instead of autonomously calculating the states from scratch.
        /// Note that this option is intended to be exposed to end users through a feasible user
        /// interface so that they can decide whom to trust for themselves.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token used to propagate notification that this
        /// operation should be canceled.
        /// </param>
        /// <returns>An awaitable task without value.</returns>
        /// <exception cref="SwarmException">Thrown when this <see cref="Swarm{T}"/> instance is
        /// already <see cref="Running"/>.</exception>
        /// <remarks>If the <see cref="BlockChain"/> has no blocks at all or there are long behind
        /// blocks to caught in the network this method could lead to unexpected behaviors, because
        /// this tries to render <em>all</em> actions in the behind blocks so that there are
        /// a lot of calls to methods of <see cref="BlockChain{T}.Renderers"/> in a short
        /// period of time.  This can lead a game startup slow.  If you want to omit rendering of
        /// these actions in the behind blocks use <see cref=
        /// "PreloadAsync(TimeSpan?, IProgress{PreloadState}, IImmutableSet{Address},
        /// CancellationToken)"
        /// /> method too.</remarks>
        public async Task StartAsync(
            TimeSpan dialTimeout,
            TimeSpan broadcastTxInterval,
            IImmutableSet<Address> trustedStateValidators = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var tasks = new List<Task>();
            trustedStateValidators ??= ImmutableHashSet<Address>.Empty;
            _workerCancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
                    _workerCancellationTokenSource.Token, cancellationToken
                ).Token;
            _demandBlockHash = null;
            _demandTxIds = new ConcurrentDictionary<TxId, BoundPeer>();
            await Transport.StartAsync(_cancellationToken);

            _logger.Debug("Starting swarm...");
            _logger.Debug("Peer information : {Peer}", AsPeer);

            try
            {
                tasks.Add(Transport.RunAsync(_cancellationToken));
                tasks.Add(BroadcastTxAsync(broadcastTxInterval, _cancellationToken));
                tasks.Add(ProcessFillBlocks(TimeSpan.FromMinutes(10), trustedStateValidators, _cancellationToken));
                tasks.Add(ProcessFillTxs(_cancellationToken));
                _logger.Debug("Swarm started.");

                await await Task.WhenAny(tasks);
            }
            catch (OperationCanceledException e)
            {
                _logger.Warning(e, $"{nameof(StartAsync)}() is canceled.");
                throw;
            }
            catch (Exception e)
            {
                _logger.Error(
                    e,
                    $"An unexpected exception occurred during {nameof(StartAsync)}(): {e}"
                );
                throw;
            }
        }

        public async Task BootstrapAsync(
           IEnumerable<Peer> seedPeers,
           double pingSeedTimeout,
           double findPeerTimeout,
           int depth = Kademlia.MaxDepth,
           CancellationToken cancellationToken = default(CancellationToken))
        {
            await BootstrapAsync(
                seedPeers,
                TimeSpan.FromMilliseconds(pingSeedTimeout),
                TimeSpan.FromMilliseconds(findPeerTimeout),
                depth,
                cancellationToken);
        }

        /// <summary>
        /// Join to the peer-to-peer network using seed peers.
        /// </summary>
        /// <param name="seedPeers">List of seed peers.</param>
        /// <param name="pingSeedTimeout">Timeout for connecting to seed peers.</param>
        /// <param name="findNeighborsTimeout">Timeout for requesting neighbors.</param>
        /// <param name="depth">Depth to find neighbors of current <see cref="Peer"/>
        /// from seed peers.</param>
        /// <param name="cancellationToken">A cancellation token used to propagate notification
        /// that this operation should be canceled.</param>
        /// <returns>An awaitable task without value.</returns>
        /// <exception cref="SwarmException">Thrown when this <see cref="Swarm{T}"/> instance is
        /// not <see cref="Running"/>.</exception>
        public async Task BootstrapAsync(
            IEnumerable<Peer> seedPeers,
            TimeSpan? pingSeedTimeout,
            TimeSpan? findNeighborsTimeout,
            int depth = Kademlia.MaxDepth,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (seedPeers is null)
            {
                throw new ArgumentNullException(nameof(seedPeers));
            }

            IEnumerable<BoundPeer> peers = seedPeers.OfType<BoundPeer>();
            
            var peersExceptMe = peers.Where(peer => !peer.Address.Equals(AsPeer.Address) && peer.EndPoint.Host != "127.0.0.1" && peer.EndPoint.Host != ((BoundPeer)AsPeer).EndPoint.Host );

            await Transport.BootstrapAsync(
                peersExceptMe,
                pingSeedTimeout,
                findNeighborsTimeout,
                depth,
                cancellationToken);
        }

        public void BroadcastBlock(Block<T> block)
        {
            BroadcastBlock(null, block);
        }

        public void BroadcastTxs(IEnumerable<Transaction<T>> txs)
        {
            BroadcastTxs(null, txs);
        }

        public string TraceTable()
        {
            return Transport is null ? string.Empty : (Transport as NetMQTransport)?.Trace();
        }

        /// <summary>
        /// Gets the <see cref="PeerChainState"/> of the connected <see cref="Peers"/>.
        /// </summary>
        /// <param name="dialTimeout">
        /// When the <see cref="Swarm{T}"/> tries to dial each peer in <see cref="Peers"/>,
        /// the dial-up is cancelled after this timeout, and it tries another peer.
        /// If <c>null</c> is given it never gives up dial-ups.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token used to propagate notification that this
        /// operation should be canceled.
        /// </param>
        /// <returns><see cref="PeerChainState"/> of the connected <see cref="Peers"/>.</returns>
        public async Task<IEnumerable<PeerChainState>> GetPeerChainStateAsync(
            TimeSpan? dialTimeout,
            CancellationToken cancellationToken
        )
        {
            // FIXME: It would be better if it returns IAsyncEnumerable<PeerChainState> instead.
            return (await DialToExistingPeers(dialTimeout, cancellationToken))
                .Select(pp =>
                    new PeerChainState(
                        pp.Item1,
                        pp.Item2?.TipIndex ?? -1,
                        pp.Item2?.TotalDifficulty ?? -1));
        }

        /// <summary>
        /// Preemptively downloads blocks from registered <see cref="Peer"/>s.
        /// </summary>
        /// <param name="dialTimeout">
        /// When the <see cref="Swarm{T}"/> tries to dial each peer in <see cref="Peers"/>,
        /// the dial-up is cancelled after this timeout, and it tries another peer.
        /// If <c>null</c> is given it never gives up dial-ups.
        /// </param>
        /// <param name="progress">
        /// An instance that receives progress updates for block downloads.
        /// </param>
        /// <param name="trustedStateValidators">
        /// If any peer in this set is reachable, <see cref="Swarm{T}"/> receives the latest
        /// states of the major blockchain from that trusted peer, which is also calculated by
        /// that peer, instead of autonomously calculating the states from scratch.
        /// Note that this option is intended to be exposed to end users through a feasible user
        /// interface so that they can decide whom to trust for themselves.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token used to propagate notification that this
        /// operation should be canceled.
        /// </param>
        /// <returns>
        /// A task without value.
        /// You only can <c>await</c> until the method is completed.
        /// </returns>
        /// <remarks>This does not render downloaded <see cref="IAction"/>s, but fills states only.
        /// </remarks>
        /// <exception cref="AggregateException">Thrown when the given the block downloading is
        /// failed.</exception>
        [SuppressMessage(
            "Microsoft.StyleCop.CSharp.ReadabilityRules",
            "MEN003",
            Justification = "Many lines are just for writing logs.")]
        public async Task PreloadAsync(
            TimeSpan? dialTimeout = null,
            IProgress<PreloadState> progress = null,
            IImmutableSet<Address> trustedStateValidators = null,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            cancellationToken.Register(() =>
                _logger.Information("Preloading is requested to be cancelled.")
            );

            trustedStateValidators ??= ImmutableHashSet<Address>.Empty;

            Block<T> initialTip = BlockChain.Tip;
            BlockLocator initialLocator = BlockChain.GetBlockLocator();
            _logger.Debug(
                "The tip before preloading begins: #{TipIndex} {TipHash}",
                BlockChain.Tip.Index,
                BlockChain.Tip.Hash
            );

            // As preloading takes long, the blockchain data can corrupt if a program suddenly
            // terminates during preloading is going on.  In order to make preloading done
            // all or nothing (i.e., atomic), we first fork the chain and stack up preloaded data
            // upon that forked workspace, and then if preloading ends replace the existing
            // blockchain with it.
            // Note that it does not pass any renderers here so that they render nothing
            // (because the workspace chain is for underlying).
            BlockChain<T> workspace = initialTip is Block<T> tip
                ? BlockChain.Fork(tip.Hash, inheritRenderers: false)
                : new BlockChain<T>(
                    BlockChain.Policy,
                    _store,
                    _store as IStateStore,
                    Guid.NewGuid(),
                    BlockChain.Genesis,
                    Enumerable.Empty<IRenderer<T>>());
            Guid wId = workspace.Id;
            IStore wStore = workspace.Store;
            var chainIds = new HashSet<Guid>
            {
                workspace.Id,
            };

            var complete = false;

            try
            {
                FillBlocksAsyncStarted.Set();

                var blockCompletion = new BlockCompletion<BoundPeer, T>(
                    completionPredicate: workspace.ContainsBlock,
                    window: InitialBlockDownloadWindow
                );

                long totalBlocksToDownload = 0L;
                long receivedBlockCount = 0L;
                short lapCount = 0;
                Block<T> tipCandidate = initialTip;

                Block<T> tempTip = tipCandidate;
                Block<T> branchpoint = null;

                long? receivedStateHeight = null;
                long height = 0;

                var retryCount = 0;
                const int maxRetryCount = 1;

                while (retryCount <= maxRetryCount)
                {
                    var peersWithHeight = await GetPeersWithHeight(
                        initialTip, dialTimeout, cancellationToken);

                    if (!peersWithHeight.Any())
                    {
                        _logger.Information("There is no appropriate peer for preloading.");
                        return;
                    }

                    PreloadStarted.Set();

                    // From the second lap, as it's catching up the latest blocks made
                    // in very short time, do not report the progress.  Even if it's reported,
                    // it can be very confusing, because it looks like BlockHashDownloadState
                    // recurring after later phases like BlockDownloadState.
                    IProgress<PreloadState> demandProgress = lapCount++ < 1 ? progress : null;

                    var demandBlockHashes = GetDemandBlockHashes(
                        workspace,
                        peersWithHeight,
                        demandProgress,
                        cancellationToken
                    ).WithCancellation(cancellationToken);

                    await foreach (var pair in demandBlockHashes)
                    {
                        (long index, HashDigest<SHA256> hash) = pair;
                        cancellationToken.ThrowIfCancellationRequested();

                        if (index == 0 && !hash.Equals(workspace.Genesis.Hash))
                        {
                            // FIXME: This behavior can unexpectedly terminate the swarm
                            // (and the game app) if it encounters a peer having a different
                            // blockchain, and therefore can be exploited to remotely shut
                            // down other nodes as well.
                            // Since the intention of this behavior is to prevent mistakes
                            // to try to connect incorrect seeds (by a user),
                            // this behavior should be limited for only seed peers.
                            // FIXME: ChainStatus message became to contain hash value of
                            // the genesis block, so this exception will not be happened.
                            var msg =
                                $"Since the genesis block is fixed to {workspace.Genesis} " +
                                "protocol-wise, the blockchain which does not share " +
                                "any mutual block is not acceptable.";
                            var e = new InvalidGenesisBlockException(
                                hash,
                                workspace.Genesis.Hash,
                                msg);
                            throw new AggregateException(msg, e);
                        }

                        _logger.Verbose(
                            "Enqueue #{BlockIndex} {BlockHash} to demands queue...",
                            index,
                            hash
                        );
                        if (blockCompletion.Demand(hash))
                        {
                            totalBlocksToDownload++;
                        }
                    }

                    IAsyncEnumerable<Tuple<Block<T>, BoundPeer>> completedBlocks =
                        blockCompletion.Complete(
                            peers: peersWithHeight.Select(pair => pair.Item1).ToList(),
                            blockFetcher: GetBlocksAsync,
                            cancellationToken: cancellationToken
                        );

                    BlockDownloadStarted.Set();

                    var blockDownloadCts = CancellationTokenSource.CreateLinkedTokenSource(
                        new CancellationTokenSource(Options.BlockDownloadTimeout).Token,
                        cancellationToken);

                    await foreach (
                        var pair in completedBlocks.WithCancellation(blockDownloadCts.Token))
                    {
                        pair.Deconstruct(out Block<T> block, out BoundPeer sourcePeer);
                        _logger.Verbose(
                            "Got #{BlockIndex} {BlockHash} from {Pair}.",
                            block.Index,
                            block.Hash,
                            sourcePeer
                        );
                        cancellationToken.ThrowIfCancellationRequested();

                        if (block.Index == 0 && !block.Hash.Equals(workspace.Genesis.Hash))
                        {
                            // FIXME: This behavior can unexpectedly terminate the swarm
                            // (and the game app) if it encounters a peer having a different
                            // blockchain, and therefore can be exploited to remotely shut
                            // down other nodes as well.
                            // Since the intention of this behavior is to prevent mistakes
                            // to try to connect incorrect seeds (by a user),
                            // this behavior should be limited for only seed peers.
                            var msg =
                                $"Since the genesis block is fixed to {workspace.Genesis} " +
                                "protocol-wise, the blockchain which does not share " +
                                "any mutual block is not acceptable.";

                            // Although it's actually not aggregated, but to be consistent with
                            // above code throwing InvalidGenesisBlockException, makes this
                            // to wrap an exception with AggregateException... Not sure if
                            // it show be wrapped from the very beginning.
                            throw new AggregateException(
                                msg,
                                new InvalidGenesisBlockException(
                                    block.Hash,
                                    workspace.Genesis.Hash,
                                    msg
                                )
                            );
                        }

                        _logger.Verbose(
                            "Add a block #{BlockIndex} {BlockHash}...",
                            block.Index,
                            block.Hash
                        );
                        wStore.PutBlock(block);
                        if (tempTip is null || block.Index > tempTip.Index)
                        {
                            tempTip = block;
                        }

                        receivedBlockCount++;
                        progress?.Report(new BlockDownloadState
                        {
                            TotalBlockCount = Math.Max(
                                totalBlocksToDownload,
                                receivedBlockCount),
                            ReceivedBlockCount = receivedBlockCount,
                            ReceivedBlockHash = block.Hash,
                            SourcePeer = sourcePeer,
                        });
                        _logger.Debug(
                            "Appended a block #{BlockIndex} {BlockHash} " +
                            "to the workspace chain.",
                            block.Index,
                            block.Hash
                        );
                    }

                    tipCandidate = tempTip;

                    if (tipCandidate is null)
                    {
                        // If there is no blocks in the network (or no consensus at least)
                        // it doesn't need to receive states from other peers at all.
                        return;
                    }

                    var deltaBlocks = new LinkedList<Block<T>>();
                    while (true)
                    {
                        Block<T> blockToAdd;
                        if (deltaBlocks.First is LinkedListNode<Block<T>> node)
                        {
                            Block<T> b = node.Value;
                            if (b.PreviousHash is HashDigest<SHA256> p)
                            {
                                blockToAdd = wStore.GetBlock<T>(p);
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            blockToAdd = tipCandidate;
                        }

                        if (!(initialTip is null) &&
                            blockToAdd.Index <= initialTip.Index &&
                            wStore.IndexBlockHash(wId, blockToAdd.Index).Equals(blockToAdd.Hash))
                        {
                            break;
                        }

                        deltaBlocks.AddFirst(blockToAdd);
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    if (deltaBlocks.First is LinkedListNode<Block<T>> deltaBottom)
                    {
                        Block<T> bottomBlock = deltaBottom.Value;
                        if (bottomBlock.PreviousHash is HashDigest<SHA256> bp)
                        {
                            branchpoint = workspace[bp];
                            workspace = workspace.Fork(bp);
                            chainIds.Add(workspace.Id);
                            try
                            {
                                long verifiedBlockCount = 0;
                                foreach (Block<T> deltaBlock in deltaBlocks)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    workspace.Append(
                                        deltaBlock,
                                        DateTimeOffset.UtcNow,
                                        evaluateActions: false,
                                        renderBlocks: false,
                                        renderActions: false
                                    );
                                    progress?.Report(new BlockVerificationState
                                    {
                                        TotalBlockCount = deltaBlocks.Count,
                                        VerifiedBlockCount = ++verifiedBlockCount,
                                        VerifiedBlockHash = deltaBlock.Hash,
                                    });
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.Error(
                                    e,
                                    "An exception occurred during appending blocks: {Exception}",
                                    e
                                );
                                throw;
                            }

                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        else
                        {
                            Block<T> first = deltaBlocks.First.Value, last = deltaBlocks.Last.Value;
                            HashDigest<SHA256> g = wStore.IndexBlockHash(wId, 0L).Value;
                            throw new SwarmException(
                                $"Downloaded blocks (#{first.Index} {first.Hash}\u2013" +
                                $"#{last.Index} {last.Hash}) are incompatible with the existing " +
                                $"chain (#0 {g}\u2013#{initialTip.Index} {initialTip.Hash})."
                            );
                        }
                    }

                    height = workspace.Tip.Index;

                    IEnumerable<(BoundPeer, HashDigest<SHA256> Hash)> trustedPeersWithTip =
                        peersWithHeight.Where(pair =>
                                trustedStateValidators.Contains(pair.Item1.Address) &&
                                pair.Item2 <= height)
                            .OrderByDescending(pair => pair.Item2)
                            .Select(pair => (pair.Item1, workspace[pair.Item2].Hash));

                    // FIXME: It is not guaranteed that states will be reported in order.
                    // see issue #436, #430
                    try
                    {
                        if (BlockChain.StateStore is IBlockStatesStore)
                        {
                            receivedStateHeight = await SyncRecentStatesFromTrustedPeersAsync(
                                workspace,
                                progress,
                                trustedPeersWithTip.ToImmutableList(),
                                initialLocator,
                                cancellationToken
                            );
                        }

                        break;
                    }
                    catch (InvalidStateTargetException e)
                    {
                        Log.Error(e, e.ToString());
                        retryCount++;
                    }
                }

                if (receivedStateHeight is null || receivedStateHeight < height)
                {
                    PreloadExecuteActions(
                        workspace,
                        branchpoint,
                        receivedStateHeight,
                        progress,
                        cancellationToken);
                }

                complete = true;
            }
            finally
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Information($"{nameof(PreloadAsync)}() is canceled.");
                }

                if (!complete
                    || workspace.Tip == BlockChain.Tip
                    || cancellationToken.IsCancellationRequested)
                {
                    _logger.Debug(
                        "Preloading is aborted; delete the temporary working chain ({0}: {1}), " +
                        "and make the existing chain ({2}: {3}) remains.",
                        wId,
                        workspace.Tip,
                        BlockChain.Id,
                        BlockChain.Tip
                    );
                }
                else
                {
                    _logger.Debug(
                        "Preloading finished; replace the existing chain ({0}: {1}) with " +
                        "the working chain ({2}: {3}).",
                        BlockChain.Id,
                        BlockChain.Tip,
                        wId,
                        workspace.Tip
                    );
                    BlockChain.Swap(workspace, render: false);
                }

                foreach (Guid chainId in chainIds)
                {
                    if (!chainId.Equals(BlockChain.Id))
                    {
                        _logger.Verbose("Delete an unused chain: {ChainId}", chainId);
                        wStore.DeleteChainId(chainId);
                    }
                }

                _logger.Verbose("Remaining chains: {@ChainIds}", wStore.ListChainIds());
            }
        }

        /// <summary>
        /// Use <see cref="FindNeighbors"/> messages to to find a <see cref="BoundPeer"/> with
        /// <see cref="Address"/> of <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The <see cref="Address"/> to find.</param>
        /// <param name="depth">Target depth of recursive operation. If -1 is given,
        /// will recursive until the closest <see cref="BoundPeer"/> to the
        /// <paramref name="target"/> is found.</param>
        /// <param name="timeout">
        /// <see cref="TimeSpan"/> for waiting reply of <see cref="FindNeighbors"/>.
        /// If <c>null</c> is given, <see cref="TimeoutException"/> will not be thrown.
        /// </param>
        /// <param name="cancellationToken">A cancellation token used to propagate notification
        /// that this operation should be canceled.</param>
        /// <returns>
        /// A <see cref="BoundPeer"/> with <see cref="Address"/> of <paramref name="target"/>.
        /// Returns <c>null</c> if the peer with address does not exist.
        /// </returns>
        public async Task<BoundPeer> FindSpecificPeerAsync(
            Address target,
            int depth = 3,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            NetMQTransport netMQTransport = (NetMQTransport)Transport;
            return await netMQTransport.FindSpecificPeerAsync(
                target,
                depth,
                timeout,
                cancellationToken);
        }

        /// <summary>
        /// Validates all <see cref="Peer"/>s in the routing table by sending a simple message.
        /// </summary>
        /// <param name="timeout">Timeout for this operation. If it is set to <c>null</c>,
        /// wait infinitely until the requested operation is finished.</param>
        /// <param name="cancellationToken">A cancellation token used to propagate notification
        /// that this operation should be canceled.</param>
        /// <returns>An awaitable task without value.</returns>
        public async Task CheckAllPeersAsync(
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, _cancellationToken).Token;

            var netMQTransport = (NetMQTransport)Transport;
            await netMQTransport.CheckAllPeersAsync(cancellationToken, timeout);
        }

        public async Task AddPeersAsync(
            IEnumerable<Peer> peers,
            TimeSpan? timeout,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (Transport is null)
            {
                throw new ArgumentNullException(nameof(Transport));
            }

            if (cancellationToken == default(CancellationToken))
            {
                cancellationToken = _cancellationToken;
            }

            var peersExceptMe = peers.Where(peer => !peer.Address.Equals(AsPeer.Address));
            await ((NetMQTransport)Transport).AddPeersAsync(peersExceptMe, timeout, cancellationToken);
        }

        // FIXME: This would be better if it's merged with GetDemandBlockHashes
        internal async IAsyncEnumerable<Tuple<long, HashDigest<SHA256>>> GetBlockHashes(
            BoundPeer peer,
            BlockLocator locator,
            HashDigest<SHA256>? stop,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            var request = new GetBlockHashes(locator, stop);

            Message parsedMessage = await Transport.SendMessageWithReplyAsync(
                peer,
                request,
                TimeSpan.FromMinutes(10),
                cancellationToken: cancellationToken
            );

            if (parsedMessage is BlockHashes blockHashes)
            {
                if (blockHashes.StartIndex is long idx)
                {
                    HashDigest<SHA256>[] hashes = blockHashes.Hashes.ToArray();
                    _logger.Debug(
                        $"Received a {nameof(BlockHashes)} message with an offset index " +
                        "{OffsetIndex} (total {HashesLength} hashes).",
                        idx,
                        hashes.LongLength
                    );
                    foreach (HashDigest<SHA256> hash in hashes)
                    {
                        yield return new Tuple<long, HashDigest<SHA256>>(idx, hash);
                        idx++;
                    }
                }
                else
                {
                    _logger.Debug(
                        $"Received a {nameof(BlockHashes)} message, but it has zero hashes."
                    );
                }

                yield break;
            }

            string errorMessage =
                $"The response of {nameof(GetBlockHashes)} is expected to be " +
                $"{nameof(BlockHashes)}, not {parsedMessage.GetType().Name}: {parsedMessage}";
            throw new InvalidMessageException(errorMessage, parsedMessage);
        }

        internal async IAsyncEnumerable<Block<T>> GetBlocksAsync(
            BoundPeer peer,
            IEnumerable<HashDigest<SHA256>> blockHashes,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            _logger.Information(
                "Try to download blocks from {EndPoint}@{Address}...",
                peer.EndPoint,
                peer.Address.ToHex()
            );

            var blockHashesAsArray = blockHashes as HashDigest<SHA256>[] ?? blockHashes.ToArray();
            var request = new GetBlocks(blockHashesAsArray);
            int hashCount = blockHashesAsArray.Count();

            if (hashCount < 1)
            {
                yield break;
            }

            TimeSpan blockRecvTimeout = Options.BlockRecvTimeout
                                        + TimeSpan.FromSeconds(hashCount * 30);
            if (blockRecvTimeout > Options.MaxTimeout)
            {
                blockRecvTimeout = Options.MaxTimeout;
            }

            IEnumerable<Message> replies = await Transport.SendMessageWithReplyAsync(
                peer,
                request,
                blockRecvTimeout,
                ((hashCount - 1) / request.ChunkSize) + 1,
                cancellationToken
            );

            foreach (Message message in replies)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (message is Messages.Blocks blockMessage)
                {
                    IList<byte[]> payloads = blockMessage.Payloads;
                    _logger.Debug(
                        "Received {Number} blocks from {Peer}.",
                        payloads.Count,
                        message.Remote);
                    foreach (byte[] payload in payloads)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Block<T> block = new Block<T>().Deserialize(payload);

                        yield return block;
                    }
                }
                else
                {
                    string errorMessage =
                        $"Expected a {nameof(Blocks)} message as a response of " +
                        $"the {nameof(GetBlocks)} message, but got a {message.GetType().Name} " +
                        $"message instead: {message}";
                    throw new InvalidMessageException(errorMessage, message);
                }
            }

            _logger.Information(
                "Downloaded blocks from {EndPoint}@{Address}.",
                peer.EndPoint,
                peer.Address.ToHex()
            );
        }

        internal async IAsyncEnumerable<Transaction<T>> GetTxsAsync(
            BoundPeer peer,
            IEnumerable<TxId> txIds,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var txIdsAsArray = txIds as TxId[] ?? txIds.ToArray();
            var request = new GetTxs(txIdsAsArray);
            int txCount = txIdsAsArray.Count();

            _logger.Debug("Required tx count: {Count}.", txCount);

            var txRecvTimeout = Options.TxRecvTimeout + TimeSpan.FromSeconds(txCount * 30);
            if (txRecvTimeout > Options.MaxTimeout)
            {
                txRecvTimeout = Options.MaxTimeout;
            }

            IEnumerable<Message> replies = await Transport.SendMessageWithReplyAsync(
                peer,
                request,
                txRecvTimeout,
                txCount,
                cancellationToken
            );

            foreach (Message message in replies)
            {
                if (message is Messages.Tx parsed)
                {
                    Transaction<T> tx = Transaction<T>.Deserialize(parsed.Payload);
                    yield return tx;
                }
                else
                {
                    string errorMessage =
                        $"Expected {nameof(Tx)} messages as response of " +
                        $"the {nameof(GetTxs)} message, but got a {message.GetType().Name} " +
                        $"message instead: {message}";
                    throw new InvalidMessageException(errorMessage, message);
                }
            }
        }

        internal async IAsyncEnumerable<(long, HashDigest<SHA256>)> GetDemandBlockHashes(
            BlockChain<T> blockChain,
            IList<(BoundPeer, long)> peersWithHeight,
            IProgress<PreloadState> progress,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            long currentTipIndex = blockChain.Tip?.Index ?? -1;
            BlockLocator locator = blockChain.GetBlockLocator();
            int peersCount = peersWithHeight.Count;
            int i = 0;
            var exceptions = new List<Exception>();
            foreach ((BoundPeer peer, long? peerHeight) in peersWithHeight)
            {
                i++;
                long peerIndex = peerHeight ?? -1;

                long branchIndex = -1;
                HashDigest<SHA256> branchPoint = default;

                // FIXME: The following condition should be fixed together when the issue #459 is
                // fixed.  https://github.com/planetarium/libplanet/issues/459
                if (peer is null || currentTipIndex >= peerIndex)
                {
                    continue;
                }

                long totalBlocksToDownload = peerIndex - currentTipIndex;
                var pairsToYield = new List<Tuple<long, HashDigest<SHA256>>>();
                Exception error = null;
                try
                {
                    var downloaded = new List<HashDigest<SHA256>>();
                    int previousDownloadedCount = -1;
                    int stagnant = 0;
                    const int stagnationLimit = 3;
                    while (downloaded.Count < totalBlocksToDownload)
                    {
                        if (previousDownloadedCount == downloaded.Count &&
                            ++stagnant > stagnationLimit)
                        {
                            break;
                        }

                        previousDownloadedCount = downloaded.Count;
                        _logger.Verbose(
                            "Request block hashes to {Peer} (height: {PeerHeight}) using " +
                            "the locator {@Locator}... ({CurrentIndex}/{EstimatedTotalCount})",
                            peer,
                            peerHeight,
                            locator.Select(h => h.ToString()),
                            downloaded.Count,
                            totalBlocksToDownload
                        );

                        IAsyncEnumerable<Tuple<long, HashDigest<SHA256>>> blockHashes =
                            GetBlockHashes(peer, locator, null, cancellationToken);

                        if (branchIndex == -1 &&
                            await blockHashes.FirstAsync(cancellationToken) is { } t)
                        {
                            t.Deconstruct(out branchIndex, out branchPoint);
                            totalBlocksToDownload = peerIndex - branchIndex;
                        }

                        await foreach (Tuple<long, HashDigest<SHA256>> pair in blockHashes)
                        {
                            pair.Deconstruct(out long dlIndex, out HashDigest<SHA256> dlHash);
                            _logger.Verbose(
                                "Received a block hash from {Peer}: #{BlockIndex} {BlockHash}",
                                peer,
                                dlIndex,
                                dlHash
                            );

                            if (downloaded.Contains(dlHash) || dlHash.Equals(branchPoint))
                            {
                                continue;
                            }

                            downloaded.Add(dlHash);

                            // As C# disallows to yield return inside try-catch block,
                            // we need to work around the limitation by having this buffer.
                            pairsToYield.Add(pair);
                            progress?.Report(
                                new BlockHashDownloadState
                                {
                                    EstimatedTotalBlockHashCount = Math.Max(
                                        totalBlocksToDownload,
                                        downloaded.Count),
                                    ReceivedBlockHashCount = downloaded.Count,
                                    SourcePeer = peer,
                                }
                            );
                        }

                        locator = new BlockLocator(
                            idx =>
                            {
                                if (idx < 0)
                                {
                                    idx = currentTipIndex + downloaded.Count + 1 + idx;
                                }

                                if (idx <= currentTipIndex)
                                {
                                    return blockChain.Store.IndexBlockHash(blockChain.Id, idx);
                                }

                                int relIdx = (int)(idx - currentTipIndex - 1);
                                return downloaded[relIdx];
                            },
                            hash => blockChain.Store.GetBlock<T>(hash) is Block<T> b
                                ? b.Index
                                : currentTipIndex + 1 + downloaded.IndexOf(hash)
                        );
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }

                foreach (Tuple<long, HashDigest<SHA256>> pair in pairsToYield)
                {
                    yield return pair.ToValueTuple();
                }

                if (error is null)
                {
                    break;
                }

                exceptions.Add(error);
                if (i == peersCount)
                {
                    BoundPeer[] peers = peersWithHeight.Select(p => p.Item1).ToArray();
                    _logger.Warning(
                        error,
                        "Failed to fetch demand block hashes from peers: {Peers}",
                        peers
                    );
                    throw new AggregateException(
                        "Failed to fetch demand block hashes from peers: " +
                        string.Join(", ", peers.Select(p => p.ToString())),
                        exceptions
                    );
                }

                const string message =
                    "Failed to fetch demand block hashes from {Peer}; " +
                    "retry with another peer...\n";
                _logger.Debug(error, message, peer, error);
            }
        }

        private void BroadcastBlock(Address? except, Block<T> block)
        {
            _logger.Debug("Trying to broadcast blocks...");
            //var message = new BlockHeaderMessage(BlockChain.Genesis.Hash, block.GetBlockHeader());
            var enumerable = new List<byte[]>();
            enumerable.Add(block.Serialize());
            var message = new BlockBroadcast(enumerable, BlockChain.Genesis.Hash);
            BroadcastMessage(except, message);
            _logger.Debug("Block broadcasting complete.");
        }

        private void BroadcastTxs(Address? except, IEnumerable<Transaction<T>> txs)
        {
            List<TxId> txIds = txs.Select(tx => tx.Id).ToList();
            _logger.Debug("Broadcast {TransactionsNumber} txs...", txIds.Count);
            BroadcastTxIds(except, txIds);
        }

        private void BroadcastMessage(Address? except, Message message)
        {
            Transport.BroadcastMessage(except, message);
        }

        private Task<(BoundPeer Peer, ChainStatus ChainStatus)[]> DialToExistingPeers(
            TimeSpan? dialTimeout,
            CancellationToken cancellationToken
        )
        {
            // FIXME: It would be better if it returns IAsyncEnumerable<(BoundPeer, ChainStatus)>
            // instead.
            var peersExceptMe = Peers.Where(peer => !peer.Address.Equals(AsPeer.Address));

            IEnumerable<Task<(BoundPeer, ChainStatus)>> tasks = peersExceptMe.Select(
                peer => Transport.SendMessageWithReplyAsync(
                    peer, new GetChainStatus(), TimeSpan.FromMinutes(5), cancellationToken
                ).ContinueWith<(BoundPeer, ChainStatus)>(
                    t =>
                    {
                        if (t.IsFaulted || t.IsCanceled || !(t.Result is ChainStatus chainStatus))
                        {
                            switch (t.Exception?.InnerException)
                            {
                                case TimeoutException te:
                                    _logger.Debug(
                                        $"TimeoutException occurred during dial to ({peer})."
                                    );
                                    break;
                                case DifferentAppProtocolVersionException dapve:
                                    _logger.Error(
                                        dapve,
                                        $"Protocol Version is different ({peer}).");
                                    break;
                                case Exception e:
                                    _logger.Error(
                                        e,
                                        $"An unexpected exception occurred during dial to ({peer})."
                                    );
                                    break;
                                default:
                                    break;
                            }

                            // Mark to skip
                            return (peer, null);
                        }
                        else
                        {
                            return (peer, chainStatus);
                        }
                    },
                    cancellationToken
                )
            );

            return Task.WhenAll(tasks).ContinueWith(
                t =>
                {
                    if (t.IsFaulted)
                    {
                        throw t.Exception;
                    }

                    return t.Result
                        .Where(pair => !(pair.Item1 is null || pair.Item2 is null))
                        .ToArray();
                },
                cancellationToken
            );
        }

        private async Task<List<(BoundPeer, long)>> GetPeersWithHeight(
            Block<T> initialTip,
            TimeSpan? dialTimeout,
            CancellationToken cancellationToken)
        {
            var peersWithHeightAndDiff = (await DialToExistingPeers(dialTimeout, cancellationToken))
                .Where(pp =>
                    BlockChain.Genesis.Hash.Equals(pp.ChainStatus?.GenesisHash) &&
                    pp.ChainStatus?.TotalDifficulty > initialTip?.TotalDifficulty)
                .Select(pp => (pp.Peer, pp.ChainStatus.TipIndex, pp.ChainStatus.TotalDifficulty))
                .ToList();

            return peersWithHeightAndDiff
                .OrderByDescending(p => p.Item3)
                .Select(p => (p.Item1, p.Item2))
                .ToList();
        }

        private async Task<long?> SyncRecentStatesFromTrustedPeersAsync(
            BlockChain<T> blockChain,
            IProgress<PreloadState> progress,
            IReadOnlyList<(BoundPeer Peer, HashDigest<SHA256> TipHash)> trustedPeersWithTip,
            BlockLocator baseLocator,
            CancellationToken cancellationToken)
        {
            _logger.Debug(
                "Starts to find a peer to request recent states (candidates: {0} trusted peers).",
                trustedPeersWithTip.Count
            );

            long offset = 0;
            int count = 0;
            foreach ((BoundPeer peer, var blockHash) in trustedPeersWithTip)
            {
                long topIndex = blockChain[blockHash].Index;
                while (!cancellationToken.IsCancellationRequested && offset != -1)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var request = new GetRecentStates(baseLocator, blockHash, offset);

                    _logger.Debug(
                        "Requests recent states to a peer ({Peer}) {Offset}.",
                        peer,
                        offset);
                    Message reply;
                    try
                    {
                        reply = await Transport.SendMessageWithReplyAsync(
                            peer,
                            request,
                            timeout: null,
                            cancellationToken: cancellationToken
                        );

                        _logger.Debug("Received recent states from a peer ({Peer}).", peer);
                    }
                    catch (TimeoutException e)
                    {
                        _logger.Error(
                            "Failed to receive recent states from a peer ({Peer}): " + e,
                            peer
                        );
                        break;
                    }

                    if (reply is RecentStates recentStates
                        && BlockChain.StateStore is IBlockStatesStore blockStatesStore)
                    {
                        if (recentStates.Missing)
                        {
                            throw new InvalidStateTargetException(
                                $"The peer {peer} lacks the target block: {blockHash}.");
                        }

                        int totalCount = recentStates.Iteration;
                        _logger.Debug(
                            "Received {StateRefCount} state refs and {BlockStateCount} block" +
                            " states.",
                            recentStates.StateReferences.Count,
                            recentStates.BlockStates.Count);
                        count++;

                        ReaderWriterLockSlim rwlock = blockChain._rwlock;
                        rwlock.EnterWriteLock();
                        try
                        {
                            Guid chainId = blockChain.Id;

                            _logger.Debug("Starts to store state refs received from {Peer}.", peer);

                            var d = new Dictionary<HashDigest<SHA256>, ISet<string>>();
                            foreach (var pair in recentStates.StateReferences)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                IImmutableSet<string> key = ImmutableHashSet.Create(pair.Key);
                                foreach (HashDigest<SHA256> bHash in pair.Value)
                                {
                                    if (!d.ContainsKey(bHash))
                                    {
                                        d[bHash] = new HashSet<string>();
                                    }

                                    d[bHash].UnionWith(key);
                                }
                            }

                            foreach (KeyValuePair<HashDigest<SHA256>, ISet<string>> pair in d)
                            {
                                HashDigest<SHA256> hash = pair.Key;
                                IImmutableSet<string> stateKeys = pair.Value
                                    .ToImmutableHashSet();
                                if (_store.GetBlockIndex(hash) is long index)
                                {
                                    blockStatesStore.StoreStateReference(
                                        chainId,
                                        stateKeys,
                                        hash,
                                        index);
                                }
                            }

                            _logger.Debug(
                                "Starts to store block states received from {Peer}.",
                                peer);
                            foreach (var pair in recentStates.BlockStates)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                IImmutableDictionary<string, IValue> states =
                                    pair.Value.ToImmutableDictionary();
                                blockStatesStore.SetBlockStates(pair.Key, states);
                            }

                            progress?.Report(new StateDownloadState()
                            {
                                TotalIterationCount = totalCount,
                                ReceivedIterationCount = count,
                            });
                        }
                        finally
                        {
                            rwlock.ExitWriteLock();
                        }

                        _logger.Debug(
                            "Stored state refs and block states. {Offset}",
                            offset);

                        offset = recentStates.Offset;
                    }
                    else
                    {
                        _logger.Debug(
                            "A message received from {Peer} is not a RecentStates but {Reply}.",
                            peer,
                            reply
                        );
                        break;
                    }
                }

                if (offset == -1)
                {
                    _logger.Debug("Received states from trusted peers.");
                    return topIndex;
                }
            }

            _logger.Warning("Failed to receive states from trusted peers.");
            return null;
        }

        private void PreloadExecuteActions(
            BlockChain<T> workspace,
            Block<T> branchpoint,
            long? receivedStateHeight,
            IProgress<PreloadState> progress,
            CancellationToken cancellationToken)
        {
            if (workspace.Renderers.Any())
            {
                throw new ArgumentException(
                    "The workspace chain must not have any renderers.",
                    nameof(workspace)
                );
            }

            long initHeight;
            if (receivedStateHeight is null)
            {
                initHeight = branchpoint?.Index + 1 ?? 0;
            }
            else
            {
                initHeight = receivedStateHeight.Value + 1;
            }

            int count = 0, totalCount = (int)(workspace.Count - initHeight);
            long actionsCount = 0, txsCount = 0;
            DateTimeOffset executionStarted = DateTimeOffset.Now;
            _logger.Debug("Starts to execute actions of {0} blocks.", totalCount);
            var blockHashes = workspace.IterateBlockHashes((int)initHeight);
            foreach (HashDigest<SHA256> hash in blockHashes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Block<T> block = workspace[hash];
                if (block.Index < initHeight)
                {
                    continue;
                }

                workspace.ExecuteActions(block);
                IEnumerable<Transaction<T>>
                    transactions = block.Transactions.ToImmutableArray();
                txsCount += transactions.Count();
                actionsCount +=
                    transactions.Sum(tx => tx.Actions.Count);

                _logger.Debug("Executed actions in the block {0}.", block.Hash);
                progress?.Report(new ActionExecutionState()
                {
                    TotalBlockCount = totalCount,
                    ExecutedBlockCount = ++count,
                    ExecutedBlockHash = block.Hash,
                });
            }

            _logger.Debug("Finished to execute actions.");

            TimeSpan spent = DateTimeOffset.Now - executionStarted;
            _logger.Verbose(
                "Executed totally {0} blocks, {1} txs, {2} actions during {3}",
                totalCount,
                actionsCount,
                txsCount,
                spent);
        }
        private static object broadcastFilterLock = new object();
        private async Task BroadcastTxAsync(
            TimeSpan broadcastTxInterval,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(broadcastTxInterval, cancellationToken);

                    await Task.Run(
                        () =>
                        {
                            List<TxId> txIds = BlockChain
                                .GetStagedTransactionIds()
                                .ToList();

                            if (txIds.Any())
                            {
                                _logger.Debug(
                                    "Broadcast Staged Transactions: [{txIds}]",
                                    string.Join(", ", txIds));
                                BroadcastTxIds(null, txIds);
                            }

                            lock (broadcastFilterLock)
                            {
                                //prune transactions that are no longer staged
                                for (int i = 0; i < broadcastedTransactions.Count(); i++)
                                {
                                    var item = broadcastedTransactions[i];
                                    //if not found in staged remove from broadcasted register to avoid filling up memory
                                    if (!txIds.Where(t => t == item).Any())
                                    {
                                        broadcastedTransactions.RemoveAt(i);
                                    }
                                }
                            }                            
                            
                        }, cancellationToken);
                }
                catch (OperationCanceledException e)
                {
                    _logger.Warning(e, $"{nameof(BroadcastTxAsync)}() is canceled.");
                    throw;
                }
                catch (Exception e)
                {
                    _logger.Error(
                        e,
                        $"An unexpected exception occurred during {nameof(BroadcastTxAsync)}(): {e}"
                    );
                }
            }
        }

        private void BroadcastTxIds(Address? except, IEnumerable<TxId> txIds)
        {
            IEnumerable<Transaction<T>> txs = txIds
               .Where(txId => _store.ContainsTransaction(txId))
               .Select(BlockChain.GetTransaction);

            foreach (Transaction<T> tx in txs)
            {
                _logger.Debug($"Broadcasting to GetTxs {tx.Id} message.");
                Message message = new Messages.Tx(tx.Serialize(true));
                BroadcastMessage(except, message);

                lock (broadcastFilterLock)
                {
                    //register to avoid re-broadcasts [if not already registered]
                    if (!broadcastedTransactions.Where(t => t == tx.Id).Any())
                    {
                        broadcastedTransactions.Add(tx.Id);
                    }
                }  
            }
        }

        private bool IsDemandNeeded(BlockHeader target)
        {
            var result = target.TotalDifficulty > BlockChain.Tip.TotalDifficulty &&
                   (_demandBlockHash is null ||
                    _demandBlockHash.Value.Header.TotalDifficulty < target.TotalDifficulty);
            
            _logger.Debug($"IsDemandNeeded = {result} for {target} total difficulty {target.TotalDifficulty} which is greater than {BlockChain.Tip.TotalDifficulty} and _demandBlockHash isNull={_demandBlockHash is null}");

            return result;
        }

        private async Task SyncPreviousBlocksAsync(
            BlockChain<T> blockChain,
            BoundPeer peer,
            HashDigest<SHA256>? stop,
            IProgress<BlockDownloadState> progress,
            IImmutableSet<Address> trustedStateValidators,
            TimeSpan? dialTimeout,
            long totalBlockCount,
            CancellationToken cancellationToken
        )
        {
            int retry = 3;
            long previousTipIndex = blockChain.Tip?.Index ?? -1;
            BlockChain<T> synced = null;
            StateCompleterSet<T> trustedStateCompleterSet = await GetTrustedStateCompleterAsync(
                trustedStateValidators,
                dialTimeout,
                cancellationToken: cancellationToken
            );

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        long currentTipIndex = blockChain.Tip?.Index ?? -1;
                        long receivedBlockCount = currentTipIndex - previousTipIndex;

                        FillBlocksAsyncStarted.Set();
                        synced = await FillBlocksAsync(
                            peer,
                            blockChain,
                            stop,
                            progress,
                            totalBlockCount,
                            receivedBlockCount,
                            true,
                            cancellationToken
                        );
                        break;
                    }
                    catch (TimeoutException e)
                    {
                        if (retry > 0)
                        {
                            _logger.Error(
                                e,
                                $"{nameof(FillBlocksAsync)}() failed; retrying...: {e}"
                            );
                            retry--;
                        }
                        else
                        {
                            FillBlocksAsyncFailed.Set();
                            throw;
                        }
                    }
                    catch (Exception)
                    {
                        FillBlocksAsyncFailed.Set();
                        throw;
                    }
                }
            }
            finally
            {
                if (synced is BlockChain<T> syncedNotNull
                    && !syncedNotNull.Id.Equals(blockChain?.Id)
                    && (blockChain.Tip is null
                        || blockChain.Tip.TotalDifficulty < syncedNotNull.Tip.TotalDifficulty))
                {
                    blockChain.Swap(
                        synced,
                        render: true,
                        stateCompleters: trustedStateCompleterSet
                    );
                }
            }
        }

        private async Task<BlockChain<T>> FillBlocksAsync(
            BoundPeer peer,
            BlockChain<T> blockChain,
            HashDigest<SHA256>? stop,
            IProgress<BlockDownloadState> progress,
            long totalBlockCount,
            long receivedBlockCount,
            bool evaluateActions,
            CancellationToken cancellationToken
        )
        {
            BlockChain<T> workspace = blockChain;
            var scope = new List<Guid>();
            bool renderActions = evaluateActions;
            bool renderBlocks = true;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Block<T> tip = workspace?.Tip;

                    _logger.Debug("Trying to find branchpoint...");
                    BlockLocator locator = workspace.GetBlockLocator();
                    _logger.Debug("Locator's count: {LocatorCount}", locator.Count());
                    IAsyncEnumerable<Tuple<long, HashDigest<SHA256>>> hashesAsync =
                        GetBlockHashes(peer, locator, stop, cancellationToken);
                    IEnumerable<Tuple<long, HashDigest<SHA256>>> hashes =
                        await hashesAsync.ToArrayAsync();

                    if (!hashes.Any())
                    {
                        _logger.Debug(
                            "Peer [{0}] didn't return any hashes; ignored.",
                            peer.Address.ToHex()
                        );
                        return workspace;
                    }

                    hashes.First().Deconstruct(
                        out long branchIndex,
                        out HashDigest<SHA256> branchPoint
                    );

                    _logger.Debug(
                        "Branch point is #{BranchIndex} {BranchHash}.",
                        branchIndex,
                        branchPoint
                    );

                    if (tip is null || branchPoint.Equals(tip.Hash))
                    {
                        _logger.Debug("It doesn't need to fork.");
                    }
                    else if (!workspace.ContainsBlock(branchPoint))
                    {
                        // FIXME: This behavior can unexpectedly terminate the swarm (and the game
                        // app) if it encounters a peer having a different blockchain, and therefore
                        // can be exploited to remotely shut down other nodes as well.
                        // Since the intention of this behavior is to prevent mistakes to try to
                        // connect incorrect seeds (by a user), this behavior should be limited for
                        // only seed peers.
                        var msg =
                            $"Since the genesis block is fixed to {BlockChain.Genesis} " +
                            "protocol-wise, the blockchain which does not share " +
                            "any mutual block is not acceptable.";
                        throw new InvalidGenesisBlockException(
                            branchPoint,
                            workspace.Genesis.Hash,
                            msg);
                    }
                    else
                    {
                        _logger.Debug("Forking needed. Trying to fork...");
                        workspace = workspace.Fork(branchPoint);
                        Guid workChainId = workspace.Id;
                        scope.Add(workChainId);
                        renderActions = false;
                        renderBlocks = false;
                        _logger.Debug("Forking complete.");
                    }

                    if (!(workspace.Tip is null))
                    {
                        hashes = hashes.Skip(1);
                    }

                    _logger.Debug("Trying to fill up previous blocks...");

                    var hashesAsArray =
                        hashes as Tuple<long, HashDigest<SHA256>>[] ?? hashes.ToArray();
                    if (!hashesAsArray.Any())
                    {
                        break;
                    }

                    int hashCount = hashesAsArray.Count();
                    _logger.Debug(
                        $"Required hashes (count: {hashCount}). " +
                        $"(tip: {workspace.Tip?.Hash})"
                    );

                    totalBlockCount = Math.Max(totalBlockCount, receivedBlockCount + hashCount);

                    IAsyncEnumerable<Block<T>> blocks = GetBlocksAsync(
                        peer,
                        hashesAsArray.Select(pair => pair.Item2),
                        cancellationToken
                    );

                    await foreach (Block<T> block in blocks)
                    {
                        _logger.Debug(
                            "Try to append a block #{BlockIndex} {BlockHash}...",
                            block.Index,
                            block.Hash
                        );

                        cancellationToken.ThrowIfCancellationRequested();

                        workspace.Append(
                            block,
                            DateTimeOffset.UtcNow,
                            evaluateActions: evaluateActions,
                            renderBlocks: renderBlocks,
                            renderActions: renderActions
                        );
                        receivedBlockCount++;
                        progress?.Report(new BlockDownloadState
                        {
                            TotalBlockCount = totalBlockCount,
                            ReceivedBlockCount = receivedBlockCount,
                            ReceivedBlockHash = block.Hash,
                            SourcePeer = peer,
                        });
                        _logger.Debug(
                            "Block #{BlockIndex} {BlockHash} was appended.",
                            block.Index,
                            block.Hash
                        );
                    }
                }
            }
            catch
            {
                if (workspace?.Id is Guid workspaceId && scope.Contains(workspaceId))
                {
                    _store.DeleteChainId(workspaceId);
                }

                throw;
            }
            finally
            {
                foreach (var id in scope.Where(guid => guid != workspace?.Id))
                {
                    _store.DeleteChainId(id);
                }
            }

            return workspace;
        }

        private BlockChain<T> AcceptBlock(
            BoundPeer peer,
            BlockChain<T> blockChain,
            Block<T> block,
            CancellationToken cancellationToken
        )
        {
            BlockChain<T> workspace = blockChain;
            bool renderActions = true;
            bool renderBlocks = true;
            bool evaluateActions = true;
            Block<T> tip = workspace?.Tip;

            if (workspace != null && workspace.ContainsBlock(block.Hash))
            {
                _logger.Debug($"Already contains block #{block.Index} {block.Hash}, ignoring.");
                return workspace;
            }

            if (tip.Index >= block.Index)
            {
                _logger.Debug($"Block index is equal or lower than tip #{block.Index}<{tip.Index} {block.Hash}, ignoring.");
                return workspace;
            }

            _logger.Debug($"Trying to append a block #{block.Index} {block.Hash}...");
            
            workspace.Append(
                block,
                DateTimeOffset.UtcNow,
                evaluateActions: evaluateActions,//FIXME:not yet sure what are these
                renderBlocks: renderBlocks,//FIXME:not yet sure what are these
                renderActions: renderActions//FIXME:not yet sure what are these
            );

            BlockReceived.Set();
            BlockAppended.Set();

            _logger.Debug($"Block #{block.Index} {block.Hash} was appended.");

            return workspace;
        }

        private async Task ProcessFillBlocks(
            TimeSpan dialTimeout,
            IImmutableSet<Address> trustedStateValidators,
            CancellationToken cancellationToken
        )
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_demandBlockHash is null ||
                    _demandBlockHash.Value.Header.TotalDifficulty <= BlockChain.Tip.TotalDifficulty)
                {
                    await Task.Delay(1, cancellationToken);
                    continue;
                }

                BoundPeer peer = _demandBlockHash.Value.Peer;
                var hash = new HashDigest<SHA256>(_demandBlockHash.Value.Header.Hash.ToArray());
                _logger.Debug($"Found open _demandBlockHash  for peer:{_demandBlockHash.Value.Peer} hash:{hash}");
                try
                {
                    await SyncPreviousBlocksAsync(
                        BlockChain,
                        peer,
                        hash,
                        null,
                        trustedStateValidators,
                        dialTimeout,
                        0,
                        cancellationToken);

                    // FIXME: Clean up events
                    BlockReceived.Set();
                    BlockAppended.Set();
                    BroadcastBlock(peer.Address, BlockChain.Tip);
                }
                catch (TimeoutException)
                {
                    _logger.Debug($"Timeout occurred during {nameof(ProcessFillBlocks)}");
                }
                catch (InvalidBlockIndexException ibie)
                {
                    _logger.Warning(
                        $"{nameof(InvalidBlockIndexException)} occurred during " +
                        $"{nameof(ProcessFillBlocks)}: " +
                        "{ibie}", ibie);
                }
                catch (Exception e)
                {
                    var msg =
                        $"Unexpected exception occurred during" +
                        $" {nameof(ProcessFillBlocks)}: {{e}}";
                    _logger.Error(e, msg, e);
                }
                finally
                {
                    using (await _blockSyncMutex.LockAsync(cancellationToken))
                    {
                        _logger.Debug($"Closed _demandBlockHash for peer:{_demandBlockHash.Value.Peer} hash:{hash}");
                        _demandBlockHash = null;                        
                    }
                }
            }
        }

        private async Task ProcessFillTxs(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_demandTxIds.IsEmpty)
                {
                    await Task.Delay(1, cancellationToken);
                    continue;
                }

                _logger.Debug(
                    "Processing txids: {@txIds}",
                    _demandTxIds.Keys.Select(txid => txid.ToString()));
                var demandTxIds = _demandTxIds.ToArray();
                var demands = new Dictionary<BoundPeer, HashSet<TxId>>();

                foreach (var kv in demandTxIds)
                {
                    if (!demands.ContainsKey(kv.Value))
                    {
                        demands[kv.Value] = new HashSet<TxId>();
                    }

                    demands[kv.Value].Add(kv.Key);
                }

                var txs = new HashSet<Transaction<T>>();
                var tasks = new List<Task<List<Transaction<T>>>>();
                foreach (var kv in demands)
                {
                    IAsyncEnumerable<Transaction<T>> fetched =
                        GetTxsAsync(kv.Key, kv.Value, cancellationToken);
                    ValueTask<List<Transaction<T>>> vt = fetched.ToListAsync(cancellationToken);

                    if (vt.IsCompletedSuccessfully)
                    {
                        txs.UnionWith(vt.Result);
                    }
                    else
                    {
                        tasks.Add(vt.AsTask());
                    }
                }

                try
                {
                    await tasks.WhenAll();
                }
                catch (AggregateException ae)
                {
                    foreach (var e in ae.InnerExceptions)
                    {
                        _logger.Information($"Some tasks faulted during {nameof(GetTxsAsync)}(). Error: {e} ");
                    }                    
                }

                foreach (Task<List<Transaction<T>>> task in tasks)
                {
                    if (!task.IsFaulted)
                    {
                        // `task.Result` is okay because we've already waited.
                        txs.UnionWith(task.Result);
                    }
                }

                txs = new HashSet<Transaction<T>>(
                    txs.Where(tx => BlockChain.Policy.DoesTransactionFollowsPolicy(tx, BlockChain))
                );

                foreach (Transaction<T> tx in txs)
                {
                    try
                    {
                        BlockChain.StageTransaction(tx);
                    }
                    catch (InvalidTxException ite)
                    {
                        _logger.Error(
                            ite,
                            "{TxId} will not be staged since it is invalid.",
                            tx.Id
                        );
                    }
                }

                if (txs.Any())
                {
                    TxReceived.Set();
                    _logger.Debug(
                        "Txs staged successfully: {@txIds}",
                        txs.Select(tx => tx.Id.ToString()));

                    // FIXME: Should exclude peers of source of the transaction ids.
                    BroadcastTxs(null, txs);
                }
                else
                {
                    _logger.Information(
                        "Failed to get transactions to stage: {@txIds}",
                        demandTxIds.Select(txId => txId.ToString()));
                }

                foreach (var kv in demandTxIds)
                {
                    _demandTxIds.TryRemove(kv.Key, out BoundPeer value);
                }
            }
        }
    }
}

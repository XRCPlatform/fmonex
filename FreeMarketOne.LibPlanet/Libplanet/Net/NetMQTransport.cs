using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Libplanet.Crypto;
using Libplanet.Net.Messages;
using Libplanet.Net.Protocols;
using Libplanet.Stun;
using NetMQ;
using NetMQ.Sockets;
using Nito.AsyncEx;
using Serilog;

namespace Libplanet.Net
{
    internal class NetMQTransport : ITransport
    {
        private const int MessageHistoryCapacity = 3000;

        private static readonly TimeSpan TurnAllocationLifetime =
            TimeSpan.FromSeconds(777);

        // TURN Permission lifetime was defined in RFC 5766
        // see also https://tools.ietf.org/html/rfc5766#section-8
        private static readonly TimeSpan TurnPermissionLifetime =
            TimeSpan.FromMinutes(5);

        private readonly PrivateKey _privateKey;
        private readonly AppProtocolVersion _appProtocolVersion;
        private readonly IImmutableSet<PublicKey> _trustedAppProtocolVersionSigners;
        private readonly string _host;
        private readonly IList<IceServer> _iceServers;
        private readonly ILogger _logger;
        private readonly AsyncLock _turnClientMutex;
        private string _socks5Proxy;
        private NetMQQueue<NetMQMessage> _replyQueue;
        private NetMQQueue<(Address?, Message)> _broadcastQueue;

        private RouterSocket _router;
        private NetMQPoller _routerPoller;
        private NetMQPoller _broadcastPoller;

        private int? _listenPort;
        private TurnClient _turnClient;
        private bool _behindNAT;
        private DnsEndPoint _endPoint;
        private IPAddress _publicIPAddress;

        private AsyncCollection<MessageRequest> _requests;
        private long _requestCount;
        private CancellationTokenSource _runtimeCancellationTokenSource;
        private CancellationTokenSource _turnCancellationTokenSource;
        private Task _runtimeProcessor;
        private List<Task> _turnTasks;

        private TaskCompletionSource<object> _runningEvent;
        private CancellationToken _cancellationToken;
        private ConcurrentDictionary<Address, DealerSocket> _dealers;

        /// <summary>
        /// The <see cref="EventHandler" /> triggered when the different version of
        /// <see cref="Peer" /> is discovered.
        /// </summary>
        private DifferentAppProtocolVersionEncountered _differentAppProtocolVersionEncountered;

        public NetMQTransport(
            PrivateKey privateKey,
            AppProtocolVersion appProtocolVersion,
            IImmutableSet<PublicKey> trustedAppProtocolVersionSigners,
            int? tableSize,
            int? bucketSize,
            int workers,
            string host,
            int? listenPort,
            IEnumerable<IceServer> iceServers,
            DifferentAppProtocolVersionEncountered differentAppProtocolVersionEncountered,
            EventHandler<Message> processMessageHandler,
            ILogger logger,
            string socks5Proxy)
        {
            Running = false;

            _privateKey = privateKey;
            _appProtocolVersion = appProtocolVersion;
            _trustedAppProtocolVersionSigners = trustedAppProtocolVersionSigners;
            _host = host;
            _listenPort = listenPort;
            _differentAppProtocolVersionEncountered = differentAppProtocolVersionEncountered;
            _turnClientMutex = new AsyncLock();
            _socks5Proxy = socks5Proxy ?? null;
            ProcessMessageHandler = processMessageHandler;

            if (_host != null && _listenPort is int listenPortAsInt)
            {
                _endPoint = new DnsEndPoint(_host, listenPortAsInt);
            }

            _iceServers = iceServers?.ToList();
            if (_host == null && (_iceServers == null || !_iceServers.Any()))
            {
                throw new ArgumentException(
                    $"Swarm requires either {nameof(host)} or " +
                    $"{nameof(iceServers)}."
                );
            }

            _logger = logger;

            _requests = new AsyncCollection<MessageRequest>();
            _runtimeCancellationTokenSource = new CancellationTokenSource();
            _turnCancellationTokenSource = new CancellationTokenSource();
            _requestCount = 0;
            _runtimeProcessor = Task.Factory.StartNew(
                () =>
                {
                    // Ignore NetMQ related exceptions during NetMQRuntime.Dispose() to stabilize
                    // tests
                    try
                    {
                        var runtime = new NetMQRuntime();
                        var workerTasks = new Task[workers];

                        for (int i = 0; i < workers; i++)
                        {
                            workerTasks[i] = ProcessRuntime(
                                _runtimeCancellationTokenSource.Token
                            );
                        }

                        runtime.Run(workerTasks);
                    }
                    catch (NetMQException e)
                    {
                        _logger.Error(
                            e,
                            $"NetMQException occurred in {nameof(_runtimeProcessor)}."
                        );
                    }
                    catch (ObjectDisposedException e)
                    {
                        _logger.Error(
                            e,
                            $"ObjectDisposedException occurred in {nameof(_runtimeProcessor)}."
                        );
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            );

            MessageHistory = new FixedSizedQueue<Message>(MessageHistoryCapacity);
            Protocol = new KademliaProtocol(
                this,
                _privateKey.ToAddress(),
                _appProtocolVersion,
                _trustedAppProtocolVersionSigners,
                _differentAppProtocolVersionEncountered,
                _logger,
                tableSize,
                bucketSize);
            _dealers = new ConcurrentDictionary<Address, DealerSocket>();
        }

        /// <summary>
        /// The <see cref="EventHandler" /> triggered when a <see cref="Message"/> is
        /// received and needs processing.
        /// </summary>
        private event EventHandler<Message> ProcessMessageHandler;

        public Peer AsPeer => _endPoint is null
            ? new Peer(
                _privateKey.PublicKey,
                _publicIPAddress)
            : new BoundPeer(
                _privateKey.PublicKey,
                _endPoint,
                _publicIPAddress);

        public IEnumerable<BoundPeer> Peers => Protocol.Peers;

        public DateTimeOffset? LastMessageTimestamp { get; private set; }

        /// <summary>
        /// Whether this <see cref="NetMQTransport"/> instance is running.
        /// </summary>
        public bool Running
        {
            get => _runningEvent.Task.Status == TaskStatus.RanToCompletion;

            private set
            {
                if (value)
                {
                    _runningEvent.TrySetResult(null);
                }
                else
                {
                    _runningEvent = new TaskCompletionSource<object>();
                }
            }
        }

        internal IProtocol Protocol { get; }

        internal FixedSizedQueue<Message> MessageHistory { get; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (Running)
            {
                throw new SwarmException("Swarm is already running.");
            }

            _router = new RouterSocket();
            _router.Options.RouterHandover = true;

            _router.Bind($"tcp://{(_host.Contains(".onion") ? "127.0.0.1" : _host)}:{_listenPort}");

            _logger.Information($"Listen on {_listenPort}");

            if (_host is null && !(_iceServers is null))
            {
                await InitializeTurnClient();
            }

            _cancellationToken = cancellationToken;

            if (!_behindNAT)
            {
                _endPoint = new DnsEndPoint(
                    _host ?? _publicIPAddress.ToString(),
                    _listenPort.Value);
            }

            _replyQueue = new NetMQQueue<NetMQMessage>();
            _broadcastQueue = new NetMQQueue<(Address?, Message)>();
            _routerPoller = new NetMQPoller { _router, _replyQueue };
            _broadcastPoller = new NetMQPoller { _broadcastQueue };

            _router.ReceiveReady += ReceiveMessage;
            _replyQueue.ReceiveReady += DoReply;
            _broadcastQueue.ReceiveReady += DoBroadcast;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (Running)
            {
                throw new SwarmException("Swarm is already running.");
            }

            Running = true;

            List<Task> tasks = new List<Task>();

            tasks.Add(
                RefreshTableAsync(
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(1),
                    _cancellationToken));
            tasks.Add(RebuildConnectionAsync(TimeSpan.FromMinutes(30), _cancellationToken));
            tasks.Add(RunPoller(_routerPoller));
            tasks.Add(RunPoller(_broadcastPoller));

            await Task.WhenAll(tasks);
        }

        public async Task StopAsync(
            TimeSpan waitFor,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            if (Running)
            {
                await Task.Delay(waitFor, cancellationToken);

                _broadcastQueue.ReceiveReady -= DoBroadcast;
                _replyQueue.ReceiveReady -= DoReply;
                _router.ReceiveReady -= ReceiveMessage;
                _router.Unbind($"tcp:/127.0.0.1:{_listenPort}");

                if (_routerPoller.IsRunning)
                {
                    _routerPoller.Dispose();
                }

                if (_broadcastPoller.IsRunning)
                {
                    _broadcastPoller.Dispose();
                }

                _broadcastQueue.Dispose();
                _replyQueue.Dispose();
                _router.Dispose();
                _turnClient?.Dispose();

                foreach (DealerSocket dealer in _dealers.Values)
                {
                    dealer.Dispose();
                }

                _dealers.Clear();

                Running = false;
            }
        }

        public Task BootstrapAsync(
            IEnumerable<BoundPeer> bootstrapPeers,
            TimeSpan? pingSeedTimeout,
            TimeSpan? findNeighborsTimeout,
            int depth = Kademlia.MaxDepth,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => Protocol.BootstrapAsync(
            bootstrapPeers.ToImmutableList(),
            pingSeedTimeout,
            findNeighborsTimeout,
            depth,
            cancellationToken
        );

        public async Task AddPeersAsync(
            IEnumerable<Peer> peers,
            TimeSpan? timeout,
            CancellationToken cancellationToken)
        {
            if (Protocol is null)
            {
                throw new ArgumentNullException(nameof(Protocol));
            }

            try
            {
                var kp = (KademliaProtocol)Protocol;

                var tasks = new List<Task>();
                foreach (Peer peer in peers)
                {
                    if (peer is BoundPeer boundPeer)
                    {
                        tasks.Add(kp.PingAsync(
                            boundPeer,
                            timeout: timeout,
                            cancellationToken: cancellationToken));
                    }
                }

                _logger.Verbose("Trying to ping all {PeersNumber} peers.", tasks.Count);
                await Task.WhenAll(tasks);
                _logger.Verbose("Update complete.");
            }
            catch (DifferentAppProtocolVersionException e)
            {
                AppProtocolVersion expected = e.ExpectedVersion, actual = e.ActualVersion;
                _logger.Debug(
                    $"Different version encountered during {nameof(AddPeersAsync)}().\n" +
                    "Expected version: {ExpectedVersion} ({ExpectedVersionExtra}) " +
                    "[{ExpectedSignature}; {ExpectedSigner}]\n" +
                    "Actual version: {ActualVersion} ({ActualVersionExtra}) [{ActualSignature};" +
                    "{ActualSigner}]",
                    expected.Version,
                    expected.Extra,
                    ByteUtil.Hex(expected.Signature),
                    expected.Signer.ToString(),
                    actual.Version,
                    actual.Extra,
                    ByteUtil.Hex(actual.Signature),
                    actual.Signer
                );
            }
            catch (TimeoutException)
            {
                _logger.Debug(
                    $"Timeout occurred during {nameof(AddPeersAsync)}() after {timeout}.");
                throw;
            }
            catch (TaskCanceledException)
            {
                _logger.Debug($"Task is cancelled during {nameof(AddPeersAsync)}().");
            }
            catch (Exception e)
            {
                _logger.Error(
                    e,
                    $"Unexpected exception occurred during {nameof(AddPeersAsync)}().");
                throw;
            }
        }

        public async Task<BoundPeer> FindSpecificPeerAsync(
            Address target,
            int depth,
            TimeSpan? timeout,
            CancellationToken cancellationToken)
        {
            var kp = (KademliaProtocol)Protocol;
            return await kp.FindSpecificPeerAsync(
                target,
                depth,
                timeout,
                cancellationToken);
        }

        public string Trace() => Protocol is null ? string.Empty : Protocol.Trace();

        public void Dispose()
        {
            _runtimeCancellationTokenSource.Cancel();
            _turnCancellationTokenSource.Cancel();
            _runtimeProcessor.Wait();
        }

        public Task WaitForRunningAsync() => _runningEvent.Task;

        public async Task<Message> SendMessageWithReplyAsync(
           BoundPeer peer,
           Message message,
           TimeSpan? timeout,
           CancellationToken cancellationToken
           )
        {
            return await SendMessageWithReplyAsync(peer, message, timeout, cancellationToken, 0);
        }

        private async Task<Message> SendMessageWithReplyAsync(
            BoundPeer peer,
            Message message,
            TimeSpan? timeout,
            CancellationToken cancellationToken,
            int retry = 0
            )
        {
            try
            {
                IEnumerable<Message> replies =
                await SendMessageWithReply(peer, message, timeout, 1, cancellationToken);
                Message reply = replies.First();
                return reply;
            }
            catch (Exception e)
            {
                _logger.Debug($"Error while processing SendMessageWithReplyAsync {message} to {peer} retry# {retry}");
                if (retry < 5)
                {
                    retry++;
                    return await SendMessageWithReplyAsync(peer, message, timeout, cancellationToken, retry);
                }
                else
                {
                    throw e;
                }
            }
        }

        public async Task<IEnumerable<Message>> SendMessageWithReplyAsync(
            BoundPeer peer,
            Message message,
            TimeSpan? timeout,
            int expectedResponses,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            return await SendMessageWithReplyAsync(peer, message, timeout, expectedResponses, cancellationToken, 0);
        }

        private async Task<IEnumerable<Message>> SendMessageWithReplyAsync(
            BoundPeer peer,
            Message message,
            TimeSpan? timeout,
            int expectedResponses,
            CancellationToken cancellationToken = default(CancellationToken),
            int retry = 0
        )
        {
            try
            {
                return await SendMessageWithReply(peer, message, timeout, expectedResponses, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.Debug($"Error while processing SendMessageWithReplyAsync {message} to {peer} retry# {retry}");
                if (retry < 5)
                {
                    retry++;
                    return await SendMessageWithReplyAsync(peer, message, timeout, expectedResponses, cancellationToken, retry);
                }
                else
                {
                    throw e;
                }
            }

        }

        private async Task<IEnumerable<Message>> SendMessageWithReply(
            BoundPeer peer,
            Message message,
            TimeSpan? timeout,
            int expectedResponses,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {

            if (_behindNAT)
            {
                await CreatePermission(peer);
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();
            //creating only one exclusive open channel until message is recieved to avoid unexcpected messages recieved.
            //FIXME:this is relying on string interning so a bit hack, a dictionary is probably better, but need to solve problem now.
            _logger.Debug($"About to start blocking calls to {peer} while SendMessageWithReplyAsync {message}");

            lock (string.Intern(peer.ToString()))
            {

                _logger.Debug($"Blocking further calls to {peer} while SendMessageWithReplyAsync {message} time waiting to aquire lock in ms:{sw.ElapsedMilliseconds}");
                Guid reqId = Guid.NewGuid();
                try
                {

                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    _logger.Verbose(
                        "Enqueue a request {RequestId} to {PeerAddress}: {Message}.",
                        reqId,
                        peer.Address,
                        message
                    );
                    var tcs = new TaskCompletionSource<IEnumerable<Message>>();
                    Interlocked.Increment(ref _requestCount);

                    // FIXME should we also cancel tcs sender side too?
                    cancellationToken.Register(() => tcs.TrySetCanceled());

                    _requests.AddAsync(
                        new MessageRequest(reqId, message, peer, now, timeout, expectedResponses, tcs),
                        cancellationToken
                    ).GetAwaiter().GetResult();

                    _logger.Verbose(
                        "Enqueued a request {RequestId} to {PeerAddress}: {Message}; " +
                        "{LeftRequests} left.",
                        reqId,
                        peer.Address,
                        message,
                        Interlocked.Read(ref _requestCount)
                    );

                    if (expectedResponses > 0)
                    {
                        var reply = (tcs.Task.ConfigureAwait(false).GetAwaiter().GetResult()).ToList();
                        foreach (var msg in reply)
                        {
                            MessageHistory.Enqueue(msg);
                        }

                        const string logMsg =
                            "Received {ReplyMessageCount} reply messages to {RequestId} " +
                            "from {PeerAddress}: {ReplyMessages}.";
                        _logger.Debug(logMsg, reply.Count, reqId, peer.Address, reply);

                        sw.Stop();
                        _logger.Debug($"EndOfBlocing further calls to {peer} while SendMessageWithReplyAsync {message} elapsed ms: {sw.ElapsedMilliseconds}");
                        return reply;
                    }
                    else
                    {
                        return new Message[0];
                    }
                }
                catch (DifferentAppProtocolVersionException e)
                {
                    const string logMsg =
                        "{PeerAddress} sent a reply to {RequestId} with " +
                        "a different app protocol version; " +
                        "expected: {ExpectedVersion}; actual: {ActualVersion}.";
                    _logger.Error(e, logMsg, peer.Address, reqId, e.ExpectedVersion, e.ActualVersion);
                    throw;
                }
                catch (TimeoutException)
                {
                    _logger.Debug(
                        $"{nameof(NetMQTransport)}.{nameof(SendMessageWithReply)}() timed out " +
                        "after {Timeout} of waiting a reply to {RequestId} from {PeerAddress}.",
                        timeout,
                        reqId,
                        peer.Address
                    );
                    throw;
                }
                catch (TaskCanceledException)
                {
                    _logger.Debug(
                        $"{nameof(NetMQTransport)}.{nameof(SendMessageWithReply)}() was " +
                        "cancelled to  wait a reply to {RequestId} from {PeerAddress}.",
                        reqId,
                        peer.Address
                    );
                    throw;
                }
                catch (Exception e)
                {
                    var msg =
                        $"{nameof(NetMQTransport)}.{nameof(SendMessageWithReply)}() encountered " +
                        "an unexpected exception during sending a request {RequestId} to " +
                        "{PeerAddress} and waiting a reply to it: {Exception}.";
                    _logger.Error(e, msg, reqId, peer.Address, e);
                    throw;
                }
            }
        }

        public void BroadcastMessage(Address? except, Message message)
        {
            _broadcastQueue.Enqueue((except, message));
        }

        public void ReplyMessage(Message message)
        {
            string identityHex = ByteUtil.Hex(message.Identity);
            _logger.Debug("Reply {Message} to {Identity}...", message, identityHex);
            _replyQueue.Enqueue(message.ToNetMQMessage(_privateKey, AsPeer, _appProtocolVersion));
        }

        public async Task CheckAllPeersAsync(CancellationToken cancellationToken, TimeSpan? timeout)
        {
            var kp = (KademliaProtocol)Protocol;
            await kp.CheckAllPeersAsync(cancellationToken, timeout);
        }

        private void ReceiveMessage(object sender, NetMQSocketEventArgs e)
        {
            NetMQMessage raw = new NetMQMessage();
            while (e.Socket.TryReceiveMultipartMessage(ref raw))
            {
                try
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    _logger.Verbose(
                        "A raw message [frame count: {0}] has received.",
                        raw.FrameCount
                    );
                    Message message = Message.Parse(
                        raw,
                        false,
                        _appProtocolVersion,
                        _trustedAppProtocolVersionSigners,
                        _differentAppProtocolVersionEncountered);
                    _logger.Debug("A message has parsed: {0}, from {1}", message, message.Remote);

                    if (!message.Remote.ToString().EndsWith($":{_listenPort}."))
                    {
                        _logger.Debug($"A message {message} arrived from a wrong swarm: expected swarm {_listenPort}, recieved from {message.Remote}");
                        //can't redirect to another listener at this stage
                    }
                    else
                    {
                        MessageHistory.Enqueue(message);
                        LastMessageTimestamp = DateTimeOffset.UtcNow;

                        try
                        {
                            Protocol.ReceiveMessage(message);
                            ProcessMessageHandler?.Invoke(this, message);
                        }
                        catch (Exception exc)
                        {
                            _logger.Error(
                                exc,
                                "Something went wrong during message parsing: {0}",
                                exc);
                            throw;
                        }
                    }
                }
                catch (DifferentAppProtocolVersionException dapve)
                {
                    var differentVersion = new DifferentVersion()
                    {
                        Identity = dapve.Identity,
                    };
                    ReplyMessage(differentVersion);
                    _logger.Debug("Message from peer with different version received.");
                }
                catch (InvalidMessageException ex)
                {
                    _logger.Error(ex, $"Could not parse NetMQMessage properly; ignore: {{0}}", ex);
                }
                catch (Exception ex)
                {
                    const string mname = nameof(ReceiveMessage);
                    _logger.Error(
                        ex,
                        $"An unexpected exception occurred during {mname}(): {{0}}",
                        ex
                    );
                }
            }
        }

        private void DoBroadcast(object sender, NetMQQueueEventArgs<(Address?, Message)> e)
        {
            try
            {
                (Address? except, Message msg) = e.Queue.Dequeue();

                // FIXME Should replace with PUB/SUB model.
                List<BoundPeer> peers = Protocol.PeersToBroadcast(except).ToList();
                _logger.Debug("Broadcasting message: {Message} as {AsPeer}", msg, AsPeer);
                _logger.Debug("Peers to broadcast: {PeersCount}", peers.Count);

                NetMQMessage message = msg.ToNetMQMessage(_privateKey, AsPeer, _appProtocolVersion);
                Stopwatch sw = new Stopwatch();
                sw.Start();
                foreach (BoundPeer peer in peers)
                {
                    if (peer.EndPoint.Port == _listenPort)
                    {
                        if (!_dealers.TryGetValue(peer.Address, out DealerSocket dealer))
                        {
                            dealer = new DealerSocket(ToNetMQAddress(peer));
                            _dealers[peer.Address] = dealer;
                        }
                        _logger.Debug($"About to broadcast {message} to : {peer}");
                        int retryCount = 1;
                        bool success = dealer.TrySendMultipartMessage(TimeSpan.FromMinutes(5), message);

                        while (!success && retryCount < 5)
                        {
                            _logger.Debug($"Broadcasting try # {retryCount} failed. [Peer: {peer}, Message: {msg}] duration ms: {sw.ElapsedMilliseconds}");
                            retryCount++;
                            success = dealer.TrySendMultipartMessage(TimeSpan.FromMinutes(5), message);
                        }

                        if (success)
                        {
                            sw.Stop();
                            _logger.Debug($"Broadcasting try # {retryCount} succeeded. [Peer: {peer}, Message: {msg}] duration ms: {sw.ElapsedMilliseconds}");
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                _logger.Error(exc, $"Unexpected error occurred during {nameof(DoBroadcast)}(). {exc}");
                throw;
            }
        }

        private void DoReply(object sender, NetMQQueueEventArgs<NetMQMessage> e)
        {
            NetMQMessage msg = e.Queue.Dequeue();
            string identityHex = ByteUtil.Hex(msg[0].Buffer);

            // FIXME The current timeout value(1 sec) is arbitrary.
            // We should make this configurable or fix it to an unneeded structure.
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int tryCount = 1;
            bool success = _router.TrySendMultipartMessage(TimeSpan.FromMinutes(5), msg);

            while (!success && tryCount < 5)
            {
                _logger.Debug($"Failed to reply to {identityHex} on try# {tryCount} duration ms: {sw.ElapsedMilliseconds}");
                tryCount++;
                success = _router.TrySendMultipartMessage(TimeSpan.FromMinutes(5), msg);
            }

            if (success)
            {
                sw.Stop();
                _logger.Debug($"A reply sent to {identityHex} on try# {tryCount} duration ms: {sw.ElapsedMilliseconds}");
            }
        }

        private async Task RefreshAllocate(CancellationToken cancellationToken)
        {
            TimeSpan lifetime = TurnAllocationLifetime;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(lifetime - TimeSpan.FromMinutes(1), cancellationToken);
                    _logger.Debug("Refreshing TURN allocation...");
                    lifetime = await _turnClient.RefreshAllocationAsync(lifetime);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException e)
                {
                    _logger.Warning(e, $"{nameof(RefreshAllocate)}() is cancelled.");
                    throw;
                }
                catch (Exception e)
                {
                    _logger.Error(
                        e,
                        $"An unexpected exception occurred during {nameof(RefreshAllocate)}(): {e}"
                    );
                }
            }
        }

        private async Task RefreshPermissions(
            CancellationToken cancellationToken)
        {
            TimeSpan lifetime = TurnPermissionLifetime;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(lifetime - TimeSpan.FromMinutes(1), cancellationToken);
                    _logger.Debug("Refreshing permissions...");
                    await Task.WhenAll(Protocol.Peers.Select(CreatePermission));
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException e)
                {
                    _logger.Warning(e, $"{nameof(RefreshPermissions)}() is cancelled.");
                    throw;
                }
                catch (Exception e)
                {
                    var msg = "An unexpected exception occurred during " +
                              $"{nameof(RefreshPermissions)}(): {{e}}";
                    _logger.Error(e, msg, e);
                }
            }
        }

        private async Task ProcessRuntime(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _logger.Verbose("Waiting for a new request...");
                MessageRequest req = await _requests.TakeAsync(cancellationToken);
                Interlocked.Decrement(ref _requestCount);
                _logger.Debug(
                    "Request taken. {Count} requests are left.",
                    Interlocked.Read(ref _requestCount));

                try
                {
                    if (req.Peer.EndPoint.Port == _listenPort.Value)
                    {
                        await ProcessRequest(req, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.Information(
                        $"Cancellation requested; shut down {nameof(NetMQTransport)}." +
                        $"{nameof(ProcessRuntime)}()..."
                    );
                    throw;
                }
                catch (Exception e)
                {
                    if (req.Retryable)
                    {
                        const int retryAfter = 100;
                        _logger.Debug(
                            $"Unexpected exception occurred during {nameof(ProcessRuntime)}(): " +
                            "{Exception}; retry after {DelayMs} ms...",
                            e,
                            retryAfter
                        );
                        Interlocked.Increment(ref _requestCount);
                        await _requests.AddAsync(req.Retry(), cancellationToken);
                        await Task.Delay(retryAfter, cancellationToken);
                    }
                    else
                    {
                        _logger.Error("Failed to process request[{req}]; discard it.", req);
                    }
                }
            }
        }

        private async Task ProcessRequest(MessageRequest req, CancellationToken cancellationToken)
        {
            _logger.Verbose(
                "Request {Message}({RequestId}) is ready to be processed in {TimeSpan}.",
                req.Message,
                req.Id,
                DateTimeOffset.UtcNow - req.RequestedTime);
            DateTimeOffset startedTime = DateTimeOffset.UtcNow;

            if (!_dealers.TryGetValue(req.Peer.Address, out DealerSocket dealer))
            {
                dealer = new DealerSocket(ToNetMQAddress(req.Peer));
                _dealers[req.Peer.Address] = dealer;
            }

            _logger.Debug(
                "Trying to send {Message} to {Peer}...",
                req.Message,
                req.Peer
            );
            var message = req.Message.ToNetMQMessage(_privateKey, AsPeer, _appProtocolVersion);
            var result = new List<Message>();
            TaskCompletionSource<IEnumerable<Message>> tcs = req.TaskCompletionSource;
            try
            {
                await dealer.SendMultipartMessageAsync(
                    message,
                    timeout: req.Timeout,
                    cancellationToken: cancellationToken
                );

                _logger.Debug("A message {Message} sent.", req.Message);

                foreach (var i in Enumerable.Range(0, req.ExpectedResponses))
                {
                    NetMQMessage raw = await dealer.ReceiveMultipartMessageAsync(
                        timeout: req.Timeout,
                        cancellationToken: cancellationToken
                    );
                    _logger.Verbose(
                        "A raw message ({FrameCount} frames) has replied.",
                        raw.FrameCount
                    );
                    Message reply = Message.Parse(
                        raw,
                        true,
                        _appProtocolVersion,
                        _trustedAppProtocolVersionSigners,
                        _differentAppProtocolVersionEncountered);
                    _logger.Debug(
                        "A reply has parsed: {Reply} from {ReplyRemote}",
                        reply,
                        reply.Remote
                    );

                    result.Add(reply);
                }

                if (req.ExpectedResponses > 0)
                {
                    Protocol.ReceiveMessage(result[0]);
                }

                tcs.TrySetResult(result);
            }
            catch (DifferentAppProtocolVersionException dapve)
            {
                tcs.TrySetException(dapve);
            }
            catch (TimeoutException te)
            {
                tcs.TrySetException(te);
            }

            _logger.Verbose(
                "Request {Message}({RequestId}) processed in {TimeSpan}.",
                req.Message,
                req.Id,
                DateTimeOffset.UtcNow - startedTime);
        }

        private void CheckStarted()
        {
            if (!Running)
            {
                throw new NoSwarmContextException("Swarm hasn't started yet.");
            }
        }

        private string ToNetMQAddress(BoundPeer peer)
        {
            return string.IsNullOrEmpty(_socks5Proxy) ?
                $"tcp://{peer.EndPoint.Host}:{peer.EndPoint.Port}" :
                $"socks5://{_socks5Proxy};{peer.EndPoint.Host}:{peer.EndPoint.Port}";
        }

        private async Task CreatePermission(BoundPeer peer)
        {
            var cts = new CancellationTokenSource();
            IPAddress[] ips;

            // Cancellation After 2.5 sec
            cts.CancelAfter(2500);
            if (peer.PublicIPAddress is null)
            {
                string peerHost = peer.EndPoint.Host;
                if (IPAddress.TryParse(peerHost, out IPAddress asIp))
                {
                    ips = new[] { asIp };
                }
                else
                {
                    ips = await Dns.GetHostAddressesAsync(peerHost);
                }
            }
            else
            {
                ips = new[] { peer.PublicIPAddress };
            }

            try
            {
                foreach (IPAddress ip in ips)
                {
                    var ep = new IPEndPoint(ip, peer.EndPoint.Port);
                    if (IPAddress.IsLoopback(ip))
                    {
                        // This translation is only used in test case because a
                        // seed node exposes loopback address as public address to
                        // other node in test case
                        ep = await _turnClient.GetMappedAddressAsync(cts.Token);
                    }

                    // FIXME Can we really ignore IPv6 case?
                    if (ip.AddressFamily.Equals(AddressFamily.InterNetwork))
                    {
                        await _turnClient.CreatePermissionAsync(ep, cts.Token);
                    }
                }
            }
            catch (TaskCanceledException tce)
            {
                if (cts.IsCancellationRequested)
                {
                    _logger.Debug($"Timeout occurred during {nameof(CreatePermission)}: {tce}");
                }
                else
                {
                    throw;
                }
            }
            catch (Exception e) when (e is SocketException || e is ObjectDisposedException)
            {
                _logger.Error($"Unexpected error occurred during {nameof(CreatePermission)}: {e}");

                using (await _turnClientMutex.LockAsync(_cancellationToken))
                {
                    if (!(_turnClient is null))
                    {
                        _logger.Debug("Trying to reconnect to the TURN server...");

                        _turnClient.Dispose();
                        _turnCancellationTokenSource.Cancel();
                        await Task.WhenAny(_turnTasks);
                        await InitializeTurnClient();
                    }
                }
            }
        }

        private async Task InitializeTurnClient()
        {
            _logger.Debug("Initializing TURN client...");
            _turnCancellationTokenSource = new CancellationTokenSource();
            _turnClient = await IceServer.CreateTurnClient(_iceServers);

            _publicIPAddress = (await _turnClient.GetMappedAddressAsync(_cancellationToken))
                .Address;
            _behindNAT = await _turnClient.IsBehindNAT(_cancellationToken);

            if (_behindNAT)
            {
                IPEndPoint turnEp = await _turnClient.AllocateRequestAsync(
                    TurnAllocationLifetime,
                    _cancellationToken
                );
                _endPoint = new DnsEndPoint(turnEp.Address.ToString(), turnEp.Port);
                _logger.Debug($"TURN Endpoint: {_endPoint}");

                _turnTasks = BindMultipleProxies(
                    _listenPort.Value, 3, _turnCancellationTokenSource.Token);
                _turnTasks.Add(RefreshAllocate(_turnCancellationTokenSource.Token));
                _turnTasks.Add(RefreshPermissions(_turnCancellationTokenSource.Token));
            }

            _logger.Debug("TURN client is initialized.");
        }

        private async Task RefreshTableAsync(
            TimeSpan period,
            TimeSpan maxAge,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(period, cancellationToken);
                    await Protocol.RefreshTableAsync(maxAge, cancellationToken);
                    await Protocol.CheckReplacementCacheAsync(cancellationToken);

                    ImmutableHashSet<Address> peerAddresses =
                        Peers.Select(p => p.Address).ToImmutableHashSet();
                    foreach (var address in _dealers.Keys)
                    {
                        if (!peerAddresses.Contains(address) &&
                            _dealers.TryGetValue(address, out DealerSocket removed))
                        {
                            //remove but not dispose it will be cleaned up by runtime later
                            _dealers.TryRemove(address, out DealerSocket removed2);
                            //dispose was causing errors down the line in concurent code
                            //removed.Dispose();
                        }
                    }
                }
                catch (OperationCanceledException e)
                {
                    _logger.Warning(e, $"{nameof(RefreshTableAsync)}() is cancelled.");
                    throw;
                }
                catch (Exception e)
                {
                    var msg = "Unexpected exception occurred during " +
                        $"{nameof(RefreshTableAsync)}(): {{0}}";
                    _logger.Warning(e, msg, e);
                }
            }
        }

        private async Task RebuildConnectionAsync(
            TimeSpan period,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Protocol.RebuildConnectionAsync(cancellationToken);
                    await Task.Delay(period, cancellationToken);
                }
                catch (OperationCanceledException e)
                {
                    _logger.Warning(e, $"{nameof(RebuildConnectionAsync)}() is cancelled.");
                    throw;
                }
                catch (Exception e)
                {
                    var msg = "Unexpected exception occurred during " +
                              $"{nameof(RebuildConnectionAsync)}(): {{0}}";
                    _logger.Warning(e, msg, e);
                }
            }
        }

        private Task RunPoller(NetMQPoller poller) =>
            Task.Factory.StartNew(
                () =>
                {
                    // Ignore NetMQ related exceptions during NetMQPoller.Run() to stabilize
                    // tests.
                    try
                    {
                        poller.Run();
                    }
                    catch (TerminatingException)
                    {
                        _logger.Error($"TerminatingException occurred during poller.Run()");
                    }
                    catch (ObjectDisposedException)
                    {
                        _logger.Error(
                            $"ObjectDisposedException occurred during poller.Run()"
                        );
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            );

        private List<Task> BindMultipleProxies(
            int listenPort,
            int count,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Enumerable.Range(1, count)
                .Select(x => _turnClient.BindProxies(listenPort, cancellationToken))
                .ToList();
        }

        private readonly struct MessageRequest
        {
            private readonly int _retried;

            public MessageRequest(
                in Guid id,
                Message message,
                BoundPeer peer,
                DateTimeOffset requestedTime,
                in TimeSpan? timeout,
                in int expectedResponses,
                TaskCompletionSource<IEnumerable<Message>> taskCompletionSource)
                : this(
                      id,
                      message,
                      peer,
                      requestedTime,
                      timeout,
                      expectedResponses,
                      taskCompletionSource,
                      0
                    )
            {
            }

            internal MessageRequest(
                in Guid id,
                Message message,
                BoundPeer peer,
                DateTimeOffset requestedTime,
                in TimeSpan? timeout,
                in int expectedResponses,
                TaskCompletionSource<IEnumerable<Message>> taskCompletionSource,
                int retried)
            {
                Id = id;
                Message = message;
                Peer = peer;
                RequestedTime = requestedTime;
                Timeout = timeout;
                ExpectedResponses = expectedResponses;
                TaskCompletionSource = taskCompletionSource;
                _retried = retried;
            }

            public Guid Id { get; }

            public Message Message { get; }

            public BoundPeer Peer { get; }

            public DateTimeOffset RequestedTime { get; }

            public TimeSpan? Timeout { get; }

            public int ExpectedResponses { get; }

            public TaskCompletionSource<IEnumerable<Message>> TaskCompletionSource { get; }

            public bool Retryable => _retried < 10;

            public MessageRequest Retry()
            {
                return new MessageRequest(
                    Id,
                    Message,
                    Peer,
                    RequestedTime,
                    Timeout,
                    ExpectedResponses,
                    TaskCompletionSource,
                    _retried + 1
                );
            }
        }
    }
}

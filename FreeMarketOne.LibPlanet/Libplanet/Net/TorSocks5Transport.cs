using FreeMarketOne.Tor;
using FreeMarketOne.Tor.Exceptions;
using FreeMarketOne.Tor.Models.Fields.OctetFields;
using FreeMarketOne.Tor.TorOverTcp.Models.Fields;
using FreeMarketOne.Tor.TorOverTcp.Models.Messages;
using Libplanet.Crypto;
using Libplanet.Net.Messages;
using Libplanet.Net.Protocols;
using NetMQ;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Libplanet.Net
{
    //FMONECHANGE created new transport
    internal class TorSocks5Transport : ITransport
    {
        private readonly PrivateKey _privateKey;
        private readonly AppProtocolVersion _appProtocolVersion;
        private readonly IImmutableSet<PublicKey> _trustedAppProtocolVersionSigners;
        private readonly string _host;
        private readonly ILogger _logger;
        private int? _listenPort;
        private DnsEndPoint _endPoint;
        private CancellationTokenSource _runtimeCancellationTokenSource;
        private CancellationToken _cancellationToken;
        private TorClientPool _clientPool;
        private int findConcurrency = 3;
        public event EventHandler<PeerStateChangeEventArgs> PeerStateChangeEvent;
        private event EventHandler<ReceivedRequestEventArgs> ProcessMessageHandler;
        private DifferentAppProtocolVersionEncountered _differentAppProtocolVersionEncountered;

        private TorSocks5Manager _torSocs5Manager;
        private TotServer _server;
        private static TorControlClient torControlClient;
        private TorProcessManager _torProcessManager;
        private readonly AsyncLock _circuitLoadingMutex;
        private RoutingTable _routingTable;
        public TorSocks5Transport(
            PrivateKey privateKey,
            AppProtocolVersion appProtocolVersion,
            IImmutableSet<PublicKey> trustedAppProtocolVersionSigners,
            RoutingTable routingTable,
            string host,
            int? listenPort,
            DifferentAppProtocolVersionEncountered differentAppProtocolVersionEncountered,
            EventHandler<ReceivedRequestEventArgs> processMessageHandler,
            ILogger logger,
            TorSocks5Manager torSocs5Manager,
            TorProcessManager torProcessManager,
            EventHandler<PeerStateChangeEventArgs> peerStateChangeHandler = null)
        {
            Running = false;

            _privateKey = privateKey;
            _appProtocolVersion = appProtocolVersion;
            _trustedAppProtocolVersionSigners = trustedAppProtocolVersionSigners;
            _host = host;
            _listenPort = listenPort;
            _differentAppProtocolVersionEncountered = differentAppProtocolVersionEncountered;
            _torSocs5Manager = torSocs5Manager;
            _routingTable = routingTable;
            ProcessMessageHandler = processMessageHandler;

            if (_host != null && _listenPort is int listenPortAsInt)
            {
                _endPoint = new DnsEndPoint(_host, listenPortAsInt);
            }

            _logger = logger;

            _runtimeCancellationTokenSource = new CancellationTokenSource();

            _clientPool = new TorClientPool(_logger, torSocs5Manager);

            Protocol = new KademliaProtocol(
                _routingTable,
                this,
                _privateKey.ToAddress(),
                findConcurrency,
                null,
                _clientPool,
                peerStateChangeHandler);

            PeerStateChangeEvent = peerStateChangeHandler;
            _torProcessManager = torProcessManager;
            _circuitLoadingMutex = new AsyncLock();
            try
            {
                if (torControlClient == null)
                {
                    torControlClient = new TorControlClient("127.0.0.1", 9051, "Let'sUpdateThisLaterProgramatically");
                }
            }
            catch (Exception e)
            {
                _logger.Information($"Clould not construct TorControlClient on swarm {_listenPort} error {e}");
            }

        }

        internal IProtocol Protocol { get; }

        public BoundPeer AsPeer => new BoundPeer(_privateKey.PublicKey, _endPoint);

        public DateTimeOffset? LastMessageTimestamp { get; private set; }

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



        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            if (Running)
            {
                //throw new SwarmException("Swarm is already running.");
                return;
            }

            Running = true;
            _cancellationToken = cancellationToken;
            List<Task> tasks = new List<Task>();

            tasks.Add(WaitForCirquit(_cancellationToken));
            tasks.Add(RefreshTableAsync(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10), _cancellationToken));
            tasks.Add(RebuildConnectionAsync(TimeSpan.FromMinutes(30), _cancellationToken));
            await await Task.WhenAny(tasks);
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (Running)
            {
                throw new SwarmException("Swarm is already running.");
            }
            _cancellationToken = cancellationToken;

            var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), _listenPort.Value);

            _server = new TotServer(endPoint);
            _logger.Information($"Listening on {_listenPort}");

            await _server.StartAsync();
            _server.RequestArrived += RequestArrived;

            try
            {
                await torControlClient.Init().ConfigureAwait(false);
                await WaitForCirquit(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.Information($"Failed initializing TorControlClient on swarm {_listenPort} experienced error {e}");
            }

            return;
        }

        public async Task WaitForCirquit(CancellationToken cancellationToken = default)
        {
            try
            {
                using (await _circuitLoadingMutex.LockAsync(cancellationToken).ConfigureAwait(false))
                {
                    bool hasCircuit = false;
                    while (!hasCircuit)
                    {
                        hasCircuit = await torControlClient.IsCircuitEstablishedAsync(cancellationToken);

                        if (!hasCircuit)
                        {
                            _logger.Information($"TOR cirquit is NOT ESTABLISHED, changing cirquit and waiting to acquire.");
                            await torControlClient.ChangeCircuitAsync();
                            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Information($"WaitForCirquit on swarm {_listenPort} experienced error {e}");
            }

            return;
        }

        public async Task StopAsync(TimeSpan waitFor, CancellationToken cancellationToken = default)
        {
            if (Running)
            {
                await Task.Delay(waitFor, cancellationToken);
                await _server.StopAsync();
                _clientPool.ShutDown();
                Running = false;
            }
        }

        protected void OnPeerStateChange(PeerStateChangeEventArgs e)
        {
            PeerStateChangeEvent?.Invoke(this, e);
        }

        public async Task BroadcastMessage<T>(BoundPeer except, T message)
        {
            try
            {
                if (except == null)
                {
                    except = AsPeer;
                }
                List<BoundPeer> peers = _routingTable.PeersToBroadcast(except.Address).ToList();
                _logger.Debug("Broadcasting message: {Message} as {AsPeer}", message, AsPeer);
                _logger.Debug("Peers to broadcast: {PeersCount}", peers.Count);

                Envelope envelope = new Envelope(AsPeer, _appProtocolVersion);
                envelope.Initialize<T>(_privateKey, message);

                foreach (BoundPeer peer in peers)
                {
                    if (peer.EndPoint.Port == _listenPort)//hopefuly this is isolated by instance
                    {
                        _logger.Debug($"Broadcasting {message} to : {peer}");
                        var response = await SendRequest(peer, envelope, TimeSpan.FromMinutes(1));
                    }
                }
            }
            catch (Exception exc)
            {
                _logger.Error(exc, $"Unexpected error occurred during {nameof(BroadcastMessage)}(). {exc}");
                throw;
            }
        }

        public void RequestArrived(object sender, TotRequest request)
        {
            byte[] content = request.Content.Content;
            var envelope = UnPackEnvelope(content);
            TotClient client = sender as TotClient;
            LastMessageTimestamp = DateTimeOffset.UtcNow;
            try
            {
                //validation throws exceptions so no need to nest in IFs for now
                if (envelope.IsValid(_appProtocolVersion, _trustedAppProtocolVersionSigners, _differentAppProtocolVersionEncountered))
                {
                    var notification = new ReceivedRequestEventArgs()
                    {
                        Client = client,
                        Request = request,
                        Peer = envelope.FromPeer,
                        MessageType = envelope.MessageType,
                        Envelope = envelope
                    };
                    _routingTable.AddPeer(envelope.FromPeer);
                    //response must be delegated to swarm as transport is dull
                    ProcessMessageHandler?.Invoke(this, notification);

                    PeerStateChangeEventArgs args = new PeerStateChangeEventArgs
                    {
                        Peer = envelope.FromPeer,
                        Change = PeerStateChange.Joined
                    };
                    OnPeerStateChange(args);
                }
            }
            catch (DifferentAppProtocolVersionException)
            {
                var differentVersion = new DifferentVersion();
                _logger.Debug($"Message from {envelope.FromPeer} with different {envelope.Version} version received.");
                client.RespondBadRequestAsync(request.MessageId, $"Wrong version, expected:{_appProtocolVersion}")
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }

            catch (Exception ex)
            {
                _logger.Error(ex, $"An unexpected exception occurred processing {envelope.MessageType} Error: {ex}");
            }

        }

        private async Task<TotContent> SendRequest(BoundPeer peer, Envelope envelope, TimeSpan timeout, int attempt = 0)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            PooledClient pooledClient = null;
            if (timeout.TotalMilliseconds < 1000)
            {
                timeout = TimeSpan.FromSeconds(30);
            }

            _logger.Debug($"Processing request:{envelope.MessageType} Peer:[{peer.EndPoint.Host}:{peer.EndPoint.Port}] configured timeout ms:[{timeout.TotalMilliseconds}]");

            try
            {
                pooledClient = await _clientPool.Get(peer);
                var client = pooledClient.Client;
                TotRequest request = new TotRequest("Message", new TotContent(PackEnvelope(envelope)))
                {
                    MessageId = TotMessageId.Random,
                    MessageType = TotMessageType.Request
                };

                var response = await client.RequestAsync(request, (int)timeout.TotalMilliseconds);

                PeerStateChangeEventArgs args = new PeerStateChangeEventArgs
                {
                    Peer = peer,
                    Change = PeerStateChange.TwoWayDialogConfirmed
                };
                OnPeerStateChange(args);

                _clientPool.Return(pooledClient);
                sw.Stop();
                _logger.Debug($"Processed request:{envelope.MessageType} Peer:[{pooledClient.Host}:{pooledClient.Port}] configured timeout ms:[{timeout.TotalMilliseconds}] elapsed time {sw.ElapsedMilliseconds}");
                return response;
            }
            catch (ConnectionException cex)
            {
                _logger.Debug($"Tor connection exception:{cex}");
                //restart tor /// cex
                try
                {
                    if (_torProcessManager != null)//&& !_torProcessManager.IsTorRunning()
                    {
                        _logger.Debug($"Tor is down, starting ...");
                        _torProcessManager.ReStart();
                        _logger.Debug($"Tor started.");
                    }
                }
                catch (Exception e)
                {
                    _logger.Debug($"Failure starting Tor {e}.");
                }

                throw cex;
            }
            catch (TorSocks5FailureResponseException socks5Error)
            {
                //useful info
                //https://github.com/torproject/torspec/blob/master/control-spec.txt
                //https://iphelix.medium.com/hacking-the-tor-control-protocol-fb844db6a606
                if (torControlClient.Running)
                {
                    try
                    {
                        var ct = new CancellationToken();
                        var hasCircuit = await torControlClient.IsCircuitEstablishedAsync();
                        var streaminfo = await torControlClient.SendCommandAsync("GETINFO stream-status", ct);
                        _logger.Debug($"Connection to peer Peer:[{peer.EndPoint.Host}:{peer.EndPoint.Port}] failed with Error:{socks5Error} Status: hasCircuit:{hasCircuit} StreamInfo:{streaminfo}");
                    }
                    catch (Exception)
                    {
                        //swallow;
                    }

                }
                else
                {
                    _logger.Debug($"Connection to peer Peer:[{peer.EndPoint.Host}:{peer.EndPoint.Port}] failed with Error:{socks5Error}");
                }
                throw socks5Error;
            }
            catch (Exception e)
            {
                _logger.Error($"Error procesing:{envelope.MessageType} Peer:[{peer.EndPoint.Host}:{peer.EndPoint.Port}] configured timeout ms:[{timeout.Milliseconds}] elapsed time {sw.ElapsedMilliseconds} Error:{e}");
                if (pooledClient != null)
                {
                    _clientPool.KillIfUnhealthy(pooledClient);
                }

                if (attempt < 2)
                {
                    attempt++;
                    return await SendRequest(peer, envelope, timeout, attempt);
                }
                throw e;
            }
        }

        public async Task<TResponse> SendMessageWithReplyAsync<TRequest, TResponse>(BoundPeer peer, TRequest message, TimeSpan? timeout)
        {
            Envelope envelope = new Envelope(AsPeer, _appProtocolVersion);
            envelope.Initialize(_privateKey, message);
            if (!timeout.HasValue)
            {
                // 1 hour is infinity
                timeout = TimeSpan.FromHours(1);
            }
            var response = await SendRequest(peer, envelope, timeout.Value).ConfigureAwait(false);
            var rEnvelope = UnPackEnvelope(response.Content);
            rEnvelope.IsValid(_appProtocolVersion, _trustedAppProtocolVersionSigners, _differentAppProtocolVersionEncountered);
            return rEnvelope.GetBody<TResponse>();
        }


        public void ReplyMessage<TResponse>(TotRequest request, TotClient client, TResponse responseMessage)
        {
            Envelope responseEnvelope = new Envelope(AsPeer, _appProtocolVersion);
            responseEnvelope.Initialize<TResponse>(_privateKey, responseMessage);
            TotContent response = new TotContent(PackEnvelope(responseEnvelope));
            Stopwatch sw = new Stopwatch();
            sw.Start();
            client.RespondSuccessAsync(request.MessageId, response).ConfigureAwait(false).GetAwaiter().GetResult();
            _logger.Debug($"Sent a response for {responseEnvelope.MessageType} time taken to respond {sw.ElapsedMilliseconds} ms.");
        }


        public bool Running { get; set; }
        //{
        //    get => _server?.TcpListener != null;            
        //}

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
                    await Task.Delay(period, cancellationToken);
                    await Protocol.RebuildConnectionAsync(2, cancellationToken);
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

        //public string Trace() => Protocol is null ? string.Empty : Protocol.Trace();

        public Task WaitForRunningAsync()
        {
            while (true)
            {
                if (Running) break;
                Task.Delay(TimeSpan.FromSeconds(1));
            }
            return Task.CompletedTask;
        }


        //public async Task CheckAllPeersAsync(CancellationToken cancellationToken, TimeSpan? timeout)
        //{
        //    var kp = (KademliaProtocol)Protocol;
        //    await kp.CheckAllPeersAsync(cancellationToken, timeout);
        //}

        public void Dispose()
        {
            try
            {
                _clientPool.ShutDown();
            }
            catch (Exception)
            {
                //swallow
            }
        }

        public Envelope UnPackEnvelope(byte[] bytes)
        {
            string json = Unzip(bytes);
            var e = JsonConvert.DeserializeObject<Envelope>(json);
            //if (e.MessageType != MessageType.Ping && e.MessageType != MessageType.Pong) {
            //    _logger.Debug($"JSON received:{json}");
            //}
            return e;
        }

        public byte[] PackEnvelope(Envelope envelope)
        {
            string json = JsonConvert.SerializeObject(envelope, Formatting.None);
            //if (envelope.MessageType != MessageType.Ping && envelope.MessageType != MessageType.Pong)
            //{
            //    _logger.Debug($"JSON sent:{json}");
            //}
            return Zip(json);
        }

        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public static byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    //msi.CopyTo(gs);
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public static string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    //gs.CopyTo(mso);
                    CopyTo(gs, mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        private MessageType ExtractMessageType(string json)
        {
            MessageType retval = MessageType.Unrecognized;
            try
            {
                byte jsonvalue = 0;
                JsonTextReader reader = new JsonTextReader(new StringReader(json));
                //normally message type is first element so this should be fairly efficient
                while (reader.Read())
                {
                    if (reader.Value != null && reader.TokenType == JsonToken.PropertyName && reader.Value.Equals("MessageType"))
                    {
                        //advance by 1
                        reader.Read();
                        jsonvalue = Convert.ToByte(reader.Value);
                        break;
                    }
                }

                if (jsonvalue > 0)
                {
                    retval = (MessageType)jsonvalue;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Unable to parse message", e);
            }

            return retval;
        }
    }
}

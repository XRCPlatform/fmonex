using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FreeMarketOne.Tor;
using FreeMarketOne.Tor.TorOverTcp.Models.Fields;
using FreeMarketOne.Tor.TorOverTcp.Models.Messages;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Net.Messages;
using Libplanet.Net.Protocols;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Serilog;

namespace Libplanet.Tests.Net.Protocols
{
    internal class TestTransport : ITransport
    {
        //        private static readonly PrivateKey VersionSigner = new PrivateKey();
        //        private static readonly AppProtocolVersion AppProtocolVersion =
        //            AppProtocolVersion.Sign(VersionSigner, 1);

        //        private readonly Dictionary<Address, TestTransport> _transports;
        //        private readonly ILogger _logger;
        //        private readonly ConcurrentDictionary<byte[], TotMessageId> _peersToReply;
        //        private readonly ConcurrentDictionary<byte[], Envelope> _replyToReceive;
        //        private readonly List<string> _ignoreTestMessageWithData;
        //        private readonly PrivateKey _privateKey;
        //        private readonly Random _random;
        //        private readonly bool _blockBroadcast;

        //        private CancellationTokenSource _swarmCancellationTokenSource;
        //        private TimeSpan _networkDelay;

        //        public TestTransport(
        //            Dictionary<Address, TestTransport> transports,
        //            PrivateKey privateKey,
        //            bool blockBroadcast,
        //            int? tableSize,
        //            int? bucketSize,
        //            TimeSpan? networkDelay)
        //        {
        //            _privateKey = privateKey;
        //            _blockBroadcast = blockBroadcast;
        //            var loggerId = _privateKey.ToAddress().ToHex();
        //            _logger = Log.ForContext<TestTransport>()
        //                .ForContext("Address", loggerId);

        //            _peersToReply = new ConcurrentDictionary<byte[], TotMessageId>();
        //            _replyToReceive = new ConcurrentDictionary<byte[], Envelope>();
        //            ReceivedMessages = new ConcurrentBag<Envelope>();
        //            MessageReceived = new AsyncAutoResetEvent();
        //            _transports = transports;
        //            _transports[privateKey.ToAddress()] = this;
        //            _networkDelay = networkDelay ?? TimeSpan.Zero;

        //            _ignoreTestMessageWithData = new List<string>();
        //            _random = new Random();
        //            Protocol = new KademliaProtocol(
        //                this,
        //                Address,
        //                AppProtocolVersion,
        //                null,
        //                null,
        //                _logger,
        //                tableSize,
        //                bucketSize
        //            );
        //        }

        //        public AsyncAutoResetEvent MessageReceived { get; }

        //        public Address Address => _privateKey.ToAddress();

        //        public Peer AsPeer => new BoundPeer(
        //            _privateKey.PublicKey,
        //            new DnsEndPoint("localhost", 1234));

        //        public IEnumerable<BoundPeer> Peers => Protocol.Peers;

        //        public DateTimeOffset? LastMessageTimestamp { get; private set; }

        //        internal ConcurrentBag<Envelope> ReceivedMessages { get; }

        //        internal IProtocol Protocol { get; }

        //        internal bool Running => !(_swarmCancellationTokenSource is null);

        //        BoundPeer ITransport.AsPeer => throw new NotImplementedException();

        //        public void Dispose()
        //        {
        //        }

        //#pragma warning disable CS1998 // Method need to implement ITransport but it isn't be async
        //        public async Task StartAsync(
        //            CancellationToken cancellationToken = default(CancellationToken))
        //        {
        //            _logger.Debug("Starting transport of {Peer}.", AsPeer);
        //            _swarmCancellationTokenSource = new CancellationTokenSource();
        //        }
        //#pragma warning restore CS1998

        //        public async Task RunAsync(
        //            CancellationToken cancellationToken = default(CancellationToken))
        //        {
        //            CancellationToken token = cancellationToken.Equals(CancellationToken.None)
        //                ? _swarmCancellationTokenSource.Token
        //                : CancellationTokenSource
        //                    .CreateLinkedTokenSource(
        //                        _swarmCancellationTokenSource.Token, cancellationToken)
        //                    .Token;
        //            //await ProcessRuntime(token);
        //            await Task.Delay(1);
        //        }

        //        public async Task StopAsync(
        //            TimeSpan waitFor,
        //            CancellationToken cancellationToken = default(CancellationToken))
        //        {
        //            _logger.Debug("Stopping transport of {Peer}.", AsPeer);
        //            _swarmCancellationTokenSource.Cancel();
        //            await Task.Delay(waitFor, cancellationToken);
        //        }

        //        public async Task BootstrapAsync(
        //            IEnumerable<Peer> bootstrapPeers,
        //            TimeSpan? pingSeedTimeout = null,
        //            TimeSpan? findPeerTimeout = null,
        //            int depth = 3,
        //            CancellationToken cancellationToken = default(CancellationToken))
        //        {
        //            IEnumerable<BoundPeer> peers = bootstrapPeers.OfType<BoundPeer>();

        //            await BootstrapAsync(
        //                peers,
        //                pingSeedTimeout,
        //                findPeerTimeout,
        //                depth,
        //                cancellationToken);
        //        }

        //#pragma warning disable S4457 // Cannot split the method since method is in interface
        //        public async Task BootstrapAsync(
        //            IEnumerable<BoundPeer> bootstrapPeers,
        //            TimeSpan? pingSeedTimeout = null,
        //            TimeSpan? findPeerTimeout = null,
        //            int depth = 3,
        //            CancellationToken cancellationToken = default(CancellationToken))
        //        {
        //            if (!Running)
        //            {
        //                throw new SwarmException("Start swarm before use.");
        //            }

        //            if (bootstrapPeers is null)
        //            {
        //                throw new ArgumentNullException(nameof(bootstrapPeers));
        //            }

        //            await Protocol.BootstrapAsync(
        //                bootstrapPeers.ToImmutableList(),
        //                pingSeedTimeout,
        //                findPeerTimeout,
        //                Kademlia.MaxDepth,
        //                cancellationToken);
        //        }
        //#pragma warning restore S4457 // Cannot split the method since method is in interface

        //        public Task AddPeersAsync(
        //            IEnumerable<Peer> peers,
        //            TimeSpan? timeout,
        //            CancellationToken cancellationToken = default(CancellationToken))
        //        {
        //            if (!Running)
        //            {
        //                throw new SwarmException("Start swarm before use.");
        //            }

        //            if (peers is null)
        //            {
        //                throw new ArgumentNullException(nameof(peers));
        //            }

        //            async Task DoAddPeersAsync()
        //            {
        //                try
        //                {
        //                    KademliaProtocol kp = (KademliaProtocol)Protocol;

        //                    var tasks = new List<Task>();
        //                    foreach (var peer in peers)
        //                    {
        //                        if (peer is BoundPeer boundPeer)
        //                        {
        //                            tasks.Add(kp.PingAsync(
        //                                boundPeer,
        //                                timeout: timeout,
        //                                cancellationToken: cancellationToken));
        //                        }
        //                    }

        //                    _logger.Verbose("Trying to ping all {PeersNumber} peers.", tasks.Count);
        //                    await Task.WhenAll(tasks);
        //                    _logger.Verbose("Update complete.");
        //                }
        //                catch (DifferentAppProtocolVersionException)
        //                {
        //                    _logger.Debug("Different version encountered during AddPeersAsync().");
        //                }
        //                catch (PingTimeoutException)
        //                {
        //                    var msg =
        //                        $"Timeout occurred during {nameof(AddPeersAsync)}() after {timeout}.";
        //                    _logger.Debug(msg);
        //                    throw new TimeoutException(msg);
        //                }
        //                catch (TaskCanceledException)
        //                {
        //                    _logger.Debug($"Task is cancelled during {nameof(AddPeersAsync)}().");
        //                }
        //                catch (Exception e)
        //                {
        //                    _logger.Error(
        //                        e,
        //                        $"Unexpected exception occurred during {nameof(AddPeersAsync)}().");
        //                    throw;
        //                }
        //            }

        //            return DoAddPeersAsync();
        //        }

        //        //public void SendPing(Peer target, TimeSpan? timeSpan = null)
        //        //{
        //        //    if (!Running)
        //        //    {
        //        //        throw new SwarmException("Start swarm before use.");
        //        //    }

        //        //    if (!(target is BoundPeer boundPeer))
        //        //    {
        //        //        throw new ArgumentException("Target peer does not have endpoint.", nameof(target));
        //        //    }

        //        //    Task.Run(() =>
        //        //    {
        //        //        _ = (Protocol as KademliaProtocol).PingAsync(
        //        //            boundPeer,
        //        //            timeSpan,
        //        //            default(CancellationToken));
        //        //    });
        //        //}

        //        //public void BroadcastTestMessage(Address? except, string data)
        //        //{
        //        //    if (!Running)
        //        //    {
        //        //        throw new SwarmException("Start swarm before use.");
        //        //    }

        //        //    var message = new TestMessage(data) { Remote = AsPeer };
        //        //    _ignoreTestMessageWithData.Add(data);
        //        //    BroadcastMessage(except, message);
        //        //}

        //        //public void BroadcastMessage(BoundPeer? except, Message message)
        //        //{
        //        //    var peers = Protocol.PeersToBroadcast(except.Address).ToList();
        //        //    var peersString = string.Join(", ", peers.Select(peer => peer.Address));
        //        //    _logger.Debug(
        //        //        "Broadcasting test message {Data} to {Count} peers which are: {Peers}",
        //        //        ((TestMessage)message).Data,
        //        //        peers.Count,
        //        //        peersString);
        //        //    foreach (var peer in peers)
        //        //    {
        //        //        _requests.Add(new Request()
        //        //        {
        //        //            RequestTime = DateTimeOffset.UtcNow,
        //        //            Message = message,
        //        //            Target = peer,
        //        //        });
        //        //    }
        //        //}

        //#pragma warning disable S4457 // Cannot split the method since method is in interface
        //        //public async Task<Message> SendMessageWithReplyAsync(
        //        //    BoundPeer peer,
        //        //    Message message,
        //        //    TimeSpan? timeout)
        //        //{
        //        //    if (!Running)
        //        //    {
        //        //        throw new SwarmException("Start swarm before use.");
        //        //    }

        //        //    if (!(peer is BoundPeer boundPeer))
        //        //    {
        //        //        throw new ArgumentException("Target peer is not a BoundPeer.");
        //        //    }

        //        //    message.Remote = AsPeer;
        //        //    var bytes = new byte[10];
        //        //    _random.NextBytes(bytes);
        //        //    message.Identity = _privateKey.ToAddress().ByteArray.Concat(bytes).ToArray();
        //        //    var sendTime = DateTimeOffset.UtcNow;
        //        //    _logger.Debug("Adding request of {Message} of {Identity}.", message, message.Identity);
        //        //    await _requests.AddAsync(
        //        //        new Request()
        //        //        {
        //        //            RequestTime = sendTime,
        //        //            Message = message,
        //        //            Target = peer,
        //        //        }, cancellationToken);

        //        //    while (!cancellationToken.IsCancellationRequested &&
        //        //           !_replyToReceive.ContainsKey(message.Identity))
        //        //    {
        //        //        if (DateTimeOffset.UtcNow - sendTime > (timeout ?? TimeSpan.MaxValue))
        //        //        {
        //        //            _logger.Error(
        //        //                "Reply of {Message} of {identity} did not received in " +
        //        //                "expected timespan {TimeSpan}.",
        //        //                message,
        //        //                message.Identity,
        //        //                timeout ?? TimeSpan.MaxValue);
        //        //            throw new TimeoutException(
        //        //                $"Timeout occurred during {nameof(SendMessageWithReplyAsync)}().");
        //        //        }

        //        //        await Task.Delay(10, cancellationToken);
        //        //    }

        //        //    if (cancellationToken.IsCancellationRequested)
        //        //    {
        //        //        throw new OperationCanceledException(
        //        //            $"Operation is canceled during {nameof(SendMessageWithReplyAsync)}().");
        //        //    }

        //        //    if (_replyToReceive.TryRemove(message.Identity, out Message reply))
        //        //    {
        //        //        _logger.Debug(
        //        //            "Received reply {Reply} of message with identity {identity}.",
        //        //            reply,
        //        //            message.Identity);
        //        //        LastMessageTimestamp = DateTimeOffset.UtcNow;
        //        //        ReceivedMessages.Add(reply);
        //        //        Protocol.ReceiveMessage(reply);
        //        //        MessageReceived.Set();
        //        //        return reply;
        //        //    }
        //        //    else
        //        //    {
        //        //        _logger.Error(
        //        //            "Unexpected error occurred during " +
        //        //            $"{nameof(SendMessageWithReplyAsync)}()");
        //        //        throw new SwarmException();
        //        //    }
        //        //}
        //#pragma warning restore S4457 // Cannot split the method since method is in interface



        //        public void ReplyMessage(TotRequest message)
        //        {
        //            if (!Running)
        //            {
        //                throw new SwarmException("Start swarm before use.");
        //            }

        //            _logger.Debug("Replying {Message}...", message);

        //            Task.Run(async () =>
        //            {
        //                await Task.Delay(_networkDelay);
        //                //_transports[_peersToReply[message.MessageId]].ReceiveReply(message);

        //            });
        //        }

        //        public void ReplyMessage<TResponse>(TotRequest request, TotClient client, TResponse responseMessage)
        //        {
        //            Envelope responseEnvelope = new Envelope();
        //            responseEnvelope.Initialize<TResponse>(_privateKey, responseMessage);
        //            TotContent response = new TotContent(PackEnvelope(responseEnvelope));
        //            client.RespondSuccessAsync(request.MessageId, response).ConfigureAwait(false).GetAwaiter().GetResult();
        //        }

        //        public static Envelope UnPackEnvelope(byte[] bytes)
        //        {
        //            string json = Unzip(bytes);
        //            return JsonConvert.DeserializeObject<Envelope>(json);
        //        }

        //        public static byte[] PackEnvelope(Envelope envelope)
        //        {
        //            string json = JsonConvert.SerializeObject(envelope, Formatting.None);
        //            return Zip(json);
        //        }

        //        public static void CopyTo(Stream src, Stream dest)
        //        {
        //            byte[] bytes = new byte[4096];

        //            int cnt;

        //            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
        //            {
        //                dest.Write(bytes, 0, cnt);
        //            }
        //        }

        //        public static byte[] Zip(string str)
        //        {
        //            var bytes = Encoding.UTF8.GetBytes(str);

        //            using (var msi = new MemoryStream(bytes))
        //            using (var mso = new MemoryStream())
        //            {
        //                using (var gs = new GZipStream(mso, CompressionMode.Compress))
        //                {
        //                    //msi.CopyTo(gs);
        //                    CopyTo(msi, gs);
        //                }

        //                return mso.ToArray();
        //            }
        //        }

        //        public static string Unzip(byte[] bytes)
        //        {
        //            using (var msi = new MemoryStream(bytes))
        //            using (var mso = new MemoryStream())
        //            {
        //                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
        //                {
        //                    //gs.CopyTo(mso);
        //                    CopyTo(gs, mso);
        //                }

        //                return Encoding.UTF8.GetString(mso.ToArray());
        //            }
        //        }

        //        public async Task WaitForTestMessageWithData(
        //            string data,
        //            CancellationToken token = default(CancellationToken))
        //        {
        //            if (!Running)
        //            {
        //                throw new SwarmException("Start swarm before use.");
        //            }

        //            while (!token.IsCancellationRequested && !ReceivedTestMessageOfData(data))
        //            {
        //                await Task.Delay(10, token);
        //            }
        //        }

        //        public bool ReceivedTestMessageOfData(string data)
        //        {
        //            if (!Running)
        //            {
        //                throw new SwarmException("Start swarm before use.");
        //            }

        //            return ReceivedMessages.OfType<TestMessage>().Any(msg => msg.Data == data);
        //        }

        //        //private void ReceiveMessage(Message message)
        //        //{
        //        //    if (_swarmCancellationTokenSource.IsCancellationRequested)
        //        //    {
        //        //        return;
        //        //    }

        //        //    if (!(message.Remote is BoundPeer boundPeer))
        //        //    {
        //        //        throw new ArgumentException("Sender of message is not a BoundPeer.");
        //        //    }

        //        //    if (message is TestMessage testMessage)
        //        //    {
        //        //        if (_ignoreTestMessageWithData.Contains(testMessage.Data))
        //        //        {
        //        //            _logger.Debug("Ignore received test message {Data}.", testMessage.Data);
        //        //        }
        //        //        else
        //        //        {
        //        //            _logger.Debug("Received test message with {Data}.", testMessage.Data);
        //        //            _ignoreTestMessageWithData.Add(testMessage.Data);
        //        //            // If this transport is blocked for testing, do not broadcast.
        //        //            if (!_blockBroadcast)
        //        //            {
        //        //                BroadcastTestMessage(testMessage.Remote.Address, testMessage.Data);
        //        //            }
        //        //        }
        //        //    }
        //        //    else
        //        //    {
        //        //        _peersToReply[message.Identity] = boundPeer.Address;
        //        //    }

        //        //    if (message is Ping)
        //        //    {
        //        //        ReplyMessage(new Pong);
        //        //    }

        //        //    LastMessageTimestamp = DateTimeOffset.UtcNow;
        //        //    ReceivedMessages.Add(message);
        //        //    Protocol.ReceiveMessage(message);
        //        //    MessageReceived.Set();
        //        //}

        //        //private void ReceiveReply(TotRequest message)
        //        //{
        //        //    _replyToReceive[message.MessageId] = message;
        //        //}

        //        //private async Task ProcessRuntime(CancellationToken cancellationToken)
        //        //{
        //        //    while (!cancellationToken.IsCancellationRequested)
        //        //    {
        //        //        Request req = await _requests.TakeAsync(cancellationToken);

        //        //        if (req.RequestTime + _networkDelay <= DateTimeOffset.UtcNow)
        //        //        {
        //        //            _logger.Debug(
        //        //                "Send {Message} with {Identity} to {Peer}.",
        //        //                req.Message,
        //        //                req.Message.Identity,
        //        //                req.Target);
        //        //            _transports[req.Target.Address].ReceiveMessage(req.Message);
        //        //        }
        //        //        else
        //        //        {
        //        //            await _requests.AddAsync(req, cancellationToken);
        //        //            await Task.Delay(10, cancellationToken);
        //        //        }
        //        //    }
        //        //}

        //        public Task<TResponse> SendMessageWithReplyAsync<TRequest, TResponse>(BoundPeer peer, TRequest message, TimeSpan? timeout)
        //        {
        //            throw new NotImplementedException();
        //        }

        //        public Task BroadcastMessage<T>(BoundPeer except, T message)
        //        {
        //            throw new NotImplementedException();
        //        }
        public BoundPeer AsPeer => throw new NotImplementedException();

        public IEnumerable<BoundPeer> Peers => throw new NotImplementedException();

        public DateTimeOffset? LastMessageTimestamp => throw new NotImplementedException();

        public Task BootstrapAsync(IEnumerable<BoundPeer> bootstrapPeers, TimeSpan? pingSeedTimeout, TimeSpan? findNeighborsTimeout, int depth, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task BroadcastMessage<T>(BoundPeer except, T message)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void ReplyMessage<TResponse>(TotRequest request, TotClient client, TResponse responseMessage)
        {
            throw new NotImplementedException();
        }

        public Task RunAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<TResponse> SendMessageWithReplyAsync<TRequest, TResponse>(BoundPeer peer, TRequest message, TimeSpan? timeout)
        {
            throw new NotImplementedException();
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(TimeSpan waitFor, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

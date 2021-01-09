using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.P2P.Models;
using FreeMarketOne.Tor;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.Net;
using MihaZupan;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using FreeMarketOne.Tor.Exceptions;

namespace FreeMarketOne.P2P
{
    public class OnionSeedsManager : IOnionSeedsManager, IDisposable
    {
        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped
        /// </summary>
        private long running;
        private static HttpClient _httpClient = null;

        public bool IsRunning => Interlocked.Read(ref running) == 1;
        private ILogger _logger { get; set; }
        private EndPoint _torSocks5EndPoint { get; set; }
        private string _torOnionEndPoint { get; set; }
        private string _appVersion { get; set; }

        private bool _listenersUseTor;

        public List<OnionSeedPeer> OnionSeedPeers { get; set; }
        private IAsyncLoopFactory _asyncLoopFactory { get; set; }
        private CancellationTokenSource _cancellationToken { get; set; }
        private TorProcessManager _torProcessManager { get; set; }
        private IPAddress _serverPublicAddress { get; set; }
        private string _serverOnionAddress { get; set; }

        public Swarm<BaseAction> BaseSwarm { get; set; }
        public Swarm<MarketAction> MarketSwarm { get; set; }

        public OnionSeedsManager(
            IBaseConfiguration configuration,
            TorProcessManager torManager,
            IPAddress serverPublicAddress)
        {
            _logger = Log.Logger.ForContext<OnionSeedsManager>();
            _logger.Information("Initializing Onion Seeds Manager");

            _torSocks5EndPoint = configuration.TorEndPoint;
            _torOnionEndPoint = configuration.OnionSeedsEndPoint;
            _appVersion = configuration.Version;
            _listenersUseTor = configuration.ListenersUseTor;

            _torProcessManager = torManager;
            _serverPublicAddress = serverPublicAddress;
            _serverOnionAddress = torManager.TorOnionEndPoint;

            _asyncLoopFactory = new AsyncLoopFactory(_logger);

            _cancellationToken = new CancellationTokenSource();

            OnionSeedPeers = new List<OnionSeedPeer>();
        }

        public bool IsOnionSeedsManagerRunning()
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

        public void Start()
        {
            _logger.Information(string.Format("Loading of: {0} by Tor Gate: {1}", _torOnionEndPoint, _torSocks5EndPoint));

            //we have to load first onion seeds synchroniously
            ProcessOnionSeeds();

            if (_listenersUseTor)
            {
                _logger.Information("Warming Tor onion service ...");
                bool isOk3 = WarmTorOnionServicetWithTcpServer(9113).GetAwaiter().GetResult();
                var isOk4 = WarmTorOnionServicetWithTcpServer(9114).GetAwaiter().GetResult();
                _logger.Information($"Warmed Tor circuit: {isOk3} && {isOk4}");
            }

            IAsyncLoop periodicLogLoop = this._asyncLoopFactory.Run("OnionPeriodicCheck", (cancellation) =>
            {
                ProcessOnionSeeds();

                return Task.CompletedTask;
            },
            _cancellationToken.Token,
            repeatEvery: TimeSpans.Minute,
            startAfter: TimeSpans.Minute);

            _logger.Information(string.Format("Done: {0}", OnionSeedPeers.Count));

            Interlocked.Exchange(ref running, 1);
        }

        private static HttpClient GetHttpClient(string uri)
        {
            if (_httpClient == null)
            {
                var proxy = new HttpToSocks5Proxy("127.0.0.1", 9050);
                var handler = new HttpClientHandler { Proxy = proxy };
                HttpClient httpClient = new HttpClient(handler, true);
                httpClient.BaseAddress = new Uri(uri);
                httpClient.Timeout = TimeSpan.FromSeconds(3);
                httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
                {
                    Public = true
                };
                _httpClient = httpClient;
            }

            return _httpClient;
        }

        private void ProcessOnionSeeds()
        {
            var dateTimeUtc = DateTime.UtcNow;

            StringBuilder periodicCheckLog = new StringBuilder();

            periodicCheckLog.AppendLine("======Onion Seed Status Check====== " + dateTimeUtc.ToString(CultureInfo.InvariantCulture) + " agent " + _appVersion);
            periodicCheckLog.AppendLine("My Tor EndPoint " + _torProcessManager.TorOnionEndPoint);

            var isTorRunning = _torProcessManager.IsTorRunningAsync().Result;
            if (isTorRunning)
            {
                var httpClient = GetHttpClient(_torOnionEndPoint);
                var response = httpClient.GetAsync("").ConfigureAwait(false).GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    var onionsStreamData = response.Content.ReadAsStreamAsync().Result;

                    using (StreamReader sr = new StreamReader(onionsStreamData))
                    {
                        while (sr.Peek() >= 0)
                        {
                            var onion = sr.ReadLine();

                            _logger.Information(string.Format("Parsing: {0}", onion));

                            if (IsOnionAddressValid(onion))
                            {
                                var parts = onion.Split(":");
                                var newOnionSeed = new OnionSeedPeer();

                                newOnionSeed.UrlTor = parts[0];
                                newOnionSeed.PortTor = int.Parse(parts[1]);
                                newOnionSeed.UrlBlockChain = parts[2];
                                newOnionSeed.PortBlockChainBase = int.Parse(parts[3]);
                                newOnionSeed.PortBlockChainMaster = int.Parse(parts[4]);
                                newOnionSeed.PublicKeyHex = parts[5];

                                if (!OnionSeedPeers.Exists(a => a.PublicKeyHex == newOnionSeed.PublicKeyHex))
                                    OnionSeedPeers.Add(newOnionSeed);

                                _logger.Information(string.Format("Valid source: {0} Port: {1}", newOnionSeed.UrlTor, newOnionSeed.PortTor));
                            }
                        }
                    }

                    if ((OnionSeedPeers != null) && (OnionSeedPeers.Any()) && (BaseSwarm != null) && (MarketSwarm != null))
                    {
                        foreach (var itemSeedPeer in OnionSeedPeers)
                        {
                            Task.Run(async () =>
                            {
                                try
                                {
                                    await AddSeedsToBaseSwarmAsPeer(itemSeedPeer);
                                    await AddSeedsToMarketSwarmAsPeer(itemSeedPeer);
                                }
                                catch (Exception e)
                                {
                                    _logger.Error(string.Format("Cant add seed to swarm {0}", e));
                                }
                            });
                        }
                    }

                    if (BaseSwarm != null)
                        periodicCheckLog.AppendLine(string.Format("Swarm Base Peers: {0}", BaseSwarm.Peers.Count()));

                    if (MarketSwarm != null)
                        periodicCheckLog.AppendLine(string.Format("Swarm Market Peers: {0}", MarketSwarm.Peers.Count()));
                }
                else
                {
                    periodicCheckLog.AppendLine(string.Format("OnionSeed list {0} is down!", _torOnionEndPoint));
                }
            }
            else
            {
                periodicCheckLog.AppendLine("Tor is down!");
            }

            Console.WriteLine(periodicCheckLog.ToString());
        }

        private bool IsOnionAddressValid(string onionSeedPeer)
        {
            var result = true;

            if (!onionSeedPeer.Contains(":")) result = false;
            if (!onionSeedPeer.Contains("onion")) result = false;

            var parts = onionSeedPeer.Split(":");
            if (parts.Length < 3) result = false;

            //ignore me
            if ((!string.IsNullOrEmpty(_serverOnionAddress)) && (parts[0] == _serverOnionAddress)) result = false;
            if ((_serverPublicAddress != null) && (parts[2] == _serverPublicAddress.ToString())) result = false;

            return result;
        }

        /// <summary>
        /// Adding peers to base swarms
        /// </summary>
        /// <param name="seed">Seed info</param>
        /// <returns></returns>
        private async Task AddSeedsToBaseSwarmAsPeer(OnionSeedPeer seed)
        {
            if (BaseSwarm.Peers.Any())
            {
                var publicKey = new PublicKey(ByteUtil.ParseHex(seed.PublicKeyHex));
                var boundPeer = new BoundPeer(publicKey, new DnsEndPoint(seed.UrlBlockChain, seed.PortBlockChainBase));

                _logger.Information(string.Format("Adding base peer pubkey: {0}", boundPeer.ToString()));

                var exist = BaseSwarm.Peers.FirstOrDefault(p => p.PublicKey == publicKey);
                if (exist == null)
                    await BaseSwarm.AddPeersAsync(
                        new[] { boundPeer },
                        TimeSpan.FromMilliseconds(30000),
                        _cancellationToken.Token);
            }
        }

        /// <summary>
        /// Adding peers to market swarms
        /// </summary>
        /// <param name="seed">Seed info</param>
        /// <returns></returns>
        private async Task AddSeedsToMarketSwarmAsPeer(OnionSeedPeer seed)
        {
            if (MarketSwarm.Peers.Any())
            {
                var publicKey = new PublicKey(ByteUtil.ParseHex(seed.PublicKeyHex));
                var boundPeer = new BoundPeer(publicKey, new DnsEndPoint(seed.UrlBlockChain, seed.PortBlockChainBase));

                _logger.Information(string.Format("Adding market peer pubkey: {0}", boundPeer.ToString()));

                var exist = MarketSwarm.Peers.FirstOrDefault(p => p.PublicKey == publicKey);
                if (exist == null)
                    await MarketSwarm.AddPeersAsync(
                        new[] { boundPeer },
                        TimeSpan.FromMilliseconds(30000),
                        _cancellationToken.Token);
            }
        }

        private async Task<bool> WarmTorOnionServicetWithTcpServer(int port)
        {
            var duration = TimeSpan.FromSeconds(60);

            // Server task
            Task.Run(() =>
            {
                var server = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
                server.Start();

                var bytes = new byte[] {0x1, 0x2};
                _logger.Information($"Accepting warm up stream for {port}");
                TcpClient client = server.AcceptTcpClient();
                NetworkStream stream = client.GetStream();
                _logger.Information($"Got warm up stream from Tor using {port}");
                stream.Write(bytes, 0, 2);

                client.Close();
                server.Stop();

                _logger.Information($"Closing warm up server on {port}");
            });

            var sleepDuration = TimeSpan.FromSeconds(10);
            var stopwatch = Stopwatch.StartNew();
            var connected = false;

            do
            {
                TorSocks5Client client = new TorSocks5Client(_torSocks5EndPoint);

                try
                {
                    await client.ConnectAsync().ConfigureAwait(false);
                    _logger.Information($"Connected to Tor on {port}");
                    await client.HandshakeAsync(true).ConfigureAwait(false);
                    _logger.Information($"Handshake with Tor on {port}");
                    await client.ConnectToDestinationAsync(_serverOnionAddress, port).ConfigureAwait(false);
                    _logger.Information($"Warm up connected? {client.IsConnected}");
                    if (client.IsConnected)
                    {
                        _logger.Information($"Warm up test for {port}");
                        connected = true;
                        var bytes = new byte[2];
                        int i = client.Stream.Read(bytes, 0, 2);

                        return i == 2 && bytes[0] == 0x1 && bytes[1] == 0x2;
                    }
                }
                catch (TorSocks5FailureResponseException ex)
                {

                    Console.WriteLine(ex);
                    if (duration - stopwatch.Elapsed > sleepDuration)
                    {
                        Thread.Sleep(sleepDuration);
                    }
                }
                finally
                {
                    client.Dispose();
                }
            } while (!connected && stopwatch.Elapsed < duration);

            return false;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref running, 2);

            _cancellationToken.Cancel();

            Interlocked.Exchange(ref running, 3);
        }
    }
}

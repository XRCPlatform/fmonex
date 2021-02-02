using FreeMarketOne.DataStructure;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.P2P.Models;
using FreeMarketOne.Tor;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.Net;
using MihaZupan;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        private static bool _listenersUseTor;
        public List<OnionSeedPeer> OnionSeedPeers { get; set; }
        private IAsyncLoopFactory _asyncLoopFactory { get; set; }
        private CancellationTokenSource _cancellationToken { get; set; }
        private TorProcessManager _torProcessManager { get; set; }
        private IPAddress _serverPublicAddress { get; set; }
        private string _serverOnionAddress { get; set; }
        private List<string> _onionSeeds { get; set; }
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
            _onionSeeds = configuration.OnionSeeds;

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

        public async Task Start()
        {
            _logger.Information(string.Format("Loading of: {0} by Tor Gate: {1}", _torOnionEndPoint, _torSocks5EndPoint));

            await ProcessOnionSeedsAsync();

            IAsyncLoop periodicLogLoop = this._asyncLoopFactory.Run("OnionPeriodicCheck", (cancellation) =>
            {
                return ProcessOnionSeedsAsync();
            },
            _cancellationToken.Token,
            repeatEvery: TimeSpans.TenMinutes,
            startAfter: TimeSpans.Minute);

            _logger.Information(string.Format("Done: {0}", OnionSeedPeers.Count));

            Interlocked.Exchange(ref running, 1);
        }

        private static HttpClient GetHttpClient(string uri)
        {
            if (_httpClient == null)
            {
                var handler = new HttpClientHandler { };
                if (_listenersUseTor)
                {
                    var proxy = new HttpToSocks5Proxy("127.0.0.1", 9050);
                    handler = new HttpClientHandler { Proxy = proxy };
                }

                HttpClient httpClient = new HttpClient(handler);
                httpClient.BaseAddress = new Uri(uri);
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
                {
                    Public = true
                };
                _httpClient = httpClient;
            }

            return _httpClient;
        }

        private void AddOnionSeedString(string onion)
        {
            if (IsOnionAddressValid(onion))
            {
                var parts = onion.Split(":");
                var newOnionSeed = new OnionSeedPeer
                {
                    UrlTor = parts[0],
                    PortTor = int.Parse(parts[1]),
                    UrlBlockChain = parts[2],
                    PortBlockChainBase = int.Parse(parts[3]),
                    PortBlockChainMaster = int.Parse(parts[4]),
                    PublicKeyHex = parts[5]
                };

                if (!OnionSeedPeers.Exists(a => a.PublicKeyHex == newOnionSeed.PublicKeyHex))
                    OnionSeedPeers.Add(newOnionSeed);

                _logger.Information(string.Format("Valid source: {0} Port: {1}", newOnionSeed.UrlTor, newOnionSeed.PortTor));
            }
        }

        private async Task ProcessOnionSeedsAsync()
        {
            var dateTimeUtc = DateTime.UtcNow;

            StringBuilder periodicCheckLog = new StringBuilder();

            periodicCheckLog.AppendLine("======Onion Seed Status Check====== " + dateTimeUtc.ToString(CultureInfo.InvariantCulture) + " agent " + _appVersion);
            periodicCheckLog.AppendLine("My Tor EndPoint " + _torProcessManager.TorOnionEndPoint);

            // Get local seeds
            if (_onionSeeds != null)
            {
                foreach (string onionSeed in _onionSeeds)
                {
                    AddOnionSeedString(onionSeed);
                }
            }

            var isTorRunning = _torProcessManager.IsTorRunning();

            if (isTorRunning)
            {
                //remote seeds are optional, subject to potential DDOS attacks, single point of failures 
                //however offer some way if distributing new seeds when needed and possible
                try
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
                                AddOnionSeedString(onion);

                            }
                        }
                    }
                    else
                    {
                        periodicCheckLog.AppendLine(string.Format("OnionSeed list {0} is down!", _torOnionEndPoint));
                    }
                }
                catch (Exception e)
                {
                    periodicCheckLog.AppendLine(string.Format("OnionSeed request exception:", e.ToString()));
                }

                if ((OnionSeedPeers != null) && (OnionSeedPeers.Any()) && (BaseSwarm != null) && (MarketSwarm != null))
                {
                    List<Task> tasks = new List<Task>();
                    foreach (var itemSeedPeer in OnionSeedPeers)
                    {
                        tasks.Add(AddSeedsToBaseSwarmAsPeer(itemSeedPeer));
                        tasks.Add(AddSeedsToMarketSwarmAsPeer(itemSeedPeer));
                    }
                    try
                    {
                        Task.WaitAll(tasks.ToArray());
                    }
                    catch (AggregateException ae)
                    {
                        foreach (var e in ae.Flatten().InnerExceptions)
                        {
                            _logger.Error($"Error adding seeds to swarm {e.Message}");
                        }
                        
                    }
                }

                if (BaseSwarm != null)
                {
                    periodicCheckLog.AppendLine(string.Format("Swarm Base Peers: {0}", BaseSwarm.Peers.Count()));
                }

                if (MarketSwarm != null)
                {
                    periodicCheckLog.AppendLine(string.Format("Swarm Market Peers: {0}", MarketSwarm.Peers.Count()));
                }

            }
            else
            {
                periodicCheckLog.AppendLine("Tor is down!");
            }

            periodicCheckLog.AppendLine($"Current Onion Seeds: ");
            foreach (OnionSeedPeer peer in OnionSeedPeers)
            {
                _ = periodicCheckLog.AppendLine($"{peer.UrlTor}:{peer.PortTor}:{peer.UrlBlockChain}:{peer.PortBlockChainBase}:{peer.PortBlockChainMaster}:{peer.PublicKeyHex}");
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
            var publicKey = new PublicKey(ByteUtil.ParseHex(seed.PublicKeyHex));
            var boundPeer = new BoundPeer(publicKey, new DnsEndPoint(seed.UrlBlockChain, seed.PortBlockChainBase));

            if (BaseSwarm.Peers.Any())
            {
                var exist = BaseSwarm.Peers.FirstOrDefault(p => p.PublicKey == publicKey);
                if (exist != null)
                {
                    return;
                }
            }

            _logger.Information(string.Format("Adding base peer pubkey: {0}", boundPeer.ToString()));

            await BaseSwarm.AddPeersAsync(
                    new[] { boundPeer },
                    TimeSpan.FromSeconds(10),
                    _cancellationToken.Token).ConfigureAwait(false);

        }

        /// <summary>
        /// Adding peers to market swarms
        /// </summary>
        /// <param name="seed">Seed info</param>
        /// <returns></returns>
        private async Task AddSeedsToMarketSwarmAsPeer(OnionSeedPeer seed)
        {

            var publicKey = new PublicKey(ByteUtil.ParseHex(seed.PublicKeyHex));
            var boundPeer = new BoundPeer(publicKey, new DnsEndPoint(seed.UrlBlockChain, seed.PortBlockChainBase));

            if (MarketSwarm.Peers.Any())
            {
                var exist = MarketSwarm.Peers.FirstOrDefault(p => p.PublicKey == publicKey);
                if (exist != null)
                {
                    return;
                }
            }

            _logger.Information(string.Format("Adding market peer pubkey: {0}", boundPeer.ToString()));
            await MarketSwarm.AddPeersAsync(
                    new[] { boundPeer },
                    TimeSpan.FromSeconds(10),
                    _cancellationToken.Token).ConfigureAwait(false);

        }


        public void Dispose()
        {
            Interlocked.Exchange(ref running, 2);

            _cancellationToken.Cancel();

            Interlocked.Exchange(ref running, 3);
        }
    }
}

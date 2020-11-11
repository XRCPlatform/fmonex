using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.P2P.Models;
using FreeMarketOne.Tor;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.Net;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
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
        public bool IsRunning => Interlocked.Read(ref running) == 1;
        private ILogger _logger { get; set; }
        private EndPoint _torSocks5EndPoint { get; set; }
        private string _torOnionEndPoint { get; set; }
        private string _appVersion { get; set; }
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

            IAsyncLoop periodicLogLoop = this._asyncLoopFactory.Run("OnionPeriodicCheck", (cancellation) =>
            {
                var dateTimeUtc = DateTime.UtcNow;

                StringBuilder periodicCheckLog = new StringBuilder();

                periodicCheckLog.AppendLine("======Onion Seed Status Check====== " + dateTimeUtc.ToString(CultureInfo.InvariantCulture) + " agent " + _appVersion);
                periodicCheckLog.AppendLine("My Tor EndPoint " + _torProcessManager.TorOnionEndPoint);

                var isTorRunning = _torProcessManager.IsTorRunningAsync().Result;
                if (isTorRunning)
                {
                    var torHttpClient = new TorHttpClient(new Uri(_torOnionEndPoint), _torSocks5EndPoint);
                    var response = torHttpClient.SendAsync(HttpMethod.Get, string.Empty).Result;

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
                                newOnionSeed.SecretKeyHex = parts[5];

                                if (!OnionSeedPeers.Exists(a => a.SecretKeyHex == newOnionSeed.SecretKeyHex))
                                    OnionSeedPeers.Add(newOnionSeed);

                                _logger.Information(string.Format("Valid source: {0} Port: {1}", newOnionSeed.UrlTor, newOnionSeed.PortTor));
                            }
                        }
                    }

                    if ((OnionSeedPeers != null) && (OnionSeedPeers.Any()))
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
                    periodicCheckLog.AppendLine("Tor is down!");
                }

                Console.WriteLine(periodicCheckLog.ToString());

                return Task.CompletedTask;
            },
            _cancellationToken.Token,
            repeatEvery: TimeSpans.Minute,
            startAfter: TimeSpans.Ms100);

            _logger.Information(string.Format("Done: {0}", OnionSeedPeers.Count));

            Interlocked.Exchange(ref running, 1);
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
            if ((BaseSwarm != null) && (BaseSwarm.Peers.Any()))
            {
                var publicKey = new PublicKey(ByteUtil.ParseHex(seed.SecretKeyHex));
                var boundPeer = new BoundPeer(publicKey, new DnsEndPoint(seed.UrlBlockChain, seed.PortBlockChainBase), default(AppProtocolVersion));

                _logger.Information(string.Format("Adding base peer pubkey: {0}", boundPeer.ToString()));

                var exist = BaseSwarm.Peers.FirstOrDefault(p => p.PublicKey == publicKey);
                if (exist == null)
                    await BaseSwarm.AddPeersAsync(
                        new[] { boundPeer },
                        TimeSpan.FromMilliseconds(5000),
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
            if ((MarketSwarm != null) && (MarketSwarm.Peers.Any()))
            {
                var publicKey = new PublicKey(ByteUtil.ParseHex(seed.SecretKeyHex));
                var boundPeer = new BoundPeer(publicKey, new DnsEndPoint(seed.UrlBlockChain, seed.PortBlockChainBase), default(AppProtocolVersion));

                _logger.Information(string.Format("Adding market peer pubkey: {0}", boundPeer.ToString()));

                var exist = MarketSwarm.Peers.FirstOrDefault(p => p.PublicKey == publicKey);
                if (exist == null)
                    await MarketSwarm.AddPeersAsync(
                        new[] { boundPeer },
                        TimeSpan.FromMilliseconds(5000),
                        _cancellationToken.Token);
            }
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref running, 2);

            _cancellationToken.Cancel();

            Interlocked.Exchange(ref running, 3);
        }
    }
}

using FreeMarketOne.DataStructure;
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

        public List<Swarm<BaseAction>> Swarms { get; set; }

        public OnionSeedsManager(IBaseConfiguration configuration, TorProcessManager torManager)
        {
            _logger = Log.Logger.ForContext<OnionSeedsManager>();
            _logger.Information("Initializing Onion Seeds Manager");

            _torSocks5EndPoint = configuration.TorEndPoint;
            _torOnionEndPoint = configuration.OnionSeedsEndPoint;
            _appVersion = configuration.Version;

            _torProcessManager = torManager;

            _asyncLoopFactory = new AsyncLoopFactory(_logger);

            _cancellationToken = new CancellationTokenSource();

            Swarms = new List<Swarm<BaseAction>>();
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
            OnionSeedPeers = new List<OnionSeedPeer>();

            _logger.Information(string.Format("Prepairing loading of: {0} by Tor Gate: {1}", _torOnionEndPoint, _torSocks5EndPoint));

            try
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

                            OnionSeedPeers.Add(newOnionSeed);

                            _logger.Information(string.Format("Valid source: {0} Port: {1}", newOnionSeed.UrlTor, newOnionSeed.PortTor));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("Unexpected Error: {0}", e.Message));
            }

            _logger.Information(string.Format("Done: {0}", OnionSeedPeers.Count));

            Interlocked.Exchange(ref running, 1);

            StartListener();
            StartPeriodicCheck();
        }

        private static bool IsOnionAddressValid(string onionSeedPeer)
        {
            var result = true;

            if (!onionSeedPeer.Contains(":")) result = false;
            if (!onionSeedPeer.Contains("onion")) result = false;

            var parts = onionSeedPeer.Split(":");
            if (parts.Length < 3) result = false;

            return result;
        }

        private void StartPeriodicCheck()
        {
            IAsyncLoop periodicLogLoop = this._asyncLoopFactory.Run("OnionPeriodicCheck", (cancellation) =>
            {
                var dateTimeUtc = DateTime.UtcNow;

                StringBuilder periodicCheckLog = new StringBuilder();

                periodicCheckLog.AppendLine("======Onion Seed Status Check====== " + dateTimeUtc.ToString(CultureInfo.InvariantCulture) + " agent " + _appVersion);
                periodicCheckLog.AppendLine("My Tor EndPoint " + _torProcessManager.TorOnionEndPoint);

                var isTorRunning = _torProcessManager.IsTorRunningAsync().Result;
                if (isTorRunning)
                {
                    foreach (var itemSeed in OnionSeedPeers)
                    {
                        if (_torProcessManager.TorOnionEndPoint != itemSeed.UrlTor) //ignore me
                        {
                            var resultLog = string.Format("Checking peer {0}:{1}", itemSeed.UrlTor, itemSeed.PortTor);
                            _logger.Information(resultLog);

                            itemSeed.State = OnionSeedPeer.OnionSeedStates.Offline;

                            try
                            {
                                var isOnionSeedRunning = _torProcessManager.IsOnionSeedRunningAsync(itemSeed.UrlTor, itemSeed.PortTor).Result;
                                if (isOnionSeedRunning)
                                {
                                    itemSeed.State = OnionSeedPeer.OnionSeedStates.Online;

                                    Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await AddSeedsToSwarmsAsPeer(itemSeed);
                                        }
                                        catch (Exception e)
                                        {
                                            _logger.Error(string.Format("Cant add seed to swarm {0}", e));
                                        }
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex.Message);
                            }

                            periodicCheckLog.AppendLine(string.Format("{0} {1}", resultLog, itemSeed.State));
                        }
                        else
                        {
                            itemSeed.State = OnionSeedPeer.OnionSeedStates.Online;
                        }
                    }

                    foreach (var itemSwarm in Swarms)
                    {
                        var type = itemSwarm.GetType().GetGenericArguments()[0];
                        periodicCheckLog.AppendLine(string.Format("Swarm {0} Peers: {1}", type.Name, itemSwarm.Peers.Count()));
                    }
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
                startAfter: TimeSpans.TenSeconds);
        }

        /// <summary>
        /// Adding peers to swarms
        /// </summary>
        /// <param name="seed">Seed info</param>
        /// <returns></returns>
        private async Task AddSeedsToSwarmsAsPeer(OnionSeedPeer seed)
        {
            foreach (var itemSwarm in Swarms)
            {
                var publicKey = new PublicKey(ByteUtil.ParseHex(seed.SecretKeyHex));

                if (itemSwarm.Peers.Any())
                {
                    var exist = itemSwarm.Peers.FirstOrDefault(p => p.PublicKey == publicKey);
                    if (exist != null) continue;
                }

                var boundPeer = new BoundPeer(publicKey, new DnsEndPoint(seed.UrlBlockChain, seed.PortBlockChainBase), default(AppProtocolVersion));
              
                _logger.Information(string.Format("Adding peer pubkey: {0}", boundPeer.ToString()));
                
                await itemSwarm.AddPeersAsync(
                    new[] { boundPeer }, 
                    TimeSpan.FromMilliseconds(5000), 
                    _cancellationToken.Token);
            }
        }

        /// <summary>
        /// Online listener for onion checking
        /// </summary>
        private void StartListener()
        {
            Task.Run(() =>
            {

                TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 27272);
                listener.Start();

                while (true)
                {
                    Socket client = listener.AcceptSocket();

                    if (client.Connected)
                    {
                        byte[] b = new byte[65535];
                        int k = client.Receive(b);

                        ASCIIEncoding enc = new ASCIIEncoding();

                        client.Send(enc.GetBytes("FM.ONE EndPoint - " + DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) + " agent " + _appVersion));
                        client.Close();
                    }
                }
            });
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref running, 2);

            _cancellationToken.Cancel();

            Interlocked.Exchange(ref running, 3);
        }
    }
}

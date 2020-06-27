﻿using FreeMarketOne.DataStructure;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.P2P.Models;
using FreeMarketOne.Tor;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        private ILogger logger { get; set; }
        private EndPoint torSocks5EndPoint { get; set; }
        private string torOnionEndPoint { get; set; }
        private string appVersion { get; set; }
        public List<OnionSeedPeer> OnionSeedPeers { get; set; }
        private IAsyncLoopFactory asyncLoopFactory { get; set; }
        private CancellationTokenSource cancellationToken { get; set; }
        private TorProcessManager torProcessManager { get; set; }

        public OnionSeedsManager(IBaseConfiguration configuration, TorProcessManager torManager)
        {
            logger = Log.Logger.ForContext<OnionSeedsManager>();
            logger.Information("Initializing Onion Seeds Manager");

            torSocks5EndPoint = configuration.TorEndPoint;
            torOnionEndPoint = configuration.OnionSeedsEndPoint;
            appVersion = configuration.Version;

            torProcessManager = torManager;

            asyncLoopFactory = new AsyncLoopFactory(logger);

            cancellationToken = new CancellationTokenSource();
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

            logger.Information(string.Format("Prepairing loading of: {0} by Tor Gate: {1}", torOnionEndPoint, torSocks5EndPoint));

            try
            {
                var torHttpClient = new TorHttpClient(new Uri(torOnionEndPoint), torSocks5EndPoint);
                var response = torHttpClient.SendAsync(HttpMethod.Get, string.Empty).Result;

                var onionsStreamData = response.Content.ReadAsStreamAsync().Result;

                using (StreamReader sr = new StreamReader(onionsStreamData))
                {
                    while (sr.Peek() >= 0)
                    {
                        var onion = sr.ReadLine();

                        logger.Information(string.Format("Parsing: {0}", onion));

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

                            logger.Information(string.Format("Valid source: {0} Port: {1}", newOnionSeed.UrlTor, newOnionSeed.PortTor));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(string.Format("Unexpected Error: {0}", e.Message));
            }

            logger.Information(string.Format("Done: {0}", OnionSeedPeers.Count));

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
            IAsyncLoop periodicLogLoop = this.asyncLoopFactory.Run("OnionPeriodicCheck", (cancellation) =>
            {
                var dateTimeUtc = DateTime.UtcNow;

                StringBuilder periodicCheckLog = new StringBuilder();

                periodicCheckLog.AppendLine("======Onion Seed Status Check====== " + dateTimeUtc.ToString(CultureInfo.InvariantCulture) + " agent " + appVersion);
                periodicCheckLog.AppendLine("My Tor EndPoint " + torProcessManager.TorOnionEndPoint);

                var isTorRunning = torProcessManager.IsTorRunningAsync().Result;
                if (isTorRunning)
                {
                    foreach (var itemSeed in OnionSeedPeers)
                    {
                        if (torProcessManager.TorOnionEndPoint != itemSeed.UrlTor) //ignore me
                        {
                            var resultLog = string.Format("Checking {0} {1}", itemSeed.UrlTor, itemSeed.PortTor);
                            logger.Information(resultLog);

                            itemSeed.State = OnionSeedPeer.OnionSeedStates.Offline;

                            try
                            {
                                var isOnionSeedRunning = torProcessManager.IsOnionSeedRunningAsync(itemSeed.UrlTor, itemSeed.PortTor).Result;
                                if (isOnionSeedRunning)
                                {
                                    itemSeed.State = OnionSeedPeer.OnionSeedStates.Online;
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex.Message);
                            }

                            periodicCheckLog.AppendLine(string.Format("{0} {1}", resultLog, itemSeed.State));
                        }
                        else
                        {
                            itemSeed.State = OnionSeedPeer.OnionSeedStates.Online;
                        }
                    }
                }
                else
                {
                    periodicCheckLog.AppendLine("Tor is down!");
                }

                Console.WriteLine(periodicCheckLog.ToString());

                return Task.CompletedTask;
            },
                cancellationToken.Token,
                repeatEvery: TimeSpans.Minute,
                startAfter: TimeSpans.TenSeconds);
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

                        client.Send(enc.GetBytes("FM.ONE EndPoint - " + DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) + " agent " + appVersion));
                        client.Close();
                    }
                }
            });
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref running, 2);

            cancellationToken.Cancel();

            Interlocked.Exchange(ref running, 3);
        }
    }
}

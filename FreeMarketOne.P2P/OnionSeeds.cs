using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Extensions.Models;
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeMarketOne.P2P
{
    public class OnionSeedsManager : IDisposable
    {
        private ILogger logger { get; set; }
        private EndPoint torSocks5EndPoint { get; set; }
        private string torOnionEndPoint { get; set; }
        private string appVersion { get; set; }

        public List<OnionSeed> OnionSeeds { get; set; }

        private IAsyncLoopFactory asyncLoopFactory { get; set; }
        private CancellationTokenSource cancellationToken { get; set; }

        private TorProcessManager torProcessManager { get; set; }

        public OnionSeedsManager(Logger serverLogger, BaseConfiguration configuration, TorProcessManager torManager)
        {
            logger = serverLogger.ForContext<OnionSeedsManager>();
            logger.Information("Initializing Onion Seeds Manager");

            torSocks5EndPoint = configuration.TorEndPoint;
            torOnionEndPoint = configuration.OnionSeedsEndPoint;
            appVersion = configuration.Version;

            torProcessManager = torManager;

            asyncLoopFactory = new AsyncLoopFactory(logger);
        }

        public void GetOnions()
        {
            OnionSeeds = new List<OnionSeed>();

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
                            var newOnionSeed = new OnionSeed();

                            newOnionSeed.Url = parts[0];
                            newOnionSeed.Port = int.Parse(parts[1]);

                            OnionSeeds.Add(newOnionSeed);

                            logger.Information(string.Format("Valid source: {0} Port: {1}", newOnionSeed.Url, newOnionSeed.Port));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(string.Format("Unexpected Error: {0}", e.Message));
            }

            logger.Information(string.Format("Done: {0}", OnionSeeds.Count));
        }

        private static bool IsOnionAddressValid(string onionSeed)
        {
            var result = true;

            if (!onionSeed.Contains(":")) result = false;
            if (!onionSeed.Contains("onion")) result = false;

            return result;
        }

        public void StartPeriodicCheck()
        {
            cancellationToken = new CancellationTokenSource();

            IAsyncLoop periodicLogLoop = this.asyncLoopFactory.Run("OnionPeriodicCheck", (cancellation) =>
            {
                var dateTimeUtc = DateTime.UtcNow;

                StringBuilder periodicCheckLog = new StringBuilder();

                periodicCheckLog.AppendLine("======Onion Seed Check====== " + dateTimeUtc.ToString(CultureInfo.InvariantCulture) + " agent " + appVersion);

                var isTorRunning = torProcessManager.IsTorRunningAsync().Result;
                if (isTorRunning)
                {
                    foreach (var itemSeed in OnionSeeds)
                    {
                        var resultLog = string.Format("Checking {0} {1}", itemSeed.Url, itemSeed.Port);

                        var isOnionSeedRunning = torProcessManager.IsOnionSeedRunningAsync(itemSeed.Url, itemSeed.Port).Result;
                        if (isOnionSeedRunning)
                        {
                            itemSeed.State = OnionSeed.OnionSeedStates.Online;
                        }
                        else
                        {
                            itemSeed.State = OnionSeed.OnionSeedStates.Offline;
                        }

                        periodicCheckLog.AppendLine(string.Format("{0} {1}", resultLog, itemSeed.State));
                    }
                }
                else
                {
                    periodicCheckLog.AppendLine("Tor is down!");
                }

                logger.Information(periodicCheckLog.ToString());
                Console.WriteLine(periodicCheckLog.ToString());

                return Task.CompletedTask;
            },
                cancellationToken.Token,
                repeatEvery: TimeSpans.Minute,
                startAfter: TimeSpans.TenSeconds);
        }

        public void Dispose()
        {
            cancellationToken.Cancel();
        }
    }
}

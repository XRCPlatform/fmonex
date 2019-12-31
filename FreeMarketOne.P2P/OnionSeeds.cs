using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Extensions.Models;
using FreeMarketOne.P2P.Models;
using FreeMarketOne.Tor;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FreeMarketOne.P2P
{
    public class OnionSeedsManager : IDisposable
    {
        private ILogger logger { get; set; }
        private EndPoint torSocks5EndPoint { get; set; }
        private string torOnionEndPoint { get; set; }

        public List<OnionSeed> OnionSeeds { get; set; }

        private IAsyncLoopFactory asyncLoopFactory { get; set; }
        private CancellationTokenSource cancellationToken { get; set; }

        public OnionSeedsManager(Logger serverLogger, BaseConfiguration configuration)
        {
            logger = serverLogger.ForContext<OnionSeedsManager>();
            logger.Information("Initializing Onion Seeds Manager");

            torSocks5EndPoint = configuration.TorEndPoint;
            torOnionEndPoint = configuration.OnionSeedsEndPoint;

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
                //StringBuilder benchLogs = new StringBuilder();

                //benchLogs.AppendLine("======Node stats====== " + this.DateTimeProvider.GetUtcNow().ToString(CultureInfo.InvariantCulture) + " agent " +
                //                     this.ConnectionManager.Parameters.UserAgent);

                //// Display node stats grouped together.
                //foreach (var feature in this.Services.Features.OfType<INodeStats>())
                //    feature.AddNodeStats(benchLogs);

                //// Now display the other stats.
                //foreach (var feature in this.Services.Features.OfType<IFeatureStats>())
                //    feature.AddFeatureStats(benchLogs);

                //benchLogs.AppendLine();
                //benchLogs.AppendLine("======Connection======");
                //benchLogs.AppendLine(this.ConnectionManager.GetNodeStats());
                //this.logger.LogInformation(benchLogs.ToString());

                Console.WriteLine("Start sXXXX");

                return Task.CompletedTask;
            },
                cancellationToken.Token,
                repeatEvery: TimeSpans.FiveSeconds,
                startAfter: TimeSpans.FiveSeconds);
        }

        public void Dispose()
        {
            cancellationToken.Cancel();
        }
    }
}

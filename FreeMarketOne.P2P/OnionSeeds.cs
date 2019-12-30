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

namespace FreeMarketOne.P2P
{
    public class OnionSeedsManager
    {
        private ILogger Logger { get; set; }

        private EndPoint TorSocks5EndPoint { get; set; }

        private string TorOnionEndPoint { get; set; }

        public List<OnionSeed> OnionSeeds { get; set; }

        public OnionSeedsManager(Logger serverLogger, BaseConfiguration configuration)
        {
            Logger = serverLogger.ForContext<OnionSeedsManager>();
            Logger.Information("Initializing Onion Seeds Manager");

            TorSocks5EndPoint = configuration.TorEndPoint;
            TorOnionEndPoint = configuration.OnionSeedsEndPoint;
        }

        public void GetOnions()
        {
            OnionSeeds = new List<OnionSeed>();

            Logger.Information(string.Format("Prepairing loading of: {0} by Tor Gate: {1}", TorOnionEndPoint, TorSocks5EndPoint));

            try
            {
                var torHttpClient = new TorHttpClient(new Uri(TorOnionEndPoint), TorSocks5EndPoint);
                var response = torHttpClient.SendAsync(HttpMethod.Get, string.Empty).Result;

                var onionsStreamData = response.Content.ReadAsStreamAsync().Result;

                using (StreamReader sr = new StreamReader(onionsStreamData))
                {
                    while (sr.Peek() >= 0)
                    {
                        var onion = sr.ReadLine();

                        Logger.Information(string.Format("Parsing: {0}", onion));

                        if (IsOnionAddressValid(onion))
                        {
                            var parts = onion.Split(":");
                            var newOnionSeed = new OnionSeed();

                            newOnionSeed.Url = parts[0];
                            newOnionSeed.Port = int.Parse(parts[1]);

                            OnionSeeds.Add(newOnionSeed);

                            Logger.Information(string.Format("Valid source: {0} Port: {1}", newOnionSeed.Url, newOnionSeed.Port));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("Unexpected Error: {0}", e.Message));
            }

            Logger.Information(string.Format("Done: {0}", OnionSeeds.Count));
        }

        private static bool IsOnionAddressValid(string onionSeed)
        {
            var result = true;

            if (!onionSeed.Contains(":")) result = false;
            if (!onionSeed.Contains("onion")) result = false;

            return result;
        }
    }
}

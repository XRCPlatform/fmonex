using FreeMarketOne.Extensions.Models;
using FreeMarketOne.P2P.Models;
using FreeMarketOne.Tor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;

namespace FreeMarketOne.P2P
{
    public static class OnionSeeds
    {
        public static List<OnionSeed> GetOnions(BaseConfiguration configuration)
        {
            List<OnionSeed> onionSeeds = new List<OnionSeed>();

            var torHttpClient = new TorHttpClient(new Uri(configuration.OnionSeedsEndPoint), configuration.TorEndPoint);
            var response = torHttpClient.SendAsync(HttpMethod.Get, string.Empty).Result;

            var onionsStreamData = response.Content.ReadAsStreamAsync().Result;

            using (StreamReader sr = new StreamReader(onionsStreamData))
            {
                while (sr.Peek() >= 0)
                {
                    var onion = sr.ReadLine();

                    if (IsOnionAddressValid(onion))
                    {
                        var parts = onion.Split(":");
                        var newOnionSeed = new OnionSeed();

                        newOnionSeed.Url = parts[0];
                        newOnionSeed.Port = int.Parse(parts[1]);

                        onionSeeds.Add(newOnionSeed);
                    }
                }
            }

            return onionSeeds;
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

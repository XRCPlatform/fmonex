using FreeMarketOne.DataStructure;
using FreeMarketOne.Tor;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace FreeMarketOne.P2P
{
    public class IpHelper
    {
        private bool _useTor { get; set; }
        private EndPoint  _torEndPoint { get; set; }

        public IPAddress PublicIP { get; set; }
        public IpHelper(IBaseConfiguration configuration)
        {
            _useTor = configuration.ListenersUseTor;
            _torEndPoint = configuration.TorEndPoint;

            if (!_useTor)
            {
                PublicIP = GetIp();
            }
        }

        public IPAddress GetIp()
        {
            List<string> services = new List<string>()
            {
                "https://ipv4.icanhazip.com",
                "https://api.ipify.org",
                "https://ipinfo.io/ip",
                "https://checkip.amazonaws.com",
                "https://wtfismyip.com/text",
                "http://icanhazip.com"
            };

            using (var webclient = new WebClient())
            {
                foreach (var service in services)
                {
                    try
                    {
                        return IPAddress.Parse(webclient.DownloadString(service));
                    }
                    catch
                    {
                    }
                }
            }

            return null;
        }

        public IPAddress GetMyTorExitIP()
        {
            if (_useTor)
            {
                List<string> services = new List<string>()
                {
                    "https://check.torproject.org",
                };

                foreach (var service in services)
                {
                    try
                    {
                        var torHttpClient = new TorHttpClient(new Uri(service), _torEndPoint);
                        var response = torHttpClient.SendAsync(HttpMethod.Get, string.Empty).Result;

                        var html = response.Content.ReadAsStringAsync().Result;

                        var doc = new HtmlAgilityPack.HtmlDocument();
                        doc.LoadHtml(html);

                        var nodes = doc.DocumentNode.SelectNodes("//p/strong");

                        foreach (var node in nodes)
                        {
                            try
                            {
                                var exitIP = IPAddress.Parse(node.InnerText);
                                PublicIP = exitIP;
                                return exitIP;
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return null;
        }
    }
}

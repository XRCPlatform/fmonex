using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace FreeMarketOne.Extensions.Helpers
{
    public class IpHelper
    {
        public IPAddress PublicIp { get; set; }
        public IpHelper()
        {
            PublicIp = GetIp();
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
                
            foreach (var service in services)
            {
                try
                { 
                    return IPAddress.Parse(webclient.DownloadString(service));
                } catch {
                
                }
            }

            return null;
        }
    }
}

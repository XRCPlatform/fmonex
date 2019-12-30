using FreeMarketOne.Extensions.Helpers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace FreeMarketOne.Extensions.Models
{
    public class BaseConfiguration
    {
        public BaseConfiguration() {

            this.Environment = EnvironmentTypes.Test;
            this.TorEndPoint = EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9050/");
            this.LogFilePath = "log/log.txt";
            this.OnionSeedsEndPoint = "https://www.freemarket.one/onionseeds_testnet.txt";
        }

        public enum EnvironmentTypes
        {
            Main = 0,
            Test = 1
        }

        public EndPoint TorEndPoint { get; set; }

        public string OnionSeedsEndPoint { get; set; }

        public EnvironmentTypes Environment { get; set; }

        public string LogFilePath { get; set; }
    }
}

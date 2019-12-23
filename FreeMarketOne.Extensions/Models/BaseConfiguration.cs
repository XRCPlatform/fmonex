using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace FreeMarketOne.Extensions.Models
{
    public class BaseConfiguration
    {
        public enum EnvironmentTypes
        {
            Main = 0,
            Test = 1
        }

        public EndPoint TorEndPoint;

        public string OnionSeedsEndPoint;

        public EnvironmentTypes Environment { get; set; }

        public string LogFilePath;
    }
}

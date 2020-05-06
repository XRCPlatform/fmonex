using System.Net;
using System.Reflection;

namespace FreeMarketOne.DataStructure
{
    public class BaseConfiguration : IBaseConfiguration
    {
        public BaseConfiguration() {

            this.Environment = (int)EnvironmentTypes.Test;
            this.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public enum EnvironmentTypes
        {
            Main = 0,
            Test = 1
        }

        public EndPoint TorEndPoint { get; set; }

        public string OnionSeedsEndPoint { get; set; }

        public int Environment { get; set; }

        public string LogFilePath { get; set; }

        public string Version { get; set; }

        public string MemoryBasePoolPath { get; set; }

        public string MemoryMarketPoolPath { get; set; }

        public string BlockChainBasePath { get; set; }

        public string BlockChainMarketPath { get; set; }
        
        public string BlockChainSecretPath { get; set; }

        public EndPoint ListenerBaseEndPoint { get; set; }

        public EndPoint ListenerMarketEndPoint { get; set; }

        public string ChangellyApiKey { get; set; }

        public string ChangellySecret { get; set; }
        public string ChangellyApiBaseUrl { get ; set ; }
        public string TelemetryServerUri { get; set; }
    }
}

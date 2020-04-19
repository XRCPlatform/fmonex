using FreeMarketOne.Extensions.Helpers;

namespace FreeMarketOne.DataStructure
{
    public class MainConfiguration : BaseConfiguration
    {
        public MainConfiguration()
        {
            this.Environment = (int)EnvironmentTypes.Main;
            this.TorEndPoint = EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9050/");
            this.LogFilePath = "log/log.txt";
            this.OnionSeedsEndPoint = "https://www.freemarket.one/onionseeds.txt";
            this.ChangellyApiKey = "5fe8cbe95ade4e73bdb62db0897e3615";
            this.ChangellySecret = "2b8c94c3c7befcc751c932117a63b63e12c2f2c176ebf6553b5e375da2a8b656";
            this.ChangellyApiBaseUrl = "https://api.changelly.com";
        }
    }
}

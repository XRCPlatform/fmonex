using FreeMarketOne.Extensions.Helpers;

namespace FreeMarketOne.DataStructure
{
    public class TestConfiguration : BaseConfiguration
    {
        public TestConfiguration()
        {
            this.Environment = (int)EnvironmentTypes.Test;
            this.TorEndPoint = EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9050/");
            this.LogFilePath = "log/testnet_log.txt";
            this.OnionSeedsEndPoint = "https://www.freemarket.one/onionseeds_testnet.txt";
        }
    }
}

using FreeMarketOne.Extensions.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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
            this.MemoryBasePoolPath = "data/testnet_memory_basetx.data";
            this.MemoryMarketPoolPath = "data/testnet_memory_markettx.data";
            this.BlockChainBasePath = "data/testnet_blockchain_base";
            this.BlockChainMarketPath = "data/testnet_blockchain_market";
            this.BlockChainSecretPath = "data/testnet_key.data";
            this.ListenerBaseEndPoint = EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9113/");
            this.ListenerMarketEndPoint = EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9114/");
        }
    }
}

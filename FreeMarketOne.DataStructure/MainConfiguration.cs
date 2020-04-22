using FreeMarketOne.Extensions.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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
            this.MemoryBasePoolPath = "data/memory_basetx.data";
            this.MemoryMarketPoolPath = "data/memory_markettx.data";
            this.BlockChainBasePath = "data/blockchain_base";
            this.BlockChainMarketPath = "data/blockchain_market";
        }
    }
}

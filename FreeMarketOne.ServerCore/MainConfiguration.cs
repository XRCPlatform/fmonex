﻿using FreeMarketOne.BlockChain.Policy;
using FreeMarketOne.Extensions.Helpers;
using System;

namespace FreeMarketOne.DataStructure
{
    public class MainConfiguration : BaseConfiguration
    {
        private static readonly TimeSpan blockInterval = TimeSpan.FromSeconds(300);
        private static readonly TimeSpan poolCheckInterval = TimeSpan.FromSeconds(30);
        private static readonly long difficulty = 100000;

        public MainConfiguration()
        {
            this.Environment = (int)EnvironmentTypes.Main;
            this.TorEndPoint = EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9050/");
            this.LogFilePath = "log/log.txt";
            this.OnionSeedsEndPoint = "https://www.freemarket.one/onionseeds_v2.txt";
            this.MemoryBasePoolPath = "data/memory_basetx.data";
            this.MemoryMarketPoolPath = "data/memory_markettx.data";
            this.BlockChainBasePath = "data/blockchain_base";
            this.BlockChainMarketPath = "data/blockchain_market";
            this.BlockChainSecretPath = "data/key.data";
            this.ListenerBaseEndPoint = EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9111/");
            this.ListenerMarketEndPoint = EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9112/");
            this.ChangellyApiKey = "5fe8cbe95ade4e73bdb62db0897e3615";
            this.ChangellySecret = "2b8c94c3c7befcc751c932117a63b63e12c2f2c176ebf6553b5e375da2a8b656";
            this.ChangellyApiBaseUrl = "https://api.changelly.com";
            this.TelemetryServerUri = "https://telemetry.freemarket.one/";

            this.BlockChainBasePolicy = new BaseBlockPolicy<BaseAction>(
                    null,
                    blockInterval,
                    difficulty,
                    poolCheckInterval);

            this.BlockChainMarketPolicy = new BaseBlockPolicy<MarketAction>(
                    null,
                    blockInterval,
                    difficulty,
                    poolCheckInterval);
        }
    }
}

using FreeMarketOne.BlockChain.Policy;
using FreeMarketOne.DataStructure;
using FreeMarketOne.Extensions.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.BlockChain.Test
{
    internal class DebugConfiguration : BaseConfiguration
    {
        private static readonly TimeSpan _blockInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan _poolCheckInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan? _validBlockInterval = null;
        private static readonly long _difficulty = 100000;

        internal DebugConfiguration()
        {
            this.Environment = (int)EnvironmentTypes.Test;
            this.TorEndPoint = EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9050/");
            this.LogFilePath = "log/debug_log.txt";
            this.OnionSeedsEndPoint = "https://www.freemarket.one/onionseeds_testnet_v2.txt";
            this.MemoryBasePoolPath = "data/debug_memory_basetx.data";
            this.MemoryMarketPoolPath = "data/debug_memory_markettx.data";
            this.BlockChainBasePath = "data/debug_blockchain_base";
            this.BlockChainMarketPath = "data/debug_blockchain_market";
            this.BlockChainSecretPath = "data/debug_key.data";
            this.ListenersUseTor = true;
            this.ListenerBaseEndPoint = EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9113/");
            this.ListenerMarketEndPoint = EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9114/");
            this.TelemetryServerUri = "http://40.115.21.64:8088/services/collector/event";

            this.BlockChainBasePolicy = new BaseBlockPolicy<BaseAction>(
                    null,
                    _blockInterval,
                    _difficulty,
                    _poolCheckInterval,
                    null);

            this.BlockChainMarketPolicy = new BaseBlockPolicy<MarketAction>(
                    null,
                    _blockInterval,
                    _difficulty,
                    _poolCheckInterval,
                    _validBlockInterval);
        }
    }
}

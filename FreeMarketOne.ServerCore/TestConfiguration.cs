using FreeMarketOne.BlockChain.Policy;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using System;

namespace FreeMarketOne.DataStructure
{
    public class TestConfiguration : BaseConfiguration
    {
        private static readonly TimeSpan _blockInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan _poolCheckInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan _validBlockInterval = TimeSpan.FromDays(15);
        private static readonly long _difficulty = 100000;

        public TestConfiguration()
        {
            this.Environment = (int)EnvironmentTypes.Test;
            this.TorEndPoint = EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9050/");
            this.LogFilePath = "log/testnet_log.txt";
            this.OnionSeedsEndPoint = "https://www.freemarket.one/onionseeds_testnet_v2.txt";
            this.MemoryBasePoolPath = "data/testnet_memory_basetx.data";
            this.MemoryMarketPoolPath = "data/testnet_memory_markettx.data";
            this.BlockChainBaseGenesis = "testnet_base_genesis.dat";
            this.BlockChainBasePath = "data/testnet_blockchain_base";
            this.BlockChainMarketGenesis = "testnet_market_genesis.dat";
            this.BlockChainMarketPath = "data/testnet_blockchain_market";
            this.BlockChainSecretPath = "data/testnet_key.data";
            this.BlockChainUserPath = "data/testnet_user.data";
            this.ListenersUseTor = false;
            this.ListenerBaseEndPoint = EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9113/");
            this.ListenerMarketEndPoint = EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9114/");
            this.TelemetryServerUri = "http://40.115.21.64:8088/services/collector/event";
            this.ChatPath = "data/testnet_chat";
            this.ListenerChatEndPoint = EndPointHelper.ParseIPEndPoint("tcp://127.0.0.1:9115/");
            this.SearchEnginePath = "data/testnet_searchindex";

            this.BlockChainBasePolicy = new BaseBlockPolicy<BaseAction>(
                    null,
                    _blockInterval,
                    _difficulty,
                    _poolCheckInterval,
                    null,
                    typeof(BaseAction),
                    new Type[] { 
                        typeof(CheckPointMarketDataV1),
                        typeof(ReviewUserDataV1),
                        typeof(UserDataV1)
                    });

            this.BlockChainMarketPolicy = new BaseBlockPolicy<MarketAction>(
                    null,
                    _blockInterval,
                    _difficulty,
                    _poolCheckInterval,
                    _validBlockInterval,
                    typeof(MarketAction),
                    new Type[] {
                        typeof(MarketItemV1)
                    });

        }
    }
}

using FreeMarketOne.BlockChain.Policy;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using System;

namespace FreeMarketOne.ServerCore
{
    public class TestConfiguration : BaseConfiguration
    {
        private static readonly TimeSpan _blockInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan _poolCheckInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan _validBlockInterval = TimeSpan.FromDays(15);
        private static readonly TimeSpan _periodicBroadcastInterval = TimeSpan.FromSeconds(10);
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
            this.ListenerBaseEndPoint = 9113;// EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9113/");
            this.ListenerMarketEndPoint = 9114;//EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9114/");
            this.TelemetryServerUri = "http://40.115.21.64:8088/services/collector/event";
            this.ChatPath = "data/testnet_chat";
            this.ListenerChatEndPoint = EndPointHelper.ParseIPEndPoint("tcp://0.0.0.0:9115/");
            this.SearchEnginePath = "data/testnet_searchindex";
            this.MinimalPeerAmount = 1;
            this.XRCDaemonUri = "188.127.231.159:16661";
            this.XRCDaemonUriSsl = false;
            this.XRCDaemonUser = "fm1_xrc_testnet_user";
            this.XRCDaemonPassword = "fm1_xrc_testnet_password";
            this.PoolMaxStagedTxCountInNetwork = 30;
            this.PoolMaxCountOfLocalItemsPropagation = 5;
            this.PoolPeriodicBroadcastTxInterval = _periodicBroadcastInterval;
            this.BlockMaxTransactionsPerBlock = 30; //should be equal to PoolMaxStagedTxCountInNetwork
            this.BlockMaxBlockBytes = 100 * 1024;
            this.BlockMaxGenesisBytes = 100 * 1024;

            this.BlockChainBasePolicy = new BaseBlockPolicy<BaseAction>(
                    null,
                    _blockInterval,
                    _difficulty,
                    this.BlockMaxTransactionsPerBlock,
                    this.BlockMaxBlockBytes,
                    this.BlockMaxGenesisBytes,
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
                    this.BlockMaxTransactionsPerBlock,
                    this.BlockMaxBlockBytes,
                    this.BlockMaxGenesisBytes,
                    _poolCheckInterval,
                    _validBlockInterval,
                    typeof(MarketAction),
                    new Type[] {
                        typeof(MarketItemV1)
                    });

        }
    }
}

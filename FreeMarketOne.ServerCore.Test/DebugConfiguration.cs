using FreeMarketOne.BlockChain.Policy;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using System;

namespace FreeMarketOne.ServerCore.Test
{
    internal class DebugConfiguration : ExtendedConfiguration
    {
        private static readonly TimeSpan _blockInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan _poolCheckInterval = TimeSpan.FromSeconds(6);
        private static readonly TimeSpan _periodicBroadcastInterval = TimeSpan.FromSeconds(10);
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
            this.BlockChainUserPath = "data/debug_user.data";
            this.ListenersUseTor = true;
            this.ListenerBaseEndPoint = 9113;//EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9113/");
            this.ListenerMarketEndPoint = 9114;//EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9114/");
            this.TelemetryServerUri = "http://40.115.21.64:8088/services/collector/event";
            this.ListenerChatEndPoint = EndPointHelper.ParseIPEndPoint("tcp://127.0.0.1:9115/");
            this.ChatPath = "data/debug_chat";
            this.SearchEnginePath = "data/debug_searchindex";
            this.MinimalPeerAmount = 0;
            this.PoolMaxStagedTxCountInNetwork = 1;
            this.PoolMaxCountOfLocalItemsPropagation = 5;
            this.PoolPeriodicBroadcastTxInterval = _periodicBroadcastInterval;
            this.BlockMaxTransactionsPerBlock = 1; //should be equal to PoolMaxStagedTxCountInNetwork
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

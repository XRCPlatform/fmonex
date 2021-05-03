using FreeMarketOne.BlockChain.Policy;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using System;

namespace FreeMarketOne.ServerCore
{
    public class MainConfiguration : ExtendedConfiguration
    {
        private static readonly TimeSpan _blockInterval = TimeSpan.FromSeconds(300);
        private static readonly TimeSpan _poolCheckInterval = TimeSpan.FromSeconds(32);
        private static readonly TimeSpan _validBlockInterval = TimeSpan.FromDays(120);
        private static readonly TimeSpan _periodicBroadcastInterval = TimeSpan.FromSeconds(10);
        private static readonly long _difficulty = 100000;

        public MainConfiguration()
        {
            this.Environment = (int)EnvironmentTypes.Main;
            this.TorEndPoint = EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9050/");
            this.LogFilePath = "log/log.txt";
            this.OnionSeedsEndPoint = "https://www.freemarket.one/onionseeds.txt";
            this.MemoryBasePoolPath = "data/memory_basetx.data";
            this.MemoryMarketPoolPath = "data/memory_markettx.data";
            this.BlockChainBasePath = "data/blockchain_base";
            this.BlockChainBaseGenesis = "base_genesis.dat";
            this.BlockChainMarketPath = "data/blockchain_market";
            this.BlockChainMarketGenesis = "market_genesis.dat";
            this.BlockChainSecretPath = "data/key.data";
            this.BlockChainUserPath = "data/user.data";
            this.ListenersUseTor = true;
            this.ListenerBaseEndPoint = 9111; //EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9111/");
            this.ListenerMarketEndPoint = 9112; // EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9112/");
            this.ChangellyApiKey = "5fe8cbe95ade4e73bdb62db0897e3615";
            this.ChangellySecret = "2b8c94c3c7befcc751c932117a63b63e12c2f2c176ebf6553b5e375da2a8b656";
            this.ChangellyApiBaseUrl = "https://api.changelly.com";
            this.TelemetryServerUri = "https://telemetry.freemarket.one/";
            this.ChatPath = "data/chat";
            this.ListenerChatEndPoint = EndPointHelper.ParseIPEndPoint("tcp://0.0.0.0:9110/");
            this.SearchEnginePath = "data/searchindex";
            this.MinimalPeerAmount = 4;
            this.XRCDaemonUri = "https://tpool.bitcoinrh.org/rpc/";
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

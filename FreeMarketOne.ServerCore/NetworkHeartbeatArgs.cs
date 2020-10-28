namespace FreeMarketOne.ServerCore
{
    public class NetworkHeartbeatArgs
    {
        public bool IsMarketChainNetworkConnected { get; internal set; }
        public bool IsBaseChainNetworkConnected { get; internal set; }
        public int PeerCount { get; set; }
        public bool IsTorUp { get; internal set; }

        public long BaseHeight { get; internal set; }
        public int PoolBaseLocalItemsCount { get; internal set; }
        public int PoolBaseStagedItemsCount { get; internal set; }

        public long MarketHeight { get; internal set; }
        public int PoolMarketLocalItemsCount { get; internal set; }
        public int PoolMarketStagedItemsCount { get; internal set; }
    }
}
namespace FreeMarketOne.ServerCore
{
    public class NetworkHeartbeatArgs
    {
        public bool IsMarketChainNetworkConnected { get; internal set; }
        public bool IsBaseChainNetworkConnected { get; internal set; }
        public int PeerCount { get; set; }
        public bool IsTorUp { get; internal set; }

        public int PoolBaseLocalItems { get; internal set; }
        public int PoolBaseStagedItems { get; internal set; }

        public int PoolMarketLocalItems { get; internal set; }
        public int PoolMarketStagedItems { get; internal set; }
    }
}
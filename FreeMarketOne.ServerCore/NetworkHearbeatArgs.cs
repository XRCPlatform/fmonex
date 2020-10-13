namespace FreeMarketOne.ServerCore
{
    public class NetworkHearbeatArgs
    {
        public bool IsMarketChainNetworkConnected { get; internal set; }
        public bool IsBaseChainNetworkConnected { get; internal set; }
        public int PeerCount { get; set; }
        public bool IsTorUp { get; internal set; }
    }
}
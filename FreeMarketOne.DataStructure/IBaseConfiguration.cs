using Libplanet.Blockchain.Policies;
using Libplanet.Action;
using System.Net;

namespace FreeMarketOne.DataStructure
{
    public interface IBaseConfiguration
    {
        EndPoint TorEndPoint { get; set; }
        string OnionSeedsEndPoint { get; set; }
        string LogFilePath { get; set; }
        string Version { get; set; }
        int Environment { get; set; }
        string MemoryBasePoolPath { get; set; }
        string MemoryMarketPoolPath { get; set; }
        string BlockChainBasePath { get; set; }
        string BlockChainMarketPath { get; set; }
        string BlockChainSecretPath { get; set; }
        EndPoint ListenerBaseEndPoint { get; set; }
        EndPoint ListenerMarketEndPoint { get; set; }
        string ChangellyApiKey { get; set; }
        string ChangellySecret { get; set; }
        string ChangellyApiBaseUrl { get; set; }
        string TelemetryServerUri { get; set; }

        string FullBaseDirectory { get; set; }

        IBlockPolicy<BaseAction> BlockChainBasePolicy { get; set; }
        IBlockPolicy<MarketAction> BlockChainMarketPolicy { get; set; }
    }
}

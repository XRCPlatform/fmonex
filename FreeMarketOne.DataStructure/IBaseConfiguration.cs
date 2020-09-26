using Libplanet.Blockchain.Policies;
using Libplanet.Action;
using System.Net;
using Libplanet.Extensions;

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
        string BlockChainBaseGenesis { get; set; }
        string BlockChainBasePath { get; set; }
        string BlockChainMarketGenesis { get; set; }
        string BlockChainMarketPath { get; set; }
        string BlockChainSecretPath { get; set; }
        string BlockChainUserPath { get; set; }
        IPEndPoint ListenerBaseEndPoint { get; set; }
        IPEndPoint ListenerMarketEndPoint { get; set; }
        string ChangellyApiKey { get; set; }
        string ChangellySecret { get; set; }
        string ChangellyApiBaseUrl { get; set; }
        string TelemetryServerUri { get; set; }
        string FullBaseDirectory { get; set; }

        IDefaultBlockPolicy<BaseAction> BlockChainBasePolicy { get; set; }
        IDefaultBlockPolicy<MarketAction> BlockChainMarketPolicy { get; set; }

        bool ListenersUseTor { get; set; }

        string ListenersForceThisIp { get; set; }

        string ChatPath { get; set; }

        IPEndPoint ListenerChatEndPoint { get; set; }

        string SearchEnginePath { get; set; }
    }
}

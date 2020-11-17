using Libplanet.Blockchain.Policies;
using Libplanet.Action;
using System.Net;
using Libplanet.Extensions;
using System;

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
        int MinimalPeerAmount { get; set; }
        string XRCDaemonUri { get; set; }
        bool XRCDaemonUriSsl { get; set; }
        string XRCDaemonUser { get; set; }
        string XRCDaemonPassword { get; set; }


        /// <summary>
        /// Maximal limit of tx in network pool
        /// </summary>
        int PoolMaxStagedTxCountInNetwork { get; set; }

        /// <summary>
        /// Maximal limit of local items to be included in tx
        /// </summary>
        int PoolMaxCountOfLocalItemsPropagation { get; set; }

        /// <summary>
        /// Interval for propagation next batch to network pool
        /// </summary>
        TimeSpan PoolPeriodicBroadcastTxInterval { get; set; }

        /// <summary>
        /// Maximal count of tx in block
        /// </summary>
        int BlockMaxTransactionsPerBlock { get; set; }

        /// <summary>
        /// Maximal block size in bytes
        /// </summary>
        int BlockMaxBlockBytes { get; set; }

        /// <summary>
        /// Maximal genesis block size
        /// </summary>
        int BlockMaxGenesisBytes { get; set; }
    }
}

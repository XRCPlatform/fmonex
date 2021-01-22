using Libplanet.Action;
using Libplanet.Blockchain.Policies;
using Libplanet.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace FreeMarketOne.DataStructure
{
    public class BaseConfiguration : IBaseConfiguration
    {
        public BaseConfiguration() {

            var assembly = Assembly.GetExecutingAssembly();
            var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);

            this.Environment = (int)EnvironmentTypes.Test;
            this.Version = fileVersion.ProductVersion;
        }

        public enum EnvironmentTypes
        {
            Main = 0,
            Test = 1
        }

        public EndPoint TorEndPoint { get; set; }

        public string OnionSeedsEndPoint { get; set; }

        public int Environment { get; set; }

        public string LogFilePath { get; set; }

        public string Version { get; set; }

        public string MemoryBasePoolPath { get; set; }

        public string MemoryMarketPoolPath { get; set; }

        public string BlockChainBaseGenesis { get; set; }

        public string BlockChainBasePath { get; set; }

        public string BlockChainMarketGenesis { get; set; }

        public string BlockChainMarketPath { get; set; }
        
        public string BlockChainSecretPath { get; set; }

        public string BlockChainUserPath { get; set; }

        public int ListenerBaseEndPoint { get; set; }

        public int ListenerMarketEndPoint { get; set; }

        public string ChangellyApiKey { get; set; }

        public string ChangellySecret { get; set; }
        public string ChangellyApiBaseUrl { get ; set ; }
        public string TelemetryServerUri { get; set; }

        public string FullBaseDirectory { get; set; }

        public IDefaultBlockPolicy<BaseAction> BlockChainBasePolicy { get; set; }
        public IDefaultBlockPolicy<MarketAction> BlockChainMarketPolicy { get; set; }

        public bool ListenersUseTor { get; set; }
        public string ListenersForceThisIp { get; set; }

        public string ChatPath { get; set; }
        public IPEndPoint ListenerChatEndPoint { get; set; }

        public string SearchEnginePath { get; set; }

        public int MinimalPeerAmount { get; set; }
        public string XRCDaemonUri { get; set; }
        public bool XRCDaemonUriSsl { get; set; }
        public string XRCDaemonUser { get; set; }
        public string XRCDaemonPassword { get; set; }


        /// <inheritdoc/>
        public int PoolMaxStagedTxCountInNetwork { get; set; }

        /// <inheritdoc/>
        public int PoolMaxCountOfLocalItemsPropagation { get; set; }

        /// <inheritdoc/>
        public TimeSpan PoolPeriodicBroadcastTxInterval { get; set; }

        /// <inheritdoc/>
        public int BlockMaxTransactionsPerBlock { get; set; }

        /// <inheritdoc/>
        public int BlockMaxBlockBytes { get; set; }

        /// <inheritdoc/>
        public int BlockMaxGenesisBytes { get; set; }

        /// <inheritdoc/>
        public List<string> OnionSeeds { get; set; }
    }
}

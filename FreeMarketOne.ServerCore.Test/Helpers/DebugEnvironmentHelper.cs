using FreeMarketOne.BlockChain.Test.Mocks;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.ProtocolVersions;
using FreeMarketOne.GenesisBlock;
using FreeMarketOne.P2P;
using FreeMarketOne.Pools;
using FreeMarketOne.ServerCore.Test;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FreeMarketOne.BlockChain.Test.Helpers
{
    public class DebugEnvironmentHelper
    {
        public void Initialize<T>(
            ref IBaseConfiguration configuration,
            ref ILogger logger, 
            ref IOnionSeedsManager onionSeedsManager, 
            ref BasePoolManager basePoolManager, 
            ref IBlockChainManager<BaseAction> baseBlockChainManager,
            ref UserPrivateKey userPrivateKey,
            ref EventHandler _baseBlockChainLoadedEvent,
            ref EventHandler<(Block<BaseAction> OldTip, Block<BaseAction> NewTip)> _baseBlockChainChangedEvent)
        {
            configuration = new DebugConfiguration();
            configuration.FullBaseDirectory = InitializeFullBaseDirectory();

            //Clear all debug old data
            ClearDefaultEnvironment(configuration);

            //Initialize Logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(Path.Combine(configuration.FullBaseDirectory, configuration.LogFilePath),
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{Exception}{NewLine}",
                    rollingInterval: RollingInterval.Day, shared: true)
                .CreateLogger();
            logger = Log.Logger.ForContext<T>();
            logger.Information("Debug Start");

            //Mock user private key
            userPrivateKey = new UserPrivateKey();
            
            //Initialize Mock OnionSeeds
            onionSeedsManager = new MockSeedManager();

            //Initialize genesis blocks
            var genesisHelper = new GenesisHelper();
            var genesis = genesisHelper.GenerateIt(configuration);

            //Initialize Base BlockChain Manager
            baseBlockChainManager = new BlockChainManager<BaseAction>(
                configuration,
                configuration.BlockChainBasePath,
                configuration.BlockChainSecretPath,
                null,
                configuration.BlockChainBasePolicy,
                configuration.ListenerBaseEndPoint,
                onionSeedsManager,
                userPrivateKey,
                new DebugChainProtocolVersion(),
                genesisBlock: genesis,
                preloadEnded: _baseBlockChainLoadedEvent,
                blockChainChanged: _baseBlockChainChangedEvent);
            baseBlockChainManager.Start();
        }

        private string InitializeFullBaseDirectory()
        {
            var fullBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!fullBaseDirectory.StartsWith('/'))
                {
                    fullBaseDirectory.Insert(0, "/");
                }
            }

            return fullBaseDirectory;
        }

        private void ClearDefaultEnvironment(IBaseConfiguration configuration)
        {
            var folderPathBase = Path.Combine(configuration.FullBaseDirectory, configuration.BlockChainBasePath);
            var folderPathMarket = Path.Combine(configuration.FullBaseDirectory, configuration.BlockChainMarketPath);
            var folderLog = Path.Combine(configuration.FullBaseDirectory, configuration.LogFilePath);
            var folderChat = Path.Combine(configuration.FullBaseDirectory, configuration.ChatPath);

            var keyFile = Path.Combine(configuration.FullBaseDirectory, configuration.BlockChainSecretPath);
            var memoryBasePoolFile = Path.Combine(configuration.FullBaseDirectory, configuration.MemoryBasePoolPath);
            var memoryMarketPoolFile = Path.Combine(configuration.FullBaseDirectory, configuration.MemoryMarketPoolPath);

            if (Directory.Exists(folderPathBase)) Directory.Delete(folderPathBase, true);
            if (Directory.Exists(folderPathMarket)) Directory.Delete(folderPathMarket, true);
            if (Directory.Exists(folderLog)) Directory.Delete(folderLog, true);
            if (Directory.Exists(folderChat)) Directory.Delete(folderChat, true);

            if (File.Exists(keyFile)) File.Delete(keyFile);
            if (File.Exists(memoryBasePoolFile)) File.Delete(memoryBasePoolFile);
            if (File.Exists(memoryMarketPoolFile)) File.Delete(memoryMarketPoolFile);
        }
    }
}

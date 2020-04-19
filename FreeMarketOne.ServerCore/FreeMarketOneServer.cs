﻿using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.P2P.Models;
using FreeMarketOne.Tor;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using FreeMarketOne.P2P;
using FreeMarketOne.Mining;
using FreeMarketOne.BasePool;
using FreeMarketOne.MarketPool;
using FreeMarketOne.DataStructure;
using static FreeMarketOne.DataStructure.BaseConfiguration;

namespace FreeMarketOne.ServerCore
{
    public class FreeMarketOneServer
    {
        static FreeMarketOneServer()
        {
            Current = new FreeMarketOneServer();
        }

        public static FreeMarketOneServer Current { get; private set; }

        public Logger Logger;
        private ILogger logger;

        public IBaseConfiguration Configuration;
        public TorProcessManager TorProcessManager;

        public IOnionSeedsManager OnionSeedsManager;
        public IMiningProcessor MiningProcessor;

        public IBasePoolManager BasePoolManager;
        public IMarketPoolManager MarketPoolManager;

        public void Initialize()
        {
            /* Configuration */
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, false);
            var configFile = builder.Build();

            /* Environment */
            Configuration = InitializeEnvironment(configFile);

            /* Config */
            InitializeBaseOnionSeedsEndPoint(Configuration, configFile);
            InitializeBaseTorEndPoint(Configuration, configFile);
            InitializeLogFilePath(Configuration, configFile);

            /* Initialize Logger */
            Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Configuration.LogFilePath,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{Exception}{NewLine}",
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();
            logger = Logger.ForContext<FreeMarketOneServer>();
            logger.Information("Application Start");

            /* Initialize Tor */
            TorProcessManager = new TorProcessManager(Logger, Configuration);
            var torInitialized = TorProcessManager.Start();

            if (torInitialized)
            {
                /* Initialize OnionSeeds */
                OnionSeedsManager = new OnionSeedsManager(Logger, Configuration, TorProcessManager);
                OnionSeedsManager.GetOnions();
                OnionSeedsManager.StartPeriodicCheck();
                OnionSeedsManager.StartPeriodicPeerBroadcast();
            }

            /* Time Manager Loader < ------- Necessary to finish */
            var genesisTimeUtc = DateTime.UtcNow.AddDays(-10).AddSeconds(-25); //!!!!FROM GENESIS BLOCK OF BASE BLOCKCHAIN
            var networkTimeUtc = DateTime.UtcNow; //!!!!TIME FROM SOME EXTERNAL SERVICE - To get real time

            /* Initialize Base And Market Pool */
            BasePoolManager = new BasePoolManager(Logger, Configuration);
            MarketPoolManager = new MarketPoolManager(Logger, Configuration);

            /* Initialize MiningProcessor */
            MiningProcessor = new MiningProcessor(Logger, Configuration, BasePoolManager, MarketPoolManager, 
                genesisTimeUtc, networkTimeUtc);
            var miningProcessorInitialized = MiningProcessor.Start();

            if (miningProcessorInitialized)
            {

            }
        }

        private void InitializeLogFilePath(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var settings = configFile.GetSection("FreeMarketOneConfiguration")["LogFilePath"];

            configuration.LogFilePath = settings;
        }

        public static IBaseConfiguration InitializeEnvironment(IConfigurationRoot configFile)
        {
            var settings = configFile.GetSection("FreeMarketOneConfiguration")["ServerEnvironment"];

            var environment = EnvironmentTypes.Test;
            Enum.TryParse(settings, out environment);

            if (environment == EnvironmentTypes.Main)
            {
                return new MainConfiguration();
            } 
            else
            {
                return new TestConfiguration();
            }
        }

        public static void InitializeBaseOnionSeedsEndPoint(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var prefix = "TestNetOnionSeedsEndPoint";
            if (configuration.Environment == (int)EnvironmentTypes.Main) prefix = "MainOnionSeedsEndPoint";

            var settings = configFile.GetSection("FreeMarketOneConfiguration")[prefix];

            configuration.OnionSeedsEndPoint = settings;
        }

        public static void InitializeBaseTorEndPoint(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var settings = configFile.GetSection("FreeMarketOneConfiguration")["TorEndPoint"];

            configuration.TorEndPoint = EndPointHelper.ParseIPEndPoint(settings);
        }

        public void Stop()
        {
            logger.Information("Ending Onion Seeds ...");

            OnionSeedsManager.Dispose();

            logger.Information("Ending Mining Processor...");

            BasePoolManager.Dispose();

            logger.Information("Ending Base Pool Manager...");

            MarketPoolManager.Dispose();

            logger.Information("Ending Market Pool Manager...");

            MiningProcessor.Dispose();

            logger.Information("Ending Tor...");

            TorProcessManager.Dispose();

            logger.Information("Application End");
        }
    }
}
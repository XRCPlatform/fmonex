using FreeMarketOne.Extensions.Helpers;
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
using FreeMarketOne.Extensions.Models;
using static FreeMarketOne.Extensions.Models.BaseConfiguration;
using FreeMarketOne.Mining;

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

        public TorProcessManager TorProcessManager;
        public OnionSeedsManager OnionSeedsManager;
        public BaseConfiguration Configuration;
        public MiningProcessor MiningProcessor;

        public void Initialize()
        {
            /* Configuration */
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, false);
            var configFile = builder.Build();
            Configuration = new BaseConfiguration();

            /* Environment */
            InitializeEnvironment(Configuration, configFile);

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

                //tests
                // var s = _torProcessManager.IsTorRunningAsync().Result;

                var breakIt = true;
            }

            /* Initialize MiningProcessor */
            MiningProcessor = new MiningProcessor(Logger, Configuration);
            var miningProcessorInitialized = MiningProcessor.Start();

            if (miningProcessorInitialized)
            {

            }
        }

        private void InitializeLogFilePath(BaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var settings = configFile.GetSection("FreeMarketOneConfiguration")["LogFilePath"];

            configuration.LogFilePath = settings;
        }

        public static void InitializeEnvironment(BaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var settings = configFile.GetSection("FreeMarketOneConfiguration")["ServerEnvironment"];

            var environment = EnvironmentTypes.Test;

            Enum.TryParse(settings, out environment);

            configuration.Environment = environment;
        }

        public static void InitializeBaseOnionSeedsEndPoint(BaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var prefix = "TestNetOnionSeedsEndPoint";
            if (configuration.Environment == EnvironmentTypes.Main) prefix = "MainOnionSeedsEndPoint";

            var settings = configFile.GetSection("FreeMarketOneConfiguration")[prefix];

            configuration.OnionSeedsEndPoint = settings;
        }

        public static void InitializeBaseTorEndPoint(BaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var settings = configFile.GetSection("FreeMarketOneConfiguration")["TorEndPoint"];

            configuration.TorEndPoint = EndPointHelper.ParseIPEndPoint(settings);
        }

        public void Stop()
        {
            logger.Information("Ending Tor...");

            TorProcessManager.Dispose();

            logger.Information("Ending Onion Seeds ...");

            OnionSeedsManager.Dispose();

            logger.Information("Application End");
        }
    }
}
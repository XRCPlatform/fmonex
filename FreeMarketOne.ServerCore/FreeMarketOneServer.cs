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

namespace FreeMarketOne.ServerCore
{
    public class FreeMarketOneServer
    {
        static FreeMarketOneServer()
        {
            Current = new FreeMarketOneServer();
        }

        public static FreeMarketOneServer Current { get; private set; }

        public Logger _logger;
        public TorProcessManager _torProcessManager;
        public List<OnionSeed> _onionSeeds;
        public BaseConfiguration _configuration;

        public void Initialize()
        {
            /* Configuration */
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configFile = builder.Build();
            _configuration = new BaseConfiguration();

            /* Environment */
            InitializeEnvironment(_configuration, configFile);

            /* Config */
            InitializeBaseOnionSeedsEndPoint(_configuration, configFile);
            InitializeBaseTorEndPoint(_configuration, configFile);
            InitializeLogFilePath(_configuration, configFile);

            /* Initialize Logger */
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(_configuration.LogFilePath)
                .CreateLogger();
            _logger.Information("Prepaire Tor");

            /* Initialize Tor */
            _torProcessManager = new TorProcessManager(_logger, _configuration);

            /* Initialize OnionSeeds */
            _onionSeeds = OnionSeeds.GetOnions(_configuration);

            //tests
           // var s = _torProcessManager.IsTorRunningAsync().Result;

            var breakIt = true;
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
    }
}
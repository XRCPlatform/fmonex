using FreeMarketOne.DataStructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.ServerCore
{
    public class MarketManager
    {
        private IBaseConfiguration _configuration;

        private ILogger _logger { get; set; }

        public MarketManager(IBaseConfiguration configuration)
        {
            _logger = Log.Logger.ForContext<ServiceManager>();
            _logger.Information("Initializing Market Manager");

            _configuration = configuration;
        }
    }
}

using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Tor;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

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
        public TorProcessManager TorProcessManager;
        public EndPoint EndPoint; 

        public void Initialize()
        {
            EndPoint = EndPointHelper.ParseIPEndPoint("http://127.0.0.1:9050/");

            Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("log/log.txt")
                .CreateLogger();

            Logger.Information("Prepaire Tor");


            TorProcessManager = new TorProcessManager(Logger, EndPoint);
            //TorProcessManager.Start();

            var s = TorProcessManager.IsTorRunningAsync().Result;

            var breakIt = true;
        }
    }
}
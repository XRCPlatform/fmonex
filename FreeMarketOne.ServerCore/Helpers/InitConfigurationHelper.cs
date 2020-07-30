﻿using FreeMarketOne.DataStructure;
using FreeMarketOne.Extensions.Helpers;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using static FreeMarketOne.DataStructure.BaseConfiguration;

namespace FreeMarketOne.ServerCore.Helpers
{
    internal static class InitConfigurationHelper
    {
        internal static string InitializeFullBaseDirectory()
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

        internal static void InitializeLogFilePath(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var logFilePath = configFile.GetSection("FreeMarketOneConfiguration")["LogFilePath"];

            if (!string.IsNullOrEmpty(logFilePath)) configuration.LogFilePath = logFilePath;
        }

        internal static IBaseConfiguration InitializeEnvironment(IConfigurationRoot configFile)
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

        internal static void InitializeBaseOnionSeedsEndPoint(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var seedsEndPoint = configFile.GetSection("FreeMarketOneConfiguration")["OnionSeedsEndPoint"];

            if (!string.IsNullOrEmpty(seedsEndPoint)) configuration.OnionSeedsEndPoint = seedsEndPoint;
        }

        internal static void InitializeBaseTorEndPoint(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var torEndPoint = configFile.GetSection("FreeMarketOneConfiguration")["TorEndPoint"];

            if (!string.IsNullOrEmpty(torEndPoint)) configuration.TorEndPoint = EndPointHelper.ParseIPEndPoint(torEndPoint);
        }

        internal static void InitializeMemoryPoolPaths(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var memoryBasePoolPath = configFile.GetSection("FreeMarketOneConfiguration")["MemoryBasePoolPath"];
            var memoryMarketPoolPath = configFile.GetSection("FreeMarketOneConfiguration")["MemoryMarketPoolPath"];

            if (!string.IsNullOrEmpty(memoryBasePoolPath)) configuration.MemoryBasePoolPath = memoryBasePoolPath;
            if (!string.IsNullOrEmpty(memoryMarketPoolPath)) configuration.MemoryMarketPoolPath = memoryMarketPoolPath;
        }

        internal static void InitializeBlockChainPaths(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var blockChainBasePath = configFile.GetSection("FreeMarketOneConfiguration")["BlockChainBasePath"];
            var blockChainMarketPath = configFile.GetSection("FreeMarketOneConfiguration")["BlockChainMarketPath"];
            var blockChainSecretPath = configFile.GetSection("FreeMarketOneConfiguration")["BlockChainSecretPath"];

            if (!string.IsNullOrEmpty(blockChainBasePath)) configuration.BlockChainBasePath = blockChainBasePath;
            if (!string.IsNullOrEmpty(blockChainMarketPath)) configuration.BlockChainMarketPath = blockChainMarketPath;
            if (!string.IsNullOrEmpty(blockChainSecretPath)) configuration.BlockChainSecretPath = blockChainSecretPath;
        }

        internal static void InitializeListenerEndPoints(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var baseEndPoint = configFile.GetSection("FreeMarketOneConfiguration")["ListenerBaseEndPoint"];
            var marketEndPoint = configFile.GetSection("FreeMarketOneConfiguration")["ListenerMarketEndPoint"];
            var useTor = configFile.GetSection("FreeMarketOneConfiguration")["ListenersUseTor"];

            if (!string.IsNullOrEmpty(baseEndPoint)) configuration.ListenerBaseEndPoint = EndPointHelper.ParseIPEndPoint(baseEndPoint);
            if (!string.IsNullOrEmpty(marketEndPoint)) configuration.ListenerMarketEndPoint = EndPointHelper.ParseIPEndPoint(marketEndPoint);
            if (!string.IsNullOrEmpty(useTor))
            {
                bool useTorParsed;
                if (bool.TryParse(useTor, out useTorParsed))
                {
                    configuration.ListenersUseTor = useTorParsed;
                }
            }

            //apply public IP address or force to use defined
            if (!configuration.ListenersUseTor)
            {
                if (string.IsNullOrEmpty(configuration.ListenersForceThisIp))
                {
                    var ipHelper = new IpHelper();
                    if (ipHelper.PublicIp != null)
                    {
                        SetToPublicIp(configuration.ListenerBaseEndPoint, ipHelper.PublicIp.MapToIPv4());
                        SetToPublicIp(configuration.ListenerMarketEndPoint, ipHelper.PublicIp.MapToIPv4());
                    }
                } 
                else
                {
                    IPAddress newIp;
                    if (IPAddress.TryParse(configuration.ListenersForceThisIp, out newIp))
                    {
                        SetToPublicIp(configuration.ListenerBaseEndPoint, newIp);
                        SetToPublicIp(configuration.ListenerMarketEndPoint, newIp);
                    }
                }
            }
        }

        private static void SetToPublicIp(EndPoint endPoint, IPAddress newIp)
        {
            ((IPEndPoint)endPoint).Address = newIp;
        }
    }
}
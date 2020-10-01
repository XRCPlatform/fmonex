using FreeMarketOne.DataStructure;
using FreeMarketOne.Extensions.Helpers;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

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

            var environment = BaseConfiguration.EnvironmentTypes.Test;
            Enum.TryParse(settings, out environment);

            if (environment == BaseConfiguration.EnvironmentTypes.Main)
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

        internal static void InitializeTorUsage(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var useTor = configFile.GetSection("FreeMarketOneConfiguration")["ListenersUseTor"];

            if (!string.IsNullOrEmpty(useTor))
            {
                bool useTorParsed;
                if (bool.TryParse(useTor, out useTorParsed))
                {
                    configuration.ListenersUseTor = useTorParsed;
                }
            }
        }

        internal static void InitializeListenerEndPoints(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var baseEndPoint = configFile.GetSection("FreeMarketOneConfiguration")["ListenerBaseEndPoint"];
            var marketEndPoint = configFile.GetSection("FreeMarketOneConfiguration")["ListenerMarketEndPoint"];
            var chatEndPoint = configFile.GetSection("FreeMarketOneConfiguration")["ListenerChatEndPoint"];

            if (!string.IsNullOrEmpty(baseEndPoint)) configuration.ListenerBaseEndPoint = EndPointHelper.ParseIPEndPoint(baseEndPoint);
            if (!string.IsNullOrEmpty(marketEndPoint)) configuration.ListenerMarketEndPoint = EndPointHelper.ParseIPEndPoint(marketEndPoint);
            if (!string.IsNullOrEmpty(chatEndPoint)) configuration.ListenerChatEndPoint = EndPointHelper.ParseIPEndPoint(chatEndPoint);

            //apply public IP address or force to use defined
            if (!configuration.ListenersUseTor)
            {
                if (string.IsNullOrEmpty(configuration.ListenersForceThisIp))
                {
                    var publicIp = FMONE.Current.ServerPublicAddress.PublicIP;
                    if (publicIp != null)
                    {
                        SetToPublicIp(configuration.ListenerBaseEndPoint, publicIp.MapToIPv4());
                        SetToPublicIp(configuration.ListenerMarketEndPoint, publicIp.MapToIPv4());
                        SetToPublicIp(configuration.ListenerChatEndPoint, publicIp.MapToIPv4());
                    }
                } 
                else
                {
                    IPAddress newIp;
                    if (IPAddress.TryParse(configuration.ListenersForceThisIp, out newIp))
                    {
                        SetToPublicIp(configuration.ListenerBaseEndPoint, newIp);
                        SetToPublicIp(configuration.ListenerMarketEndPoint, newIp);
                        SetToPublicIp(configuration.ListenerChatEndPoint, newIp);
                    }
                }
            }
        }

        private static void SetToPublicIp(EndPoint endPoint, IPAddress newIp)
        {
            ((IPEndPoint)endPoint).Address = newIp;
        }

        internal static void InitializeChatPaths(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var chatPath = configFile.GetSection("FreeMarketOneConfiguration")["ChatPath"];

            if (!string.IsNullOrEmpty(chatPath)) configuration.SearchEnginePath = chatPath;
        }

        internal static void InitializeSearchEnginePaths(IBaseConfiguration configuration, IConfigurationRoot configFile)
        {
            var searchEnginePath = configFile.GetSection("FreeMarketOneConfiguration")["SearchEnginePath"];

            if (!string.IsNullOrEmpty(searchEnginePath)) configuration.SearchEnginePath = searchEnginePath;
        }
    }
}

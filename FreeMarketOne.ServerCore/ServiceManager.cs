using FreeMarketOne.DataStructure;
using FreeMarketOne.Extensions.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeMarketOne.ServerCore
{
    public class ServiceManager
    {
        private ILogger _logger { get; set; }

        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped, 4: Mining
        /// </summary>
        private long _running;
        private IBaseConfiguration _configuration;

        private CancellationTokenSource _cancellationToken { get; set; }
        private IAsyncLoopFactory _asyncLoopFactory { get; set; }
        private string _appVersion { get; set; }

        public ServiceManager(IBaseConfiguration configuration)
        {
            _logger = Log.Logger.ForContext<ServiceManager>();
            _logger.Information("Initializing Onion Seeds Manager");

            _asyncLoopFactory = new AsyncLoopFactory(_logger);
            _configuration = configuration;
            _appVersion = configuration.Version;
            _cancellationToken = new CancellationTokenSource();
        }

        public bool IsRunning => Interlocked.Read(ref _running) == 1;

        public bool IsServiceManagerRunning()
        {
            if (Interlocked.Read(ref _running) == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Start()
        {
            Interlocked.Exchange(ref _running, 1);

            IAsyncLoop periodicLogLoop = this._asyncLoopFactory.Run("ServiceManagerChecker", (cancellation) =>
            {
                var dateTimeUtc = DateTime.UtcNow;

                StringBuilder periodicCheckLog = new StringBuilder();

                periodicCheckLog.AppendLine("======Service Manager Check====== " + dateTimeUtc.ToString(CultureInfo.InvariantCulture) + " agent " + _appVersion);

                CheckOnionSeedManager(periodicCheckLog);
                CheckTorManager(periodicCheckLog);
                CheckBaseBlockChainManager(periodicCheckLog);
                CheckBasePoolManager(periodicCheckLog);
                CheckMarketBlockChainManager(periodicCheckLog);
                CheckMarketPoolManager(periodicCheckLog);

                Console.WriteLine(periodicCheckLog.ToString());

                return Task.CompletedTask;
            },
            _cancellationToken.Token,
            repeatEvery: TimeSpans.TenSeconds,
            startAfter: TimeSpans.TenSeconds);
        }

        private void CheckOnionSeedManager(StringBuilder periodicCheckLog)
        {
            var state = false;
            try
            {
                if (FreeMarketOneServer.Current.OnionSeedsManager != null)
                {
                    var isOnionSeedRunning = FreeMarketOneServer.Current.OnionSeedsManager.IsOnionSeedsManagerRunning();
                    if (isOnionSeedRunning) state = true;
                }
            }
            catch (Exception ex)
            {
                state = false;
            }
            periodicCheckLog.AppendLine("OnionSeed Manager : " + (state ? "Active" : "Idle"));
        }

        private void CheckTorManager(StringBuilder periodicCheckLog)
        {
            var state = false;
            try
            {
                if (FreeMarketOneServer.Current.TorProcessManager != null)
                {
                    var isTorRunning = FreeMarketOneServer.Current.TorProcessManager.IsTorRunningAsync().Result;
                    if (isTorRunning) state = true;
                }
            }
            catch (Exception ex)
            {
                state = false;
            }
            periodicCheckLog.AppendLine("Tor Manager : " + (state ? "Active" : "Idle"));
        }

        private void CheckBaseBlockChainManager(StringBuilder periodicCheckLog)
        {
            var state = false;

            state = false;
            try
            {
                if (FreeMarketOneServer.Current.BaseBlockChainManager != null)
                {
                    var isBlockChainManagerRunning = FreeMarketOneServer.Current.BaseBlockChainManager.IsBlockChainManagerRunning();
                    if (isBlockChainManagerRunning) state = true;
                }
            }
            catch (Exception ex)
            {
                state = false;
            }
            periodicCheckLog.Append("Base Manager / Swarm / Storage / BlockChain : " + (state ? "Active" : "Idle"));

            state = false;
            try
            {
                if (FreeMarketOneServer.Current.BaseBlockChainManager != null)
                {
                    var isSwarpServerRunning = FreeMarketOneServer.Current.BaseBlockChainManager.SwarmServer.Running;
                    if (isSwarpServerRunning) state = true;
                }
            }
            catch (Exception ex)
            {
                state = false;
            }
            periodicCheckLog.Append(" / " + (state ? "Active" : "Idle"));

            state = false;
            try
            {
                if (FreeMarketOneServer.Current.BaseBlockChainManager != null)
                {
                    if (FreeMarketOneServer.Current.BaseBlockChainManager.Storage != null) state = true;
                }
            }
            catch (Exception ex)
            {
                state = false;
            }
            periodicCheckLog.Append(" / " + (state ? "Active" : "Idle"));

            state = false;
            try
            {
                if (FreeMarketOneServer.Current.BaseBlockChainManager != null)
                {
                    if (FreeMarketOneServer.Current.BaseBlockChainManager.BlockChain != null) state = true;
                }
            }
            catch (Exception ex)
            {
                state = false;
            }
            periodicCheckLog.Append(" / " + (state ? "Active" : "Idle"));
            periodicCheckLog.AppendLine();
        }

        private void CheckMarketBlockChainManager(StringBuilder periodicCheckLog)
        {
            var state = false;
            try
            {
                if (FreeMarketOneServer.Current.MarketBlockChainManager != null)
                {
                    var isBlockChainManagerRunning = FreeMarketOneServer.Current.MarketBlockChainManager.IsBlockChainManagerRunning();
                    if (isBlockChainManagerRunning) state = true;
                }
            }
            catch (Exception ex)
            {
                state = false;
            }
            periodicCheckLog.Append("Market Manager / Swarm / Storage / BlockChain : " + (state ? "Active" : "Idle"));

            state = false;
            try
            {
                if (FreeMarketOneServer.Current.MarketBlockChainManager != null)
                {
                    var isSwarpServerRunning = FreeMarketOneServer.Current.MarketBlockChainManager.SwarmServer.Running;
                    if (isSwarpServerRunning) state = true;
                }
            }
            catch (Exception ex)
            {
                state = false;
            }
            periodicCheckLog.Append(" / " + (state ? "Active" : "Idle"));

            state = false;
            try
            {
                if (FreeMarketOneServer.Current.MarketBlockChainManager != null)
                {
                    if (FreeMarketOneServer.Current.MarketBlockChainManager.Storage != null) state = true;
                }
            }
            catch (Exception ex)
            {
                state = false;
            }
            periodicCheckLog.Append(" / " + (state ? "Active" : "Idle"));

            state = false;
            try
            {
                if (FreeMarketOneServer.Current.MarketBlockChainManager != null)
                {
                    if (FreeMarketOneServer.Current.MarketBlockChainManager.BlockChain != null) state = true;
                }
            }
            catch (Exception ex)
            {
                state = false;
            }
            periodicCheckLog.Append(" / " + (state ? "Active" : "Idle"));
            periodicCheckLog.AppendLine();
        }

        private void CheckBasePoolManager(StringBuilder periodicCheckLog)
        {
            var state = false;
            try
            {
                if (FreeMarketOneServer.Current.BasePoolManager != null)
                {
                    var isPoolManagerRunning = FreeMarketOneServer.Current.BasePoolManager.IsPoolManagerRunning();
                    if (isPoolManagerRunning) state = true;
                }
            }
            catch (Exception ex)
            {
                state = false;
            }
            periodicCheckLog.Append("Base Pool Manager / Mining : " + (state ? "Active" : "Idle"));

            state = false;
            try
            {
                if (FreeMarketOneServer.Current.BasePoolManager != null)
                {
                    var isMiningRunning = FreeMarketOneServer.Current.BasePoolManager.IsMiningWorkerRunning();
                    if (isMiningRunning) state = true;
                }
            }
            catch (Exception ex)
            {
                state = false;
            }
            periodicCheckLog.Append(" / " + (state ? "Active" : "Idle"));
            periodicCheckLog.AppendLine();
        }

        private void CheckMarketPoolManager(StringBuilder periodicCheckLog)
        {
            var state = false;

            try
            {
                if (FreeMarketOneServer.Current.MarketPoolManager != null)
                {
                    var isPoolManagerRunning = FreeMarketOneServer.Current.MarketPoolManager.IsPoolManagerRunning();
                    if (isPoolManagerRunning) state = true;
                }
            }
            catch (Exception ex)
            {
                state = false;
            }
            periodicCheckLog.Append("Market Pool Manager / Mining : " + (state ? "Active" : "Idle"));

            state = false;
            try
            {
                if (FreeMarketOneServer.Current.MarketPoolManager != null)
                {
                    var isMiningRunning = FreeMarketOneServer.Current.MarketPoolManager.IsMiningWorkerRunning();
                    if (isMiningRunning) state = true;
                }
            }
            catch (Exception ex)
            {
                state = false;
            }
            periodicCheckLog.Append(" / " + (state ? "Active" : "Idle"));
            periodicCheckLog.AppendLine();
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _running, 2);

            _cancellationToken.Cancel();

            Interlocked.Exchange(ref _running, 3);
        }
    }
}

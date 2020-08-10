using FreeMarketOne.DataStructure;
using FreeMarketOne.Extensions.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            startAfter: TimeSpans.FiveSeconds);
        }

        private bool CheckOnionSeedManager(StringBuilder periodicCheckLog = null)
        {
            var state = false;
            try
            {
                var isOnionSeedRunning = FreeMarketOneServer.Current.OnionSeedsManager?.IsOnionSeedsManagerRunning();
                if (isOnionSeedRunning.GetValueOrDefault(false)) state = true;
            }
            catch
            {
                state = false;
            }
            periodicCheckLog?.AppendLine("OnionSeed Manager : " + (state ? "Active" : "Idle"));

            return state;
        }

        private bool CheckTorManager(StringBuilder periodicCheckLog = null)
        {
            var state = false;
            try
            {
                var isTorRunning = FreeMarketOneServer.Current.TorProcessManager?.IsTorRunningAsync().Result;
                if (isTorRunning.GetValueOrDefault(false)) state = true;
            }
            catch
            {
                state = false;
            }
            periodicCheckLog?.AppendLine("Tor Manager : " + (state ? "Active" : "Idle"));

            return state;
        }

        private bool CheckBaseBlockChainManager(StringBuilder periodicCheckLog = null)
        {
            var state = false;
            var fullState = false;
            try
            {
                var isBlockChainManagerRunning = FreeMarketOneServer.Current.BaseBlockChainManager?.IsBlockChainManagerRunning();
                if (isBlockChainManagerRunning.GetValueOrDefault(false)) state = true;
            }
            catch
            {
                state = false;
            }
            periodicCheckLog?.Append("Base Manager|Swarm|Storage|BlockChain : " + (state ? "Active" : "Idle"));
            if (state) fullState = true;

            state = false;
            try
            {
                var isSwarpServerRunning = FreeMarketOneServer.Current.BaseBlockChainManager?.SwarmServer?.Running;
                if (isSwarpServerRunning.GetValueOrDefault(false)) state = true;
            }
            catch
            {
                state = false;
            }
            periodicCheckLog?.Append("|" + (state ? "Active" : "Idle"));
            if (state) fullState = true;

            state = false;
            long? index = 0;
            try
            {
                var chainId = FreeMarketOneServer.Current.BaseBlockChainManager?.Storage?.GetCanonicalChainId();
                var hashSets = FreeMarketOneServer.Current.BaseBlockChainManager?.Storage?.IterateBlockHashes().ToHashSet();
                if (chainId.HasValue)
                {
                    index = FreeMarketOneServer.Current.BaseBlockChainManager?.Storage?.CountIndex(chainId.Value);
                    if (index.HasValue) state = true;
                }
            }
            catch
            {
                state = false;
            }
            periodicCheckLog?.Append("|" + (state ? "Active (" + index + " index)" : "Idle"));
            if (state) fullState = true;

            state = false;
            index = 0;
            try
            {
                index = FreeMarketOneServer.Current.BaseBlockChainManager?.BlockChain?.Tip?.Index;
                if (index.HasValue) state = true;
            }
            catch
            {
                state = false;
            }
            periodicCheckLog?.Append("|" + (state ? "Active (" + index + " tip)" : "Idle"));
            if (state) fullState = true;

            periodicCheckLog?.AppendLine();

            return fullState;
        }

        private bool CheckMarketBlockChainManager(StringBuilder periodicCheckLog = null)
        {
            var state = false;
            var fullState = false;
            try
            {
                var isBlockChainManagerRunning = FreeMarketOneServer.Current.MarketBlockChainManager?.IsBlockChainManagerRunning();
                if (isBlockChainManagerRunning.GetValueOrDefault(false)) state = true;
            }
            catch
            {
                state = false;
            }
            periodicCheckLog?.Append("Market Manager|Swarm|Storage|BlockChain : " + (state ? "Active" : "Idle"));
            if (state) fullState = true;

            state = false;
            try
            {
                var isSwarpServerRunning = FreeMarketOneServer.Current.MarketBlockChainManager?.SwarmServer?.Running;
                if (isSwarpServerRunning.GetValueOrDefault(false)) state = true;
            }
            catch
            {
                state = false;
            }
            periodicCheckLog?.Append("|" + (state ? "Active" : "Idle"));
            if (state) fullState = true;

            state = false;
            long? index = 0;
            try
            {
                var chainId = FreeMarketOneServer.Current.MarketBlockChainManager?.Storage?.GetCanonicalChainId();
                var hashSets = FreeMarketOneServer.Current.MarketBlockChainManager?.Storage?.IterateBlockHashes().ToHashSet();
                if (chainId.HasValue)
                {
                    index = FreeMarketOneServer.Current.MarketBlockChainManager?.Storage?.CountIndex(chainId.Value);
                    if (index.HasValue) state = true;
                }
            }
            catch
            {
                state = false;
            }
            periodicCheckLog?.Append("|" + (state ? "Active (" + index + " index)" : "Idle"));
            if (state) fullState = true;

            state = false;
            index = 0;
            try
            {
                index = FreeMarketOneServer.Current.MarketBlockChainManager?.BlockChain?.Tip?.Index;
                if (index.HasValue) state = true;
            }
            catch
            {
                state = false;
            }
            periodicCheckLog?.Append("|" + (state ? "Active (" + index + " tip) " : "Idle"));
            if (state) fullState = true;

            periodicCheckLog?.AppendLine();

            return fullState;
        }

        internal FreeMarketOneServer.FreeMarketOneServerStates GetServerState()
        {
            if (CheckOnionSeedManager() &&
                CheckTorManager() &&
                CheckBaseBlockChainManager() &&
                CheckBasePoolManager() &&
                CheckMarketBlockChainManager() &&
                CheckMarketPoolManager())
            {
                return FreeMarketOneServer.FreeMarketOneServerStates.Online;
            } 
            else
            {
                return FreeMarketOneServer.FreeMarketOneServerStates.Offline;
            }
        }

        private bool CheckBasePoolManager(StringBuilder periodicCheckLog = null)
        {
            var state = false;
            var fullState = false;
            var entries = 0;
            try
            {
                var isPoolManagerRunning = FreeMarketOneServer.Current.BasePoolManager?.IsPoolManagerRunning();
                if (isPoolManagerRunning.GetValueOrDefault(false))
                {
                    state = true;
                    entries = FreeMarketOneServer.Current.BasePoolManager.GetAllActionItemLocal().Count;
                }
            }
            catch
            {
                state = false;
            }
            periodicCheckLog?.Append("Base Pool Manager|Mining : " + (state ? "Active (" + entries + " actions)" : "Idle"));
            if (state) fullState = true;

            state = false;
            try
            {
                var isMiningRunning = FreeMarketOneServer.Current.BasePoolManager?.IsMiningWorkerRunning();
                if (isMiningRunning.GetValueOrDefault(false)) state = true;
            }
            catch
            {
                state = false;
            }
            periodicCheckLog?.Append("|" + (state ? "Active" : "Idle"));
            periodicCheckLog?.AppendLine();

            return fullState;
        }

        private bool CheckMarketPoolManager(StringBuilder periodicCheckLog = null)
        {
            var state = false;
            var fullState = false;
            var entries = 0;
            try
            {
                var isPoolManagerRunning = FreeMarketOneServer.Current.MarketPoolManager?.IsPoolManagerRunning();
                if (isPoolManagerRunning.GetValueOrDefault(false))
                {
                    state = true;
                    entries = FreeMarketOneServer.Current.MarketPoolManager.GetAllActionItemLocal().Count;
                }
            }
            catch
            {
                state = false;
            }
            periodicCheckLog?.Append("Market Pool Manager|Mining : " + (state ? "Active (" + entries + " actions) " : "Idle"));
            if (state) fullState = true;

            state = false;
            try
            {
                var isMiningRunning = FreeMarketOneServer.Current.MarketPoolManager?.IsMiningWorkerRunning();
                if (isMiningRunning.GetValueOrDefault(false)) state = true;
            }
            catch {
                state = false;
            }
            periodicCheckLog?.Append("|" + (state ? "Active" : "Idle"));
            periodicCheckLog?.AppendLine();

            return fullState;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _running, 2);

            _cancellationToken.Cancel();

            Interlocked.Exchange(ref _running, 3);
        }
    }
}

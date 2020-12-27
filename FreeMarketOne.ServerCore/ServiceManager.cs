using FreeMarketOne.DataStructure;
using FreeMarketOne.Extensions.Helpers;
using Serilog;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static FreeMarketOne.Extensions.Common.ServiceHelper;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketOne.ServerCore
{
    public class ServiceManager
    {
        private ILogger _logger { get; set; }

        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped
        /// </summary>
        private CommonStates _running;
        private IBaseConfiguration _configuration;

        private CancellationTokenSource _cancellationToken { get; set; }
        private IAsyncLoopFactory _asyncLoopFactory { get; set; }
        private string _appVersion { get; set; }

        private DateTimeOffset? _expectedMarketChainPulse;
        private DateTimeOffset? _expectedBaseChainPulse;
        private readonly object _swarmRecoveryLock = new object();
        private EventHandler<NetworkHeartbeatArgs> _networkHeartbeatEvent;

        public ServiceManager(IBaseConfiguration configuration, 
            EventHandler<NetworkHeartbeatArgs> networkHeartbeatEvent)
        {
            _logger = Log.Logger.ForContext<ServiceManager>();
            _logger.Information("Initializing Onion Seeds Manager");

            _asyncLoopFactory = new AsyncLoopFactory(_logger);
            _configuration = configuration;
            _appVersion = configuration.Version;
            _cancellationToken = new CancellationTokenSource();

            _networkHeartbeatEvent = networkHeartbeatEvent;
        }

        public bool IsRunning => _running == CommonStates.Running;

        public bool IsServiceManagerRunning()
        {
            if (_running == CommonStates.Running)
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
            _running = CommonStates.Running;

            //Service loop checker
            IAsyncLoop periodicLogLoop = this._asyncLoopFactory.Run("ServiceManagerChecker", (cancellation) =>
            {
                var dateTimeUtc = DateTime.UtcNow;
                StringBuilder periodicCheckLog = new StringBuilder();

                periodicCheckLog.AppendLine("======Service Manager Check====== " + dateTimeUtc.ToString(CultureInfo.InvariantCulture) + " agent " + _appVersion);

                //publishing info to GUI
                var networkHeartbeatInfo = new NetworkHeartbeatArgs();

                CheckOnionSeedManager(periodicCheckLog);
                CheckTorManager(periodicCheckLog, networkHeartbeatInfo);
                CheckBaseBlockChainManager(periodicCheckLog, networkHeartbeatInfo);
                CheckBasePoolManager(periodicCheckLog, networkHeartbeatInfo);
                CheckMarketBlockChainManager(periodicCheckLog, networkHeartbeatInfo);
                CheckMarketPoolManager(periodicCheckLog, networkHeartbeatInfo);
                CheckChatManager(periodicCheckLog);

                Console.WriteLine(periodicCheckLog.ToString());

                _networkHeartbeatEvent.Invoke(this, networkHeartbeatInfo);

                return Task.CompletedTask;
            },
            _cancellationToken.Token,
            repeatEvery: TimeSpans.TenSeconds,
            startAfter: TimeSpans.FiveSeconds);

            //Network HeartBeat loop checker
            IAsyncLoop periodicNetworkHeartbeatLoop = this._asyncLoopFactory.Run("NetworkHeartbeatChecker", (cancellation) =>
            {
                //Network Heartbeat validation
                if ((FMONE.Current.MarketBlockChainManager != null)
                    && (FMONE.Current.MarketBlockChainManager.IsBlockChainManagerRunning()))
                {
                    if (!_expectedMarketChainPulse.HasValue) _expectedMarketChainPulse = DateTimeOffset.UtcNow;
                    if (!_expectedBaseChainPulse.HasValue) _expectedBaseChainPulse = DateTimeOffset.UtcNow;

                    ValidateNetworkHeartbeat();
                }

                return Task.CompletedTask;
            },
            _cancellationToken.Token,
            repeatEvery: TimeSpans.HalfMinute,
            startAfter: TimeSpans.TwentySeconds);
        }

        private void ValidateNetworkHeartbeat()
        {
            bool baseUp = true;
            bool marketUp = true;

            var marketDiff = _expectedMarketChainPulse - FMONE.Current.MarketBlockChainManager.SwarmServer.LastMessageTimestamp;
            var baseDiff = _expectedBaseChainPulse - FMONE.Current.BaseBlockChainManager.SwarmServer.LastMessageTimestamp;
            if ((marketDiff.HasValue) && (marketDiff.Value.TotalMinutes > 1))
            {
                marketUp = false;
            }
            if ((baseDiff.HasValue) && (baseDiff.Value.TotalMinutes > 1))
            {
                baseUp = false;
            }

            _expectedMarketChainPulse = DateTimeOffset.UtcNow;
            _expectedBaseChainPulse = DateTimeOffset.UtcNow;

            //skip on inicialization
            if ((!marketUp || !FMONE.Current.MarketBlockChainManager.SwarmServer.Peers.Any()) 
                && FMONE.Current.MarketBlockChainManager.IsBlockChainManagerRunning())
            {
                lock (_swarmRecoveryLock)
                {
                    //if this should refresh the swarm server if network was down and recovered.
                    FMONE.Current.MarketBlockChainManager.ReConnectAfterNetworkLossAsync();
                }
            }

            if ((!baseUp || !FMONE.Current.BaseBlockChainManager.SwarmServer.Peers.Any()) 
                && FMONE.Current.BaseBlockChainManager.IsBlockChainManagerRunning())
            {
                lock (_swarmRecoveryLock)
                {
                    //if this should refresh the swarm server if network was down and recovered.
                    FMONE.Current.BaseBlockChainManager.ReConnectAfterNetworkLossAsync();
                }
            }
        }

        private bool CheckOnionSeedManager(StringBuilder periodicCheckLog = null)
        {
            var state = false;
            try
            {
                var isOnionSeedRunning = FMONE.Current.OnionSeedsManager?.IsOnionSeedsManagerRunning();
                if (isOnionSeedRunning.GetValueOrDefault(false)) state = true;
            }
            catch
            {
                state = false;
            }
            periodicCheckLog?.AppendLine("OnionSeed Manager : " + (state ? "Active" : "Idle"));

            return state;
        }

        private bool CheckTorManager(StringBuilder periodicCheckLog = null, NetworkHeartbeatArgs networkHeartbeatInfo = null)
        {
            var state = false;
            try
            {
                var isTorRunning = FMONE.Current.TorProcessManager?.IsTorRunningAsync().Result;
                if (isTorRunning.GetValueOrDefault(false)) state = true;
            }
            catch
            {
                state = false;
            }
            periodicCheckLog?.AppendLine("Tor Manager : " + (state ? "Active" : "Idle"));

            if (networkHeartbeatInfo != null) networkHeartbeatInfo.IsTorUp = state;

            return state;
        }

        private bool CheckBaseBlockChainManager(StringBuilder periodicCheckLog = null, NetworkHeartbeatArgs networkHeartbeatInfo = null)
        {
            var state = false;
            var fullState = false;
            try
            {
                var isBlockChainManagerRunning = FMONE.Current.BaseBlockChainManager?.IsBlockChainManagerRunning();
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
                var isSwarpServerRunning = FMONE.Current.BaseBlockChainManager?.SwarmServer?.Running;
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
                var chainId = FMONE.Current.BaseBlockChainManager?.Storage?.GetCanonicalChainId();
                var hashSets = FMONE.Current.BaseBlockChainManager?.Storage?.IterateBlockHashes().ToHashSet();
                if (chainId.HasValue)
                {
                    index = FMONE.Current.BaseBlockChainManager?.Storage?.CountIndex(chainId.Value);
                    if (index.HasValue) state = true;

                    if (networkHeartbeatInfo != null) networkHeartbeatInfo.BaseHeight = index.GetValueOrDefault(0);
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
                index = FMONE.Current.BaseBlockChainManager?.BlockChain?.Tip?.Index;
                if (index.HasValue) state = true;
            }
            catch
            {
                state = false;
            }
            periodicCheckLog?.Append("|" + (state ? "Active (" + index + " tip)" : "Idle"));
            if (state) fullState = true;

            periodicCheckLog?.AppendLine();

            if (networkHeartbeatInfo != null) networkHeartbeatInfo.IsBaseChainNetworkConnected = fullState;

            return fullState;
        }

        private bool CheckMarketBlockChainManager(StringBuilder periodicCheckLog = null, NetworkHeartbeatArgs networkHeartbeatInfo = null)
        {
            var state = false;
            var fullState = false;
            try
            {
                var isBlockChainManagerRunning = FMONE.Current.MarketBlockChainManager?.IsBlockChainManagerRunning();
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
                var isSwarpServerRunning = FMONE.Current.MarketBlockChainManager?.SwarmServer?.Running;
                if (isSwarpServerRunning.GetValueOrDefault(false))
                {
                    state = true;

                    if (networkHeartbeatInfo != null) networkHeartbeatInfo.PeerCount 
                            = FMONE.Current.MarketBlockChainManager.SwarmServer.Peers.Count();
                }
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
                var chainId = FMONE.Current.MarketBlockChainManager?.Storage?.GetCanonicalChainId();
                var hashSets = FMONE.Current.MarketBlockChainManager?.Storage?.IterateBlockHashes().ToHashSet();
                if (chainId.HasValue)
                {
                    index = FMONE.Current.MarketBlockChainManager?.Storage?.CountIndex(chainId.Value);
                    if (index.HasValue) state = true;

                    if (networkHeartbeatInfo != null) networkHeartbeatInfo.MarketHeight = index.GetValueOrDefault(0);
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
                index = FMONE.Current.MarketBlockChainManager?.BlockChain?.Tip?.Index;
                if (index.HasValue) state = true;
            }
            catch
            {
                state = false;
            }
            periodicCheckLog?.Append("|" + (state ? "Active (" + index + " tip) " : "Idle"));
            if (state) fullState = true;

            periodicCheckLog?.AppendLine();

            if (networkHeartbeatInfo != null) networkHeartbeatInfo.IsMarketChainNetworkConnected = fullState;

            return fullState;
        }

        internal FMONE.FreeMarketOneServerStates GetServerState()
        {
            if (CheckOnionSeedManager() &&
                CheckTorManager() &&
                CheckBaseBlockChainManager() &&
                CheckBasePoolManager() &&
                CheckMarketBlockChainManager() &&
                CheckMarketPoolManager())
            {
                return FMONE.FreeMarketOneServerStates.Online;
            } 
            else
            {
                return FMONE.FreeMarketOneServerStates.Offline;
            }
        }

        private bool CheckBasePoolManager(StringBuilder periodicCheckLog = null, NetworkHeartbeatArgs networkHeartbeatInfo = null)
        {
            var state = false;
            var fullState = false;
            var entries = 0;
            try
            {
                var isPoolManagerRunning = FMONE.Current.BasePoolManager?.IsPoolManagerRunning();
                if (isPoolManagerRunning.GetValueOrDefault(false))
                {
                    state = true;
                    entries = FMONE.Current.BasePoolManager.GetTotalCount();

                    if (networkHeartbeatInfo != null)
                    {
                        networkHeartbeatInfo.PoolBaseLocalItemsCount = FMONE.Current.BasePoolManager.GetAllActionItemLocalCount();
                        networkHeartbeatInfo.PoolBaseStagedItemsCount = FMONE.Current.BasePoolManager.GetAllActionItemStagedCount();
                    }
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
                var isMiningRunning = FMONE.Current.BasePoolManager?.IsMiningWorkerRunning();
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

        private bool CheckMarketPoolManager(StringBuilder periodicCheckLog = null, NetworkHeartbeatArgs networkHeartbeatInfo = null)
        {
            var state = false;
            var fullState = false;
            var entries = 0;
            try
            {
                var isPoolManagerRunning = FMONE.Current.MarketPoolManager?.IsPoolManagerRunning();
                if (isPoolManagerRunning.GetValueOrDefault(false))
                {
                    state = true;
                    entries = FMONE.Current.MarketPoolManager.GetTotalCount();

                    if (networkHeartbeatInfo != null)
                    {
                        networkHeartbeatInfo.PoolMarketLocalItemsCount = FMONE.Current.MarketPoolManager.GetAllActionItemLocalCount();
                        networkHeartbeatInfo.PoolMarketStagedItemsCount = FMONE.Current.MarketPoolManager.GetAllActionItemStagedCount();
                    }
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
                var isMiningRunning = FMONE.Current.MarketPoolManager?.IsMiningWorkerRunning();
                if (isMiningRunning.GetValueOrDefault(false)) state = true;
            }
            catch {
                state = false;
            }
            periodicCheckLog?.Append("|" + (state ? "Active" : "Idle"));
            periodicCheckLog?.AppendLine();

            return fullState;
        }

        private bool CheckChatManager(StringBuilder periodicCheckLog = null)
        {
            var state = false;
            var fullState = false;

            try
            {
                var isChatManagerRunning = FMONE.Current.ChatManager?.IsChatManagerRunning();
                if (isChatManagerRunning.GetValueOrDefault(false))
                {
                    state = true;
                }
            }
            catch
            {
                state = false;
            }

            periodicCheckLog?.Append("Chat Manager : " + (state ? "Active" : "Idle"));
            periodicCheckLog?.AppendLine();
            if (state) fullState = true;

            return fullState;
        }

        public void Dispose()
        {
            _running = CommonStates.Stopping;

            _cancellationToken.Cancel();

            _running = CommonStates.Stopped;
        }
    }
}

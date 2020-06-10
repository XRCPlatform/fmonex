using FreeMarketOne.BlockChain;
using FreeMarketOne.BlockChain.Policy;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Crypto;
using Libplanet.Extensions;
using Libplanet.Extensions.Helpers;
using Libplanet.Net;
using Libplanet.RocksDBStore;
using Libplanet.Tx;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FreeMarketOne.PoolManager
{
    public class PoolManager<T> : IPoolManager, IDisposable where T : IBaseAction, new()
    {
        private ILogger _logger { get; set; }

        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped, 4: Mining
        /// </summary>
        private long _running;

        public bool IsRunning => Interlocked.Read(ref _running) == 1;
        private CancellationTokenSource _cancellationToken { get; set; }

        private List<IBaseItem> _actionItemsList { get; set; }

        private readonly object _pollLock;
        private string _memoryPoolFilePath { get; set; }
        private IBaseConfiguration _configuration { get; }

        private RocksDBStore _storage;
        private Swarm<T> _swarmServer;
        private PrivateKey _privateKey;
        private BlockChain<T> _blockChain;

        private ProofOfWorkWorker<T> _proofOfWorkWorker { get; set; }
        private IDefaultBlockPolicy<T> _blockPolicy { get; set; }
        private IAsyncLoopFactory _asyncLoopFactory { get; set; }

        /// <summary>
        /// Base pool manager
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="memoryPoolFilePath"></param>
        /// <param name="storage"></param>
        /// <param name="swarmServer"></param>
        /// <param name="privateKey"></param>
        /// <param name="blockChain"></param>
        /// <param name="blockPolicy"></param>
        public PoolManager(
            IBaseConfiguration configuration,
            string memoryPoolFilePath,
            RocksDBStore storage,
            Swarm<T> swarmServer,
            PrivateKey privateKey,
            BlockChain<T> blockChain,
            IDefaultBlockPolicy<T> blockPolicy)
        {
            _logger = Log.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                string.Format("{0}.{1}.{2}", typeof(PoolManager<T>).Namespace, typeof(PoolManager<T>).Name.Replace("`1", string.Empty), typeof(T).Name));

            _configuration = configuration;
            _actionItemsList = new List<IBaseItem>();
            _pollLock = new object();
            _memoryPoolFilePath = memoryPoolFilePath;
            _storage = storage;
            _privateKey = privateKey;
            _swarmServer = swarmServer;
            _blockChain = blockChain;
            _blockPolicy = blockPolicy;

            _asyncLoopFactory = new AsyncLoopFactory(_logger);

            _proofOfWorkWorker = new ProofOfWorkWorker<T>(
                    _logger,
                    _swarmServer,
                    _blockChain,
                    _privateKey.ToAddress(),
                    _storage,
                    _privateKey
                    );

            _logger.Information("Initializing Base Pool Manager");

            Interlocked.Exchange(ref _running, 0);
            _cancellationToken = new CancellationTokenSource();

            LoadActionItemsFromFile();
        }

        public bool Start()
        {
            Interlocked.Exchange(ref _running, 1);

            var coProofOfWorkRunner = new CoroutineManager();
            coProofOfWorkRunner.RegisterCoroutine(_proofOfWorkWorker.GetEnumerator());

            var miningDelayStart = DateTime.MinValue;

            var periodicLogLoop = this._asyncLoopFactory.Run("Mining_" + typeof(T).Name, (cancellation) =>
            {
                _logger.Information("Initializing Mining Loop Checker");

                var actionStaged = GetAllActionItemStaged();
                var actionLocal = GetAllActionItemLocal();

                //check if mem pool have tx if yes then do mining
                if ((actionLocal.Count > 0) || (actionStaged.Count > 0))
                {
                    if (Interlocked.Read(ref _running) == 4)
                    {
                        if ((miningDelayStart >= DateTime.UtcNow) && (!coProofOfWorkRunner.IsActive))
                        {
                            _logger.Information(string.Format("Starting mining after mining delay."));
                            coProofOfWorkRunner.Start();
                        }
                    }
                    else
                    {
                        _logger.Information(string.Format("Found new actions in pools. Local {0}. Staged {1}.", actionLocal.Count, actionStaged.Count));

                        Interlocked.Exchange(ref _running, 4);
                        miningDelayStart = DateTime.UtcNow.Add(_blockPolicy.BlockInterval);
                    }
                }
                else
                {
                    if ((coProofOfWorkRunner.IsActive) || (Interlocked.Read(ref _running) == 4))
                    {
                        _logger.Information("Stopping Mining Loop Checker.");
                        Interlocked.Exchange(ref _running, 1);

                        coProofOfWorkRunner.Stop();
                    }
                }

                return Task.CompletedTask;
            },
            _cancellationToken.Token,
            repeatEvery: _blockPolicy.PoolCheckInterval);

            return true;
        }

        public bool IsPoolManagerRunning()
        {
            if ((Interlocked.Read(ref _running) == 1) || (Interlocked.Read(ref _running) == 4))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Stop()
        {
            Interlocked.Exchange(ref _running, 2);

            SaveActionItemsToFile();

            _cancellationToken?.Cancel();
            _cancellationToken?.Dispose();
            _cancellationToken = null;

            _logger.Information("Base Pool Manager stopped.");
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _running, 3);
            Stop();
        }

        public bool AcceptActionItem(IBaseItem actionItem)
        {
            if (CheckActionItemInProcessing(actionItem))
            {
                _actionItemsList.Add(actionItem);

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SaveActionItemsToFile()
        {
            lock (_pollLock)
            {
                _logger.Information("Saving action items data.");

                var serializedMemory = JsonConvert.SerializeObject(_actionItemsList);
                var compressedMemory = ZipHelpers.Compress(serializedMemory);

                var targetFilePath = Path.Combine(_configuration.FullBaseDirectory, _memoryPoolFilePath);
                var targetDirectory = Path.GetDirectoryName(targetFilePath);

                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                File.WriteAllBytes(targetFilePath, compressedMemory);

                _logger.Information("Action items data saved.");
            }

            return true;
        }

        public bool LoadActionItemsFromFile()
        {
            lock (_pollLock)
            {
                _logger.Information("Loading action items data.");

                var targetFilePath = Path.Combine(_configuration.FullBaseDirectory, _memoryPoolFilePath);
                var targetDirectory = Path.GetDirectoryName(targetFilePath);

                if (File.Exists(targetFilePath))
                {
                    var compressedMemory = File.ReadAllBytes(targetFilePath);
                    var serializedMemory = ZipHelpers.Decompress(compressedMemory);

                    var temporaryMemoryActionItemsList = JsonConvert.DeserializeObject<List<IBaseItem>>(serializedMemory);

                    //check all loaded tx in list
                    foreach (var itemTx in temporaryMemoryActionItemsList)
                    {
                        if (CheckActionItemInProcessing(itemTx))
                        {
                            _actionItemsList.Add(itemTx);
                        }
                    }
                }

                _logger.Information("Action items data loaded.");
            }

            return true;
        }

        public bool CheckActionItemInProcessing(IBaseItem actionItem)
        {
            if (actionItem.IsValid() && !_actionItemsList.Exists(mt => mt == actionItem))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ClearActionItemsBasedOnHashes(List<string> hashsToRemove)
        {
            foreach (var itemHash in hashsToRemove)
            {
                var actionItemsToRemove = _actionItemsList.FirstOrDefault(a => a.Hash == itemHash);

                if (actionItemsToRemove != null) _actionItemsList.Remove(actionItemsToRemove);
            }

            return true;
        }

        public IBaseItem GetActionItemLocal(string hash)
        {
            return _actionItemsList.FirstOrDefault(a => a.Hash == hash);
        }

        public List<IBaseItem> GetAllActionItemLocal()
        {
            return _actionItemsList;
        }

        public bool DeleteActionItemLocal(string hash)
        {
            var actionItem = _actionItemsList.FirstOrDefault(a => a.Hash == hash);
          
            if (actionItem != null) {
            
                _actionItemsList.Remove(actionItem);
                return true;
            } 
            else
            {
                return false;
            }
        }

        public bool PropagateAllActionItemLocal(List<IBaseAction> extraActions = null)
        {
            var actions = new List<T>();
            var action = new T();

            try
            {
                action.BaseItems.AddRange(_actionItemsList);
                actions.Add(action);

                if ((extraActions != null) && extraActions.Any())
                {
                    foreach (var extraAction in extraActions)
                    {
                        actions.Add((T)extraAction);
                    }
                }

                var tx = Transaction<T>.Create(
                    0,
                    _privateKey,
                    actions);

                _logger.Information(string.Format("Propagation of new transaction {0}.", tx.Id));

                _storage.PutTransaction(tx);
                _swarmServer.BroadcastTxs(new [] { tx });

                _logger.Information("Clearing all item actions from local pool.");
                _actionItemsList.Clear();

                return true;
            }
            catch (Exception e)
            {
                _logger.Error("Unexpected error suring propagation of transaction.", e);
                return false;
            } 
        }

        public List<IBaseItem> GetAllActionItemStaged()
        {
            var result = new List<IBaseItem>();

            foreach (var itemTxId in _storage.IterateStagedTransactionIds())
            {
                var transaction = _storage.GetTransaction<T>(itemTxId);

                if (transaction.Actions.Any())
                {
                    foreach (var action in transaction.Actions)
                    {
                        if (action.BaseItems.Any())
                        {
                            foreach (var itemAction in action.BaseItems)
                            {
                                result.Add(itemAction);
                            }
                        }
                    }
                }
            }

            return result;
        }

        public IBaseItem GetActionItemStaged(string hash)
        {
            foreach (var itemTxId in _storage.IterateStagedTransactionIds())
            {
                var transaction = _storage.GetTransaction<T>(itemTxId);

                if (transaction.Actions.Any())
                {
                    foreach (var action in transaction.Actions)
                    {
                        if (action.BaseItems.Any())
                        {
                            foreach (var itemAction in action.BaseItems)
                            {
                                if (itemAction.Hash == hash)
                                {
                                    return itemAction;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        public List<IBaseAction> GetAllActionStaged()
        {
            var result = new List<IBaseAction>();

            foreach (var itemTxId in _storage.IterateStagedTransactionIds())
            {
                var transaction = _storage.GetTransaction<T>(itemTxId);

                if (transaction.Actions.Any())
                {
                    foreach (var action in transaction.Actions)
                    {
                        result.Add(action);
                    }
                }
            }

            return result;
        }
    }
}

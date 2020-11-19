﻿using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Extensions;
using Libplanet.Extensions.Helpers;
using Libplanet.Net;
using Libplanet.RocksDBStore;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FreeMarketOne.Pools
{
    public class PoolManager<T> : IPoolManager, IDisposable where T : IBaseAction, new()
    {
        private ILogger _logger { get; set; }

        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped, 4: Mining, 5: Mined
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

        private MiningWorker<T> _miningWorker { get; set; }
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
            _cancellationToken = new CancellationTokenSource();

            _miningWorker = new MiningWorker<T>(
                    _logger,
                    _swarmServer,
                    _blockChain,
                    _privateKey.ToAddress(),
                    _storage,
                    _privateKey,
                    _cancellationToken
                    );

            _logger.Information("Initializing Base Pool Manager");

            Interlocked.Exchange(ref _running, 0);

            LoadActionItemsFromFile();
        }

        public bool Start()
        {
            Interlocked.Exchange(ref _running, 1);
            _logger.Information("Initializing Mining Loop Checker");

            var coMiningRunner = new CoroutineManager();
            coMiningRunner.RegisterCoroutine(_miningWorker.GetEnumerator());

            var miningDelayStart = DateTime.MinValue;
            var oldMiningActionStagedCount = 0;

            //mining
            var periodicMiningLoop = this._asyncLoopFactory.Run("Mining_" + typeof(T).Name, (cancellation) =>
            {
                _logger.Information("Mining Loop Check");

                var actionStaged = GetAllActionItemStaged();

                //check if mem pool have tx if yes then do mining
                if (actionStaged.Count > 0)
                {
                    if ((Interlocked.Read(ref _running) == 4) || (Interlocked.Read(ref _running) == 5))
                    {
                        if (actionStaged.Count < oldMiningActionStagedCount)
                        {
                            _logger.Information("Stopping Mining Loop Checker.");

                            coMiningRunner.Stop();
                            coMiningRunner.RegisterCoroutine(_miningWorker.GetEnumerator());
                            oldMiningActionStagedCount = 0;

                            Interlocked.Exchange(ref _running, 1);
                        }

                        if (!coMiningRunner.IsActive)
                        {
                            if ((miningDelayStart <= DateTime.UtcNow) && (Interlocked.Read(ref _running) != 5))
                            {
                                _logger.Information(string.Format("Starting mining after mining delay."));
                                coMiningRunner.Start();

                                Interlocked.Exchange(ref _running, 5);
                            }
                            else if (Interlocked.Read(ref _running) == 5)
                            {
                                _logger.Information("Stopping Mining Loop Checker.");

                                coMiningRunner.Stop();
                                coMiningRunner.RegisterCoroutine(_miningWorker.GetEnumerator());
                                oldMiningActionStagedCount = 0;

                                Interlocked.Exchange(ref _running, 1);
                            }
                        } 
                    }
                    else
                    {
                        _logger.Information(string.Format("Found new actions in pools. Staged {0}.", actionStaged.Count));

                        miningDelayStart = DateTime.UtcNow.Add(_blockPolicy.BlockInterval);
                        oldMiningActionStagedCount = actionStaged.Count;

                        Interlocked.Exchange(ref _running, 4);
                    }
                }
                else
                {
                    if ((coMiningRunner.IsActive) || (Interlocked.Read(ref _running) == 4) || (Interlocked.Read(ref _running) == 5))
                    {
                        _logger.Information("Stopping Mining Loop Checker.");

                        coMiningRunner.Stop();
                        coMiningRunner.RegisterCoroutine(_miningWorker.GetEnumerator());
                        oldMiningActionStagedCount = 0;

                        Interlocked.Exchange(ref _running, 1);
                    }
                }

                return Task.CompletedTask;
            },
            _cancellationToken.Token,
            repeatEvery: _blockPolicy.PoolCheckInterval);

            DateTime? broadcastTxDelayStart = DateTime.MinValue;

            //auto broadcast tx
            var periodicBroadcastTx = this._asyncLoopFactory.Run("BroadcastTx_" + typeof(T).Name, (cancellation) =>
            {
                _logger.Information("Broadcast Tx Loop Check");

                var actionLocal = GetAllActionItemLocal();
                if (actionLocal.Count() > 0)
                {
                    if (broadcastTxDelayStart.HasValue && (broadcastTxDelayStart <= DateTime.UtcNow))
                    {
                        _logger.Information("Propagate Tx from Local pool");

                        PropagateAllActionItemLocal();
                        broadcastTxDelayStart = null;
                    } 
                    else
                    {
                        if (broadcastTxDelayStart == null) broadcastTxDelayStart = DateTime.UtcNow.Add(TimeSpans.TenSeconds);
                    }
                } 
                else
                {
                    broadcastTxDelayStart = null;
                }

                return Task.CompletedTask;
            },
            _cancellationToken.Token,
            repeatEvery: _configuration.PoolPeriodicBroadcastTxInterval,
            startAfter: TimeSpans.TenSeconds);

            return true;
        }

        public bool IsPoolManagerRunning()
        {
            if ((Interlocked.Read(ref _running) == 1) || (Interlocked.Read(ref _running) == 4) || (Interlocked.Read(ref _running) == 5))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsMiningWorkerRunning()
        {
            if ((Interlocked.Read(ref _running) == 4) || (Interlocked.Read(ref _running) == 5))
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

        public PoolManagerStates.Errors? AcceptActionItem(IBaseItem actionItem)
        {
            if (_swarmServer.Peers.Count() >= _configuration.MinimalPeerAmount)
            {
                var isValid = CheckActionItemInProcessing(actionItem);

                if (isValid == null) _actionItemsList.Add(actionItem);

                return isValid;
            } 
            else
            {
                return PoolManagerStates.Errors.NoMinimalPeer;
            }
        }

        public bool SaveActionItemsToFile()
        {
            lock (_pollLock)
            {
                _logger.Information("Saving action items data.");

                var serializedMemory = JsonConvert.SerializeObject(_actionItemsList);
                var compressedMemory = ZipHelper.Compress(serializedMemory);

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
                    var serializedMemory = ZipHelper.Decompress(compressedMemory);

                    var temporaryMemoryActionItemsList = JsonConvert.DeserializeObject<List<IBaseItem>>(serializedMemory);

                    //check all loaded tx in list
                    foreach (var itemTx in temporaryMemoryActionItemsList)
                    {
                        if (CheckActionItemInProcessing(itemTx) == null)
                        {
                            _actionItemsList.Add(itemTx);
                        }
                    }
                }

                _logger.Information("Action items data loaded.");
            }

            return true;
        }

        public PoolManagerStates.Errors? CheckActionItemInProcessing(IBaseItem actionItem)
        {
            if (actionItem.IsValid() && !_actionItemsList.Exists(mt => mt == actionItem))
            {
                //Verify type of item in tx
                if (!((IDefaultBlockPolicy<T>)_blockChain.Policy).ValidTypesOfActionItems.Contains(actionItem.GetType()))
                {
                    return PoolManagerStates.Errors.WrontTypeOfContent;
                }

                //Verify existence of equal action item in unstaged tx
                if (ExistInStagedTransactions(actionItem))
                {
                    return PoolManagerStates.Errors.Duplication;
                }

                //Verification based on type
                if (actionItem.GetType() == typeof(MarketItemV1) && (!IsMarketItemValid(actionItem)))
                {
                    return PoolManagerStates.Errors.StateOfItemIsInProgress;
                }

                //Verification based on type
                if (actionItem.GetType() == typeof(UserDataV1) && (!IsUserDataValid(actionItem)))
                {
                    return PoolManagerStates.Errors.StateOfItemIsInProgress;
                }

                return null;
            }
            else
            {
                return PoolManagerStates.Errors.NoValidContentHash;
            }
        }

        private bool IsUserDataValid(IBaseItem actionItem)
        {
            var userData = (UserDataV1)actionItem;
            if (string.IsNullOrEmpty(userData.BaseSignature)) return true;

            //Checking existence of chain of identical items in pool
            foreach (var itemLocalPoolItem in _actionItemsList)
            {
                if (itemLocalPoolItem.GetType() == typeof(UserDataV1))
                {
                    if (((UserDataV1)itemLocalPoolItem).BaseSignature == userData.BaseSignature)
                    {
                        return false;
                    }
                }
            }

            //Checking existence of chain in staged tx
            foreach (var itemTxId in _storage.IterateStagedTransactionIds().ToImmutableHashSet())
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
                                if (itemAction.GetType() == typeof(UserDataV1))
                                {
                                    if (((UserDataV1)itemAction).BaseSignature == userData.BaseSignature)
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        private bool IsMarketItemValid(IBaseItem actionItem)
        {
            var marketItem = (MarketItemV1)actionItem;
            if (string.IsNullOrEmpty(marketItem.BaseSignature)) return true;

            //Checking existence of chain of identical items in pool
            foreach (var itemLocalPoolItem in _actionItemsList)
            {
                if (itemLocalPoolItem.GetType() == typeof(MarketItemV1))
                {
                    if (((MarketItemV1)itemLocalPoolItem).BaseSignature == marketItem.BaseSignature)
                    {
                        return false;
                    }
                }
            }

            //Checking existence of chain in staged tx
            foreach (var itemTxId in _storage.IterateStagedTransactionIds().ToImmutableHashSet())
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
                                if (itemAction.GetType() == typeof(MarketItemV1))
                                {
                                    if (((MarketItemV1)itemAction).BaseSignature == marketItem.BaseSignature)
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        public bool ExistInStagedTransactions(IBaseItem actionItem)
        {
            foreach (var itemTxId in _storage.IterateStagedTransactionIds().ToImmutableHashSet())
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
                                if (itemAction == actionItem) return true;
                            }
                        }
                    }
                }
            }

            return false;
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

        public int GetTotalCount()
        {
            var totalCount = 0;

            if (_actionItemsList.Any()) totalCount = _actionItemsList.Count();

            var staged = _storage.IterateStagedTransactionIds().ToImmutableHashSet();
            if (staged != null) totalCount += staged.Count();

            return totalCount;
        }

        public List<IBaseItem> GetAllActionItemLocal()
        {
            return _actionItemsList;
        }

        public int GetAllActionItemLocalCount()
        {
            return _actionItemsList.Count();
        }

        public bool DeleteActionItemLocal(string hash)
        {
            var actionItem = _actionItemsList.FirstOrDefault(a => a.Hash == hash);
          
            if (actionItem != null) {
            
                return _actionItemsList.Remove(actionItem);
            } 
            else
            {
                return false;
            }
        }

        public PoolManagerStates.Errors? PropagateAllActionItemLocal(bool forceIt = false)
        {
            var actions = new List<T>();
            var action = new T();

            if (_actionItemsList.Count() > 0)
            {
                if (_swarmServer.Peers.Count() >= _configuration.MinimalPeerAmount)
                {
                    var stagedTxCount = GetAllActionItemStagedCount();

                    if ((stagedTxCount >= _configuration.PoolMaxStagedTxCountInNetwork) && (!forceIt))
                    {
                        return PoolManagerStates.Errors.TooMuchStagedTx;
                    }
                    else
                    {
                        try
                        {
                            var items = _actionItemsList.Take(_configuration.PoolMaxCountOfLocalItemsPropagation).ToList();
                            action.BaseItems.AddRange(items);
                            actions.Add(action);

                            var tx = _blockChain.MakeTransaction(_privateKey, actions);

                            _logger.Information(string.Format("Propagation of new transaction {0}.", tx.Id));

                            _blockChain.StageTransaction(tx);

                            _logger.Information("Clearing all item actions from local pool.");
                            for (int i = 0; i < items.Count(); i++)
                            {
                                _actionItemsList.RemoveAll(a => a.Hash == items[i].Hash);
                            }

                            return null;
                        }
                        catch (Exception e)
                        {
                            _logger.Error("Unexpected error suring propagation of transaction.", e);
                            return PoolManagerStates.Errors.Unexpected;
                        }
                    }
                }
                else
                {
                    return PoolManagerStates.Errors.NoMinimalPeer;
                }
            } 
            else
            {
                return PoolManagerStates.Errors.NoLocalActionItems;
            }
        }

        public List<IBaseItem> GetAllActionItemStaged()
        {
            var result = new List<IBaseItem>();

            foreach (var itemTxId in _storage.IterateStagedTransactionIds().ToImmutableHashSet())
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

        public int GetAllActionItemStagedCount()
        {
            var staged = _storage.IterateStagedTransactionIds().ToImmutableHashSet();
            return staged.Count();
        }

        public IBaseItem GetActionItemStaged(string hash)
        {
            foreach (var itemTxId in _storage.IterateStagedTransactionIds().ToImmutableHashSet())
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

            foreach (var itemTxId in _storage.IterateStagedTransactionIds().ToImmutableHashSet())
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

        public List<IBaseItem> GetAllActionItemByType(Type[] type)
        {
            var result = new List<IBaseItem>();
            var localItems = GetAllActionItemLocal();
            foreach (var itemLocal in localItems)
            {
                if (type.Contains(itemLocal.GetType()))
                {
                    result.Add(itemLocal);
                }
            }

            var stagedItems = GetAllActionItemStaged();
            foreach (var itemStaged in stagedItems)
            {
                if (type.Contains(itemStaged.GetType()))
                {
                    result.Add(itemStaged);
                }
            }

            return result;
        }
    }
}

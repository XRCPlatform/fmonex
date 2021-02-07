using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Extensions;
using Libplanet.Extensions.Helpers;
using Libplanet.Net;
using Libplanet.Store;
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

        public enum PoolStates
        {
            NotStarted = 0,
            Running = 1,
            Stopping = 2,
            Stopped = 3,
            Mining = 4,
            Mined = 5
        }

        private PoolStates _running;

        public bool IsRunning => _running == PoolStates.Running;
        private CancellationTokenSource _cancellationToken { get; set; }

        private List<IBaseItem> _actionItemsList { get; set; }

        private readonly object _pollLock;
        private readonly int SOLD = 1;

        private string _memoryPoolFilePath { get; set; }
        private IBaseConfiguration _configuration { get; }

        private DefaultStore _storage;
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
            DefaultStore storage,
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

            _running = PoolStates.NotStarted;

            LoadActionItemsFromFile();
        }

        public bool Start()
        {
            _running = PoolStates.Running;
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
                    if ((_running == PoolStates.Mining) || (_running == PoolStates.Mined))
                    {
                        if (actionStaged.Count < oldMiningActionStagedCount)
                        {
                            _logger.Information("Stopping Mining Loop Checker.");

                            coMiningRunner.Stop();
                            coMiningRunner.RegisterCoroutine(_miningWorker.GetEnumerator());
                            oldMiningActionStagedCount = 0;

                            _running = PoolStates.Running;
                        }

                        if (!coMiningRunner.IsActive)
                        {
                            if ((miningDelayStart <= DateTime.UtcNow) && (_running != PoolStates.Mined))
                            {
                                _logger.Information(string.Format("Starting mining after mining delay."));
                                coMiningRunner.Start();

                                _running = PoolStates.Mined;
                            }
                            else if (_running == PoolStates.Mined)
                            {
                                _logger.Information("Stopping Mining Loop Checker.");

                                coMiningRunner.Stop();
                                coMiningRunner.RegisterCoroutine(_miningWorker.GetEnumerator());
                                oldMiningActionStagedCount = 0;

                                _running = PoolStates.Running;
                            }
                        }
                    }
                    else
                    {
                        var random = new Random();
                        _logger.Information(string.Format("Found new actions in pools. Staged {0}.", actionStaged.Count));

                        miningDelayStart = DateTime.UtcNow
                                            .Add(_blockPolicy.BlockInterval)
                                            .AddMilliseconds(random.Next(100, 5000));

                        oldMiningActionStagedCount = actionStaged.Count;

                        _running = PoolStates.Mining;
                    }
                }
                else
                {
                    if ((coMiningRunner.IsActive) || (_running == PoolStates.Mining) || (_running == PoolStates.Mined))
                    {
                        _logger.Information("Stopping Mining Loop Checker.");

                        coMiningRunner.Stop();
                        coMiningRunner.RegisterCoroutine(_miningWorker.GetEnumerator());
                        oldMiningActionStagedCount = 0;

                        _running = PoolStates.Running;
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
            if ((_running == PoolStates.Running) || (_running == PoolStates.Mining) || (_running == PoolStates.Mined))
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
            if ((_running == PoolStates.Mining) || (_running == PoolStates.Mined))
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
            _running = PoolStates.Stopping;

            SaveActionItemsToFile();

            _cancellationToken?.Cancel();
            _cancellationToken?.Dispose();
            _cancellationToken = null;

            _logger.Information("Base Pool Manager stopped.");
        }

        public void Dispose()
        {
            _running = PoolStates.Stopped;
            Stop();
        }

        public PoolManagerStates.Errors? AcceptActionItem(IBaseItem actionItem)
        {
            if (_swarmServer.Peers.Count() >= _configuration.MinimalPeerAmount)
            {
                var isValid = CheckActionItemInProcessing(actionItem);
                bool validSignature = ValidateSignature(actionItem);
                if (!validSignature)
                {
                    _logger.Information($"Item {actionItem.Hash} was rejected because of invalid signature.");
                    return PoolManagerStates.Errors.InvalidSignature;
                }

                if (isValid == null) _actionItemsList.Add(actionItem);

                return isValid;
            } 
            else
            {
                _logger.Information($"Item {actionItem.Hash} was rejected because of PoolManagerStates.Errors.NoMinimalPeer.");
                return PoolManagerStates.Errors.NoMinimalPeer;
            }
        }

        private bool ValidateSignature(IBaseItem actionItem)
        {
            if (actionItem.Signature == null)
            {
                return false;
            }
            var buyerPubKeys = UserPublicKey.Recover(actionItem.ToByteArrayForSign(), actionItem.Signature);
            return buyerPubKeys.Any();
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
                    _logger.Information($"Action item {actionItem.Hash} PoolManagerStates.Errors.WrontTypeOfContent.");
                    return PoolManagerStates.Errors.WrontTypeOfContent;
                }

                //Verify existence of equal action item in unstaged tx
                if (ExistInStagedTransactions(actionItem))
                {
                    _logger.Information($"Action item {actionItem.Hash} ExistInStagedTransactions => PoolManagerStates.Errors.Duplication.");
                    return PoolManagerStates.Errors.Duplication;
                }

                //Verification based on type
                if (actionItem.GetType() == typeof(MarketItemV1) && (!IsMarketItemValid(actionItem)))
                {
                    _logger.Information($"Action item {actionItem.Hash} IsMarketItemValid. PoolManagerStates.Errors.StateOfItemIsInProgress");
                    return PoolManagerStates.Errors.StateOfItemIsInProgress;
                }

                //Verification based on type
                if (actionItem.GetType() == typeof(UserDataV1) && (!IsUserDataValid(actionItem)))
                {
                    _logger.Information($"Action item {actionItem.Hash} IsUserDataValid. PoolManagerStates.Errors.StateOfItemIsInProgress");
                    return PoolManagerStates.Errors.StateOfItemIsInProgress;
                }

                //Verification based on type
                if (actionItem.GetType() == typeof(ReviewUserDataV1) && (!IsReviewDataValid(actionItem)))
                {
                    return PoolManagerStates.Errors.StateOfItemIsInProgress;
                }

                return null;
            }
            else
            {
                _logger.Information($"Action item {actionItem.Hash} PoolManagerStates.Errors.NoValidContentHash.");
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

        private bool IsReviewDataValid(IBaseItem actionItem)
        {
            //TODO: a review can be posted by buyer to seller or a seller to buyer on the same market (SOLD transaction basis) 
            

            var reviewData = (ReviewUserDataV1)actionItem;
            var reviewBytes = reviewData.ToByteArrayForSign();

            if (String.IsNullOrEmpty(reviewData.Signature)){
                return false;
            }
            
            var buyerPubKeys = UserPublicKey.Recover(reviewBytes, reviewData.Signature);

            bool marketItemExists = false;
            bool marketItemIsSold = false;
            bool signedByBuyer = false;
            //Checking existence of chain of identical items in pool
            foreach (var itemLocalPoolItem in _actionItemsList)
            {
                if (itemLocalPoolItem.GetType() == typeof(ReviewUserDataV1))
                {
                    if (((ReviewUserDataV1)itemLocalPoolItem).Signature == reviewData.Signature)
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
                                if (itemAction.GetType() == typeof(ReviewUserDataV1))
                                {
                                    if (((ReviewUserDataV1)itemAction).Signature.Equals(reviewData.Signature))
                                    {
                                        return false;
                                    }
                                    //this market item has already been reviewed
                                    if (((ReviewUserDataV1)itemAction).MarketItemHash.Equals(reviewData.MarketItemHash))
                                    {
                                        return false;
                                    }
                                }
                                if (itemAction.GetType() == typeof(MarketItemV1))
                                {
                                    MarketItemV1 marketData = (MarketItemV1) itemAction;
                                    if (marketData.Hash == reviewData.MarketItemHash)
                                    {
                                        marketItemExists = true;
                                        if (marketData.State == SOLD)
                                        {
                                            marketItemIsSold = true;
                                        }
                                        //get buyer pub key and compare to pubkey on review
                                        if (!string.IsNullOrEmpty(marketData.BuyerSignature))
                                        {
                                            var itemMarketBytes = marketData.ToByteArrayForSign();
                                            var itemPubKeys = UserPublicKey.Recover(itemMarketBytes, marketData.BuyerSignature);

                                            foreach (var itemPubKey in itemPubKeys)
                                            {
                                                foreach (var buyerPubKey in buyerPubKeys)
                                                {
                                                    if (itemPubKey.SequenceEqual(buyerPubKey))
                                                    {
                                                        signedByBuyer = true;
                                                        break;
                                                    }
                                                }
                                                if (signedByBuyer)
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Checking for items on chain
            foreach (var itemTxId in _storage.IterateTransactionIds().ToImmutableHashSet())
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
                                if (itemAction.GetType() == typeof(ReviewUserDataV1))
                                {
                                    if (((ReviewUserDataV1)itemAction).Signature.Equals(reviewData.Signature))
                                    {
                                        return false;
                                    }
                                    //this market item has already been reviewed
                                    if (((ReviewUserDataV1)itemAction).MarketItemHash.Equals(reviewData.MarketItemHash))
                                    {
                                        return false;
                                    }
                                }
                                if (itemAction.GetType() == typeof(MarketItemV1))
                                {
                                    MarketItemV1 marketData = (MarketItemV1)itemAction;
                                    if (marketData.Hash == reviewData.MarketItemHash)
                                    {
                                        marketItemExists = true;
                                        if (marketData.State == SOLD)
                                        {
                                            marketItemIsSold = true;
                                        }
                                        //get buyer pub key and compare to pubkey on review
                                        if (!string.IsNullOrEmpty(marketData.BuyerSignature))
                                        {
                                            var itemMarketBytes = marketData.ToByteArrayForSign();
                                            var itemPubKeys = UserPublicKey.Recover(itemMarketBytes, marketData.BuyerSignature);

                                            foreach (var itemPubKey in itemPubKeys)
                                            {
                                                foreach (var buyerPubKey in buyerPubKeys)
                                                {
                                                    if (itemPubKey.SequenceEqual(buyerPubKey))
                                                    {
                                                        signedByBuyer = true;
                                                        break;
                                                    }
                                                }
                                                if (signedByBuyer)
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //TODO: enforce stricter review validation 
            //leaving this here to describe prototype ideas. They don't work at the moment as market chain is in another instance of PoolManager
            //need to decide how to communicate between pool managers perhaps events pubsub between managers?? or IOC allowing to pull both instances? 
            //if any rules that did not return imidiately breached return false
            //if (!marketItemExists || !marketItemIsSold || !signedByBuyer)
            //{
            //    return false;
            //}

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
                            _logger.Error("Unexpected error during propagation of transaction.", e);
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

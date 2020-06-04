using FreeMarketOne.BlockChain.Helpers;
using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.RocksDBStore;
using Libplanet.Tx;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FreeMarketOne.DataStructure;

namespace FreeMarketOne.BlockChain
{
    public class ProofOfWorkWorker<T> : IDisposable where T : IBaseAction, new()
    {
        private ILogger _logger { get; set; }
        private CancellationTokenSource _cancellationToken { get; set; }

        private PrivateKey _privateKey { get; set; }
        private RocksDBStore _storage;
        private BlockChain<T> _blockChain;
        private Swarm<T> _swarmServer;
        private Address _address;
        private EventHandler _eventNewBlock { get; set; }

        public ProofOfWorkWorker(
            ILogger serverLogger,
            Swarm<T> swarmServer,
            BlockChain<T> blockChain,
            Address address,
            RocksDBStore storage,
            PrivateKey privateKey,
            EventHandler eventNewBlock = null)
        {
            _logger = serverLogger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                string.Format("{0}.{1}.{2}", typeof(ProofOfWorkWorker<T>).Namespace, typeof(ProofOfWorkWorker<T>).Name.Replace("`1", string.Empty), typeof(T).Name));

            _blockChain = blockChain;
            _swarmServer = swarmServer;
            _privateKey = privateKey;

            _storage = storage;
            _eventNewBlock = eventNewBlock;
            _address = address;

            _cancellationToken = new CancellationTokenSource();

            _logger.Information("Initializing Proof Of Work Worker");
        }

        public IEnumerator GetEnumerator()
        {
            while (true)
            {
                var txs = new HashSet<Transaction<T>>();

                var taskMiner = Task.Run(async () =>
                {
                    var block = await _blockChain.MineBlock(_address);

                    if (_swarmServer?.Running ?? false)
                    {
                        _swarmServer.BroadcastBlock(block);
                    }

                    return block;
                });

                yield return new WaitUntil(() => taskMiner.IsCompleted);

                if (!taskMiner.IsCanceled && !taskMiner.IsFaulted)
                {
                    var block = taskMiner.Result;
                    _logger.Information(string.Format("Created block index: {0}, difficulty: {1}",
                        block.Index,
                        block.Difficulty));
                }
                else
                {
                    var invalidTxs = txs;
                    var retryActions = new HashSet<IImmutableList<T>>();

                    if (taskMiner.IsFaulted)
                    {
                        foreach (var ex in taskMiner.Exception.InnerExceptions)
                        {
                            if (ex is InvalidTxNonceException invalidTxNonceException)
                            {
                                var invalidNonceTx = _storage.GetTransaction<T>(invalidTxNonceException.TxId);

                                if (invalidNonceTx.Signer == _address)
                                {
                                    _logger.Error(string.Format("Tx[{0}] nonce is invalid. Retry it.",
                                        invalidTxNonceException.TxId));
                                    retryActions.Add(invalidNonceTx.Actions);
                                }
                            }

                            if (ex is InvalidTxException invalidTxException)
                            {
                                _logger.Error(string.Format("Tx[{0}] is invalid. mark to unstage.",
                                    invalidTxException.TxId));
                                invalidTxs.Add(_storage.GetTransaction<T>(invalidTxException.TxId));
                            }

                            _logger.Error(ex.Message);
                        }
                    }

                    foreach (var invalidTx in invalidTxs)
                    {
                        _blockChain.UnstageTransaction(invalidTx);
                    }

                    foreach (var retryAction in retryActions)
                    {
                        var actions = retryAction.ToArray();
                        _blockChain.MakeTransaction(_privateKey, actions);
                    }
                }

                _eventNewBlock?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            _logger.Information("Proof Of Work Worker stopping.");

            _cancellationToken.Cancel();

            _logger.Information("Proof Of Work Worker stopped.");
        }
    }
}

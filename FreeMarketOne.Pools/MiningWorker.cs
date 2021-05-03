using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Net;
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
using Libplanet.Extensions.Helpers;
using Libplanet.Store;

namespace FreeMarketOne.Pools
{
    public class MiningWorker<T> : IDisposable where T : IBaseAction, new()
    {
        private ILogger _logger { get; set; }
        private CancellationTokenSource _cancellationToken { get; set; }

        private PrivateKey _privateKey { get; set; }
        private BlockChain<T> _blockChain;
        private Swarm<T> _swarmServer;
        private Address _address;
        private EventHandler _eventNewBlock { get; set; }

        public MiningWorker(
            ILogger serverLogger,
            Swarm<T> swarmServer,
            BlockChain<T> blockChain,
            Address address,
            PrivateKey privateKey,
            CancellationTokenSource cancellationTokenSource,
            EventHandler eventNewBlock = null)
        {
            _logger = serverLogger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                string.Format("{0}.{1}.{2}", typeof(MiningWorker<T>).Namespace, typeof(MiningWorker<T>).Name.Replace("`1", string.Empty), typeof(T).Name));

            _blockChain = blockChain;
            _swarmServer = swarmServer;
            _privateKey = privateKey;
            _eventNewBlock = eventNewBlock;
            _address = address;

            _cancellationToken = cancellationTokenSource;

            _logger.Information("Initializing Mining Worker");
        }

        public IEnumerator GetEnumerator()
        {
            //while (true)
            //{
            var txs = new HashSet<Transaction<T>>();

            var taskMiner = Task.Run(async () =>
            {
                var block = await _blockChain.MineBlock(_address, DateTimeOffset.UtcNow,
                    cancellationToken: _cancellationToken.Token);

                if (_swarmServer?.Running ?? false)
                {
                        //broadcast even if tip is unchanged
                        _swarmServer.BroadcastBlock(block);
                }

                return block;
            });

            try
            {
                taskMiner.Wait();
                if (!taskMiner.IsCanceled && !taskMiner.IsFaulted)
                {
                    var block = taskMiner.Result;
                    _logger.Information(string.Format("Created block index: {0}, difficulty: {1}",
                        block.Index,
                        block.Difficulty));
                }
            }
            catch (AggregateException ae)
            {
                var invalidTxs = txs;
                var retryActions = new HashSet<IImmutableList<T>>();
                foreach (var ex in ae.Flatten().InnerExceptions)
                {
                    if (ex is InvalidTxNonceException invalidTxNonceException)
                    {
                        var invalidNonceTx = _blockChain.GetTransaction(invalidTxNonceException.TxId);

                        //unpoison
                        _blockChain.UnstageTransaction(invalidNonceTx);

                        //if created by my peer, will build new tx
                        if (invalidNonceTx.Signer == _address)
                        {
                            _logger.Error(string.Format("Tx[{0}] nonce is invalid. Retry it.",
                                invalidTxNonceException.TxId));
                            retryActions.Add(invalidNonceTx.Actions);
                        }
                    }
                    else if (ex is InvalidTxException invalidTxException)
                    {
                        _logger.Error(string.Format("Tx[{0}] is invalid. mark to unstage.",
                            invalidTxException.TxId));
                        invalidTxs.Add(_blockChain.GetTransaction(invalidTxException.TxId));
                    }
                    else
                    {
                        throw;
                    }

                    _logger.Error(ex.Message);
                }

                foreach (var invalidTx in invalidTxs)
                {
                    _blockChain.UnstageTransaction(invalidTx);
                }

                foreach (var retryAction in retryActions)
                {
                    //should we unstage original problem transaction here?
                    var actions = retryAction.ToArray();
                    _blockChain.MakeTransaction(_privateKey, actions);

                }
            }

            yield return new WaitUntil(() => taskMiner.IsCompleted);

            _eventNewBlock?.Invoke(this, EventArgs.Empty);
            //}
        }

        public void Dispose()
        {
            _logger.Information("Mining Worker stopping.");

            _cancellationToken.Cancel();

            _logger.Information("Mining Worker stopped.");
        }
    }
}

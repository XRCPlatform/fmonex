using FreeMarketOne.Extensions.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeMarketOne.BlockChain
{
    internal class ProofOfWorkWorker<T> : IDisposable where T : IBaseAction, new()
    {
        private ILogger logger { get; set; }
        private IAsyncLoopFactory asyncLoopFactory { get; set; }
        private CancellationTokenSource cancellationToken { get; set; }

        internal ProofOfWorkWorker(
            ILogger serverLogger,
            EventHandler eventNewBlock)
        {
            serverLogger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, typeof(T).FullName);

            logger.Information("Initializing Proof Of Work Worker");

            cancellationToken = new CancellationTokenSource();
            asyncLoopFactory = new AsyncLoopFactory(serverLogger);

            var periodicLogLoop = this.asyncLoopFactory.Run("ProofOfWork" + typeof(T).Name, (cancellation) =>
            {
                while (true)
                {
                    var txs = new HashSet<Transaction<PolymorphicAction<ActionBase>>>();

                    var task = Task.Run(async () =>
                    {
                        var block = await _blocks.MineBlock(Address);

                        if (_swarm?.Running ?? false)
                        {
                            _swarm.BroadcastBlock(block);
                        }

                        return block;
                    });
                    yield return new WaitUntil(() => task.IsCompleted);

                    if (!task.IsCanceled && !task.IsFaulted)
                    {
                        var block = task.Result;
                        Debug.Log($"created block index: {block.Index}, difficulty: {block.Difficulty}");
                    }
                    else
                    {
                        var invalidTxs = txs;
                        var retryActions = new HashSet<IImmutableList<PolymorphicAction<ActionBase>>>();

                        if (task.IsFaulted)
                        {
                            foreach (var ex in task.Exception.InnerExceptions)
                            {
                                if (ex is InvalidTxNonceException invalidTxNonceException)
                                {
                                    var invalidNonceTx = _store.GetTransaction<PolymorphicAction<ActionBase>>(invalidTxNonceException.TxId);

                                    if (invalidNonceTx.Signer == Address)
                                    {
                                        Debug.Log($"Tx[{invalidTxNonceException.TxId}] nonce is invalid. Retry it.");
                                        retryActions.Add(invalidNonceTx.Actions);
                                    }
                                }

                                if (ex is InvalidTxException invalidTxException)
                                {
                                    Debug.Log($"Tx[{invalidTxException.TxId}] is invalid. mark to unstage.");
                                    invalidTxs.Add(_store.GetTransaction<PolymorphicAction<ActionBase>>(invalidTxException.TxId));
                                }

                                Debug.LogException(ex);
                            }
                        }

                        foreach (var invalidTx in invalidTxs)
                        {
                            _blocks.UnstageTransaction(invalidTx);
                        }

                        foreach (var retryAction in retryActions)
                        {
                            MakeTransaction(retryAction, true);
                        }
                    }
                }

                eventNewBlock.Invoke(this, EventArgs.Empty);

                return Task.CompletedTask;
            },
                cancellationToken.Token,
                repeatEvery: blockTime,
                startAfter: GetDelayTimeSpan());
        }

        internal void Dispose()
        {
            logger.Information("Proof Of Work Worker stopping.");

            cancellationToken.Cancel();

            logger.Information("Proof Of Work Worker stopped.");
        }
    }
}

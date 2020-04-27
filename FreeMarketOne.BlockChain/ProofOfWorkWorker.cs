using FreeMarketOne.Extensions.Helpers;
using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.RocksDBStore;
using Libplanet.Tx;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            Swarm<T> swarm,
            BlockChain<T> blocks,
            Address address,
            RocksDBStore store,
            PrivateKey privateKey,
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
                    var txs = new HashSet<Transaction<T>>();

                    var taskMiner = Task.Run(async () =>
                    {
                        var block = await blocks.MineBlock(address);

                        if (swarm?.Running ?? false)
                        {
                            swarm.BroadcastBlock(block);
                        }

                        return block;
                    });
                    taskMiner.Wait();

                    if (!taskMiner.IsCanceled && !taskMiner.IsFaulted)
                    {
                        var block = taskMiner.Result;
                        logger.Information(string.Format("Created block index: {0}, difficulty: {1}",
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
                                    var invalidNonceTx = store.GetTransaction<T>(invalidTxNonceException.TxId);

                                    if (invalidNonceTx.Signer == address)
                                    {
                                        logger.Error(string.Format("Tx[{0}] nonce is invalid. Retry it.",
                                            invalidTxNonceException.TxId));
                                        retryActions.Add(invalidNonceTx.Actions);
                                    }
                                }

                                if (ex is InvalidTxException invalidTxException)
                                {
                                    logger.Error(string.Format("Tx[{0}] is invalid. mark to unstage.",
                                        invalidTxException.TxId));
                                    invalidTxs.Add(store.GetTransaction<T>(invalidTxException.TxId));
                                }

                                logger.Error(ex.Message);
                            }
                        }

                        foreach (var invalidTx in invalidTxs)
                        {
                            blocks.UnstageTransaction(invalidTx);
                        }

                        foreach (var retryAction in retryActions)
                        {
                            var actions = retryAction.ToArray();
                            blocks.MakeTransaction(privateKey, actions);
                        }
                    }

                    eventNewBlock.Invoke(this, EventArgs.Empty);
                }
            },
                cancellationToken.Token,
                repeatEvery: TimeSpans.RunOnce);
        }

        public void Dispose()
        {
            logger.Information("Proof Of Work Worker stopping.");

            cancellationToken.Cancel();

            logger.Information("Proof Of Work Worker stopped.");
        }
    }
}

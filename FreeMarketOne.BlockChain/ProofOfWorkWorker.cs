using FreeMarketOne.BlockChain.Helpers;
using FreeMarketOne.Extensions.Helpers;
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

namespace FreeMarketOne.BlockChain
{
    internal class ProofOfWorkWorker<T> : IDisposable where T : IBaseAction, new()
    {
        private ILogger logger { get; set; }
        private CancellationTokenSource cancellationToken { get; set; }

        private PrivateKey privateKey { get; set; }
        private RocksDBStore store;
        private BlockChain<T> blocks;
        private Swarm<T> swarm;
        private Address address;
        private EventHandler eventNewBlock { get; set; }

        internal ProofOfWorkWorker(
            ILogger serverLogger,
            Swarm<T> swarm,
            BlockChain<T> blocks,
            Address address,
            RocksDBStore store,
            PrivateKey privateKey,
            EventHandler eventNewBlock = null)
        {
            this.logger = serverLogger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, typeof(T).FullName);

            this.blocks = blocks;
            this.swarm = swarm;
            this.privateKey = privateKey;

            this.store = store;
            this.eventNewBlock = eventNewBlock;
            this.address = address;

            this.cancellationToken = new CancellationTokenSource();

            this.logger.Information("Initializing Proof Of Work Worker");
        }

        internal IEnumerator GetEnumerator()
        {
            while (true)
            {
                var txs = new HashSet<Transaction<T>>();

                var taskMiner = Task.Run(async () =>
                {
                    var block = await this.blocks.MineBlock(address);

                    if (this.swarm?.Running ?? false)
                    {
                        this.swarm.BroadcastBlock(block);
                    }

                    return block;
                });

                yield return new WaitUntil(() => taskMiner.IsCompleted);

                if (!taskMiner.IsCanceled && !taskMiner.IsFaulted)
                {
                    var block = taskMiner.Result;
                    this.logger.Information(string.Format("Created block index: {0}, difficulty: {1}",
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
                                var invalidNonceTx = this.store.GetTransaction<T>(invalidTxNonceException.TxId);

                                if (invalidNonceTx.Signer == address)
                                {
                                    this.logger.Error(string.Format("Tx[{0}] nonce is invalid. Retry it.",
                                        invalidTxNonceException.TxId));
                                    retryActions.Add(invalidNonceTx.Actions);
                                }
                            }

                            if (ex is InvalidTxException invalidTxException)
                            {
                                this.logger.Error(string.Format("Tx[{0}] is invalid. mark to unstage.",
                                    invalidTxException.TxId));
                                invalidTxs.Add(store.GetTransaction<T>(invalidTxException.TxId));
                            }

                            this.logger.Error(ex.Message);
                        }
                    }

                    foreach (var invalidTx in invalidTxs)
                    {
                        this.blocks.UnstageTransaction(invalidTx);
                    }

                    foreach (var retryAction in retryActions)
                    {
                        var actions = retryAction.ToArray();
                        this.blocks.MakeTransaction(this.privateKey, actions);
                    }
                }

                this.eventNewBlock?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            logger.Information("Proof Of Work Worker stopping.");

            this.cancellationToken.Cancel();

            logger.Information("Proof Of Work Worker stopped.");
        }
    }
}

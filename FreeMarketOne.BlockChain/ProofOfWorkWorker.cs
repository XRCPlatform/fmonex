using FreeMarketOne.Extensions.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
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
                logger.Information(string.Format("Proof Of Time Worker New block has been found {0}.", this.lastNewBlockTimeUtc.Value.ToLongTimeString()));

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

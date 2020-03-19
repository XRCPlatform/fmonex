using FreeMarketOne.Extensions.Helpers;
using Serilog;
using Serilog.Core;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeMarketOne.Mining
{
    internal class ProofOfTimeWorker : IDisposable
    {
        private ILogger logger { get; set; }

        private DateTime genesisTimeUtc;
        private DateTime networkTimeUtc;
        private DateTime? lastNewBlockTimeUtc;
        private TimeSpan blockTime;
        private Stopwatch swatch = new Stopwatch();
        private IAsyncLoopFactory asyncLoopFactory { get; set; }

        private CancellationTokenSource cancellationToken { get; set; }

        internal ProofOfTimeWorker(ILogger serverLogger, DateTime genesisTimeUtc, 
            DateTime networkTimeUtc, TimeSpan blockTime, EventHandler eventNewBlock)
        {
            this.logger = serverLogger.ForContext<ProofOfTimeWorker>();
            this.genesisTimeUtc = genesisTimeUtc;
            this.networkTimeUtc = networkTimeUtc;
            this.blockTime = blockTime;

            logger.Information("Initializing Proof Of Time Worker");

            swatch.Reset();
            swatch.Start();

            cancellationToken = new CancellationTokenSource();
            asyncLoopFactory = new AsyncLoopFactory(serverLogger);

            var periodicLogLoop = this.asyncLoopFactory.Run("ProofOfTime", (cancellation) =>
            {
                this.lastNewBlockTimeUtc = UtcNow;

                logger.Information(string.Format("Proof Of Time Worker New block has been found {0}.", this.lastNewBlockTimeUtc.Value.ToLongTimeString()));

                eventNewBlock.Invoke(this, EventArgs.Empty);

                return Task.CompletedTask;
            },
                cancellationToken.Token,
                repeatEvery: blockTime,
                startAfter: GetDelayTimeSpan());
        }

        internal DateTime? UtcNow
        {
            get
            {
                if (this.networkTimeUtc != null)
                {
                    return this.networkTimeUtc.Add(this.swatch.Elapsed);
                } 
                else
                {
                    return null;
                }
            }
        }

        private TimeSpan GetDelayTimeSpan()
        {
            var timeDifference = this.networkTimeUtc.Subtract(this.genesisTimeUtc);
            var delayTimeSpan = TimeSpan.FromTicks(timeDifference.Ticks % this.blockTime.Ticks);

            logger.Information(string.Format("Proof Of Time Worker New block will be found in {0} ticks.",  delayTimeSpan.Ticks));

            return delayTimeSpan;
        } 


        public void Dispose()
        {
            logger.Information("Proof Of Time Worker stopping.");

            cancellationToken.Cancel();

            logger.Information("Proof Of Time Worker stopped.");
        }
    }
}

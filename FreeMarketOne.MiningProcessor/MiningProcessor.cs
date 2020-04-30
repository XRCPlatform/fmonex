using System;
using System.Threading;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.PoolManager;
using Serilog;

namespace FreeMarketOne.Mining
{
    public class MiningProcessor : IMiningProcessor, IDisposable
    {
        private ILogger logger { get; set; }

        private DateTime genesisTimeUtc;
        private DateTime networkTimeUtc;

        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped
        /// </summary>
        private long running;

        public bool IsRunning => Interlocked.Read(ref running) == 1;

        private CancellationTokenSource cancellationToken { get; set; }

        private ProofOfTimeWorker potWorker { get; set; }

        /// <param name="serverLogger">Base server logger.</param>
        /// <param name="configuration">Base configuration.</param>
        public MiningProcessor(
            ILogger serverLogger, 
            IPoolManager basePoolManager,
            IPoolManager marketPoolManager,
            DateTime genesisDateTimeUtc, 
            DateTime networkDateTimeUtc)
        {
            this.logger = serverLogger.ForContext<MiningProcessor>();
            this.genesisTimeUtc = genesisDateTimeUtc;
            this.networkTimeUtc = networkDateTimeUtc;

            logger.Information("Initializing Mining Processor");

            Interlocked.Exchange(ref running, 0);
            cancellationToken = new CancellationTokenSource();
         }

        internal static void EventNewBlockReached(object sender, EventArgs e)
        {
            Console.WriteLine("XXXX.");
        }

        public bool Start()
        {
            Interlocked.Exchange(ref running, 1);

            potWorker = new ProofOfTimeWorker(this.logger, this.genesisTimeUtc, this.networkTimeUtc, TimeSpans.Minute, EventNewBlockReached);

            return true;
        }

        public bool IsMiningRunning()
        {
            if (Interlocked.Read(ref running) == 1)
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
            Interlocked.Exchange(ref running, 2);

            potWorker?.Dispose();
            potWorker = null;

            cancellationToken?.Cancel();
            cancellationToken?.Dispose();
            cancellationToken = null;

            logger.Information("Mining Processor stopped.");
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref running, 3);
            Stop();
        }
    }
}

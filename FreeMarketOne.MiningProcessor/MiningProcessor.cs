using System;
using System.Threading;
using System.Threading.Tasks;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Extensions.Models;
using Serilog;
using Serilog.Core;

namespace FreeMarketOne.Mining
{
    public class MiningProcessor : IDisposable
    {
        private ILogger logger { get; set; }

        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped
        /// </summary>
        private long running;

        public bool IsRunning => Interlocked.Read(ref running) == 1;

        private CancellationTokenSource cancellationToken { get; set; }

        private ProofOfTimeWorker potWorker { get; set; }

        /// <param name="serverLogger">Base server logger.</param>
        /// <param name="configuration">Base configuration.</param>
        public MiningProcessor(Logger serverLogger, BaseConfiguration configuration, DateTime genesisDateTimeUtc, DateTime networkDateTimeUtc)
        {
            logger = serverLogger.ForContext<MiningProcessor>();
            logger.Information("Initializing Mining Processor");

            Interlocked.Exchange(ref running, 0);
            cancellationToken = new CancellationTokenSource();

            potWorker = new ProofOfTimeWorker(serverLogger, genesisDateTimeUtc, networkDateTimeUtc, TimeSpans.FiveMinutes, EventNewBlockReached);
        }

        internal static void EventNewBlockReached(object sender, EventArgs e)
        {
            Console.WriteLine("XXXX.");
        }

        public bool Start()
        {
            Interlocked.Exchange(ref running, 1);

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

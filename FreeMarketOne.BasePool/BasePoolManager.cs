using FreeMarketOne.Extensions.Models;
using Serilog;
using System;
using System.Threading;

namespace FreeMarketOne.BasePool
{
    public class BasePoolManager : IBasePoolManager, IDisposable
    {
        private ILogger logger { get; set; }

        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped
        /// </summary>
        private long running;

        public bool IsRunning => Interlocked.Read(ref running) == 1;

        private CancellationTokenSource cancellationToken { get; set; }

        /// <param name="serverLogger">Base server logger.</param>
        /// <param name="configuration">Base configuration.</param>
        public BasePoolManager(ILogger serverLogger, BaseConfiguration configuration)
        {
            this.logger = serverLogger.ForContext<BasePoolManager>();

            logger.Information("Initializing Base Pool Manager");

            Interlocked.Exchange(ref running, 0);
            cancellationToken = new CancellationTokenSource();
        }

        public bool Start()
        {
            Interlocked.Exchange(ref running, 1);

            return true;
        }

        public bool IsBasePoolManagerRunning()
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

            cancellationToken?.Cancel();
            cancellationToken?.Dispose();
            cancellationToken = null;

            logger.Information("Base Pool Manager stopped.");
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref running, 3);
            Stop();
        }
    }
}

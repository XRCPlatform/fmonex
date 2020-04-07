using FreeMarketOne.DataStructure;
using Serilog;
using System;
using System.Threading;

namespace FreeMarketOne.MarketPool
{
    public class MarketPoolManager : IMarketPoolManager, IDisposable
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

        /// <param name="serverLogger">Base server logger.</param>
        /// <param name="configuration">Base configuration.</param>
        public MarketPoolManager(ILogger serverLogger, IBaseConfiguration configuration)
        {
            this.logger = serverLogger.ForContext<MarketPoolManager>();

            logger.Information("Initializing Market Pool Manager");

            Interlocked.Exchange(ref running, 0);
            cancellationToken = new CancellationTokenSource();
        }

        public bool Start()
        {
            Interlocked.Exchange(ref running, 1);

            return true;
        }

        public bool IsMarketPoolManagerRunning()
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

            logger.Information("Market Pool Manager stopped.");
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref running, 3);
            Stop();
        }

        public bool AcceptTx()
        {
            throw new NotImplementedException();
        }

        public bool SaveTx()
        {
            throw new NotImplementedException();
        }

        public bool LoadTx()
        {
            throw new NotImplementedException();
        }

        //accept
        //isvalid
        //save
        //load
        //getforBlock
        //processblock

    }
}

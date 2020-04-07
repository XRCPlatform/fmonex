using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
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

        private List<IBaseItem> baseMemoryTxList { get; set; }

        private readonly object basePollLock;

        /// <param name="serverLogger">Base server logger.</param>
        /// <param name="configuration">Base configuration.</param>
        public BasePoolManager(ILogger serverLogger, IBaseConfiguration configuration)
        {
            this.logger = serverLogger.ForContext<BasePoolManager>();
            this.baseMemoryTxList = new List<IBaseItem>(); 
            this.basePollLock = new object();

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

        public bool AcceptTx(IBaseItem tx)
        {
            if (CheckTxInProcessing(tx))
            {

            } 
            else
            {

            }

            throw new NotImplementedException();
        }

        public bool SaveTx()
        {
            lock (basePollLock)
            {
                var serializedMemory = JsonConvert.SerializeObject(baseMemoryTxList);

                var fullBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);


                if (File.Exists(Path.Combine(rootFolder, authorsFile)))
                {
                    // If file found, delete it    
                    File.Delete(Path.Combine(rootFolder, authorsFile));
                    Console.WriteLine("File deleted.");
                }
            }


            throw new NotImplementedException();
        }

        public bool LoadTx()
        {
            throw new NotImplementedException();
        }

        public bool CheckTxInProcessing(IBaseItem tx)
        {
            throw new NotImplementedException();
        }

        public bool AcceptTx()
        {
            throw new NotImplementedException();
        }
    }
}

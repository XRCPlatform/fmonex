using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.MarketItems;
using FreeMarketOne.Extensions.Helpers;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

        private List<IMarketItem> marketMemoryTxList { get; set; }

        private readonly object marketPollLock;
        private string marketPoolFilePath { get; set; }

        /// <param name="serverLogger">Base server logger.</param>
        /// <param name="configuration">Base configuration.</param>
        public MarketPoolManager(ILogger serverLogger, IBaseConfiguration configuration)
        {
            this.logger = serverLogger.ForContext<MarketPoolManager>();
            this.marketMemoryTxList = new List<IMarketItem>();
            this.marketPollLock = new object();
            this.marketPoolFilePath = configuration.MemoryMarketPoolPath;

            logger.Information("Initializing Market Pool Manager");

            Interlocked.Exchange(ref running, 0);
            cancellationToken = new CancellationTokenSource();

            LoadTxsFromFile();
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

            SaveTxsToFile();

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

        public bool AcceptTx(IMarketItem tx)
        {
            if (CheckTxInProcessing(tx))
            {
                this.marketMemoryTxList.Add(tx);

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SaveTxsToFile()
        {
            lock (marketPollLock)
            {
                logger.Information("Saving tx data.");

                var serializedMemory = JsonConvert.SerializeObject(this.marketMemoryTxList);
                var compressedMemory = ZipHelpers.Compress(serializedMemory);
                var fullBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (!fullBaseDirectory.StartsWith('/'))
                    {
                        fullBaseDirectory.Insert(0, "/");
                    }
                }

                var targetFilePath = Path.Combine(fullBaseDirectory, this.marketPoolFilePath);
                var targetDirectory = Path.GetDirectoryName(targetFilePath);

                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                File.WriteAllBytes(targetFilePath, compressedMemory);

                logger.Information("Tx data saved.");
            }

            return true;
        }

        public bool LoadTxsFromFile()
        {
            lock (marketPollLock)
            {
                logger.Information("Loading tx data.");

                var fullBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);

                var targetFilePath = Path.Combine(fullBaseDirectory, this.marketPoolFilePath);
                var targetDirectory = Path.GetDirectoryName(targetFilePath);

                if (File.Exists(targetFilePath))
                {
                    var compressedMemory = File.ReadAllBytes(targetFilePath);
                    var serializedMemory = ZipHelpers.Decompress(compressedMemory);

                    var temporaryMemoryTxList = JsonConvert.DeserializeObject<List<IMarketItem>>(serializedMemory);

                    //check all loaded tx in list
                    foreach (var itemTx in temporaryMemoryTxList)
                    {
                        if (CheckTxInProcessing(itemTx))
                        {
                            this.marketMemoryTxList.Add(itemTx);
                        }
                    }
                }

                logger.Information("Tx data loaded.");
            }

            return true;
        }

        public bool CheckTxInProcessing(IMarketItem tx)
        {
            if (tx.IsValid() && !this.marketMemoryTxList.Exists(mt => mt == tx))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ClearTxBasedOnHashes(List<string> hashsToRemove)
        {
            foreach (var itemHash in hashsToRemove)
            {
                var txToRemove = this.marketMemoryTxList.FirstOrDefault(a => a.Hash == itemHash);

                if (txToRemove != null) this.marketMemoryTxList.Remove(txToRemove);
            }

            return true;
        }
    }
}

using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
        private string basePoolFilePath { get; set; }

        /// <param name="serverLogger">Base server logger.</param>
        /// <param name="configuration">Base configuration.</param>
        public BasePoolManager(ILogger serverLogger, IBaseConfiguration configuration)
        {
            this.logger = serverLogger.ForContext<BasePoolManager>();
            this.baseMemoryTxList = new List<IBaseItem>(); 
            this.basePollLock = new object();
            this.basePoolFilePath = configuration.MemoryBasePoolPath;

            logger.Information("Initializing Base Pool Manager");

            Interlocked.Exchange(ref running, 0);
            cancellationToken = new CancellationTokenSource();

            LoadTxsFromFile();
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

            SaveTxsToFile();

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
                this.baseMemoryTxList.Add(tx);

                return true;
            } 
            else
            {
                return false;
            }
        }

        public bool SaveTxsToFile()
        {
            lock (basePollLock)
            {
                logger.Information("Saving tx data.");

                var serializedMemory = JsonConvert.SerializeObject(this.baseMemoryTxList);
                var compressedMemory = ZipHelpers.Compress(serializedMemory);
                var fullBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (!fullBaseDirectory.StartsWith('/'))
                    {
                        fullBaseDirectory.Insert(0, "/");
                    }
                }

                var targetFilePath = Path.Combine(fullBaseDirectory, this.basePoolFilePath);
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
            lock (basePollLock)
            {
                logger.Information("Loading tx data.");
                
                var fullBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);

                var targetFilePath = Path.Combine(fullBaseDirectory, this.basePoolFilePath);
                var targetDirectory = Path.GetDirectoryName(targetFilePath);

                if (File.Exists(targetFilePath))
                {
                    var compressedMemory = File.ReadAllBytes(targetFilePath);
                    var serializedMemory = ZipHelpers.Decompress(compressedMemory);
                  
                    var temporaryMemoryTxList = JsonConvert.DeserializeObject<List<IBaseItem>>(serializedMemory);

                    //check all loaded tx in list
                    foreach (var itemTx in temporaryMemoryTxList)
                    {
                        if (CheckTxInProcessing(itemTx))
                        {
                            this.baseMemoryTxList.Add(itemTx);
                        }
                    }
                }

                logger.Information("Tx data loaded.");
            }

            return true;
        }

        public bool CheckTxInProcessing(IBaseItem tx)
        {
            if (tx.IsValid() && !this.baseMemoryTxList.Exists(mt => mt == tx))
            {
                return true;
            } else
            {
                return false;
            }
        }

        public bool ClearTxBasedOnHashes(List<string> hashsToRemove)
        {
            foreach (var itemHash in hashsToRemove)
            {
                var txToRemove = this.baseMemoryTxList.FirstOrDefault(a => a.Hash == itemHash);

                if (txToRemove != null) this.baseMemoryTxList.Remove(txToRemove);
            }

            return true;
        }
    }
}

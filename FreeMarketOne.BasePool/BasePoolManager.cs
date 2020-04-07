using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
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
        private string memoryPoolFilePath { get; set; }


        /// <param name="serverLogger">Base server logger.</param>
        /// <param name="configuration">Base configuration.</param>
        public BasePoolManager(ILogger serverLogger, IBaseConfiguration configuration)
        {
            this.logger = serverLogger.ForContext<BasePoolManager>();
            this.baseMemoryTxList = new List<IBaseItem>(); 
            this.basePollLock = new object();
            this.memoryPoolFilePath = configuration.MemoryBasePoolPath;

            var ss = new CheckPointMarketDataV1();
            this.baseMemoryTxList.Add(ss);

            logger.Information("Initializing Base Pool Manager");

            Interlocked.Exchange(ref running, 0);
            cancellationToken = new CancellationTokenSource();

            LoadTx();
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

            SaveTx();

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
                logger.Information("Saving base memory tx data.");

                var jsonSettings = new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.All
                };

                var serializedMemory = JsonConvert.SerializeObject(this.baseMemoryTxList, Formatting.None, jsonSettings);
                var compressedMemory = ZipHelpers.Compress(serializedMemory);
                var fullBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (!fullBaseDirectory.StartsWith('/'))
                    {
                        fullBaseDirectory.Insert(0, "/");
                    }
                }

                var targetFilePath = Path.Combine(fullBaseDirectory, this.memoryPoolFilePath);
                var targetDirectory = Path.GetDirectoryName(targetFilePath);

                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                File.WriteAllBytes(targetFilePath, compressedMemory);

                logger.Information("Base memory tx data saved.");
            }

            return true;
        }

        public bool LoadTx()
        {
            lock (basePollLock)
            {
                logger.Information("Loading base memory tx data.");
                
                var fullBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);

                var targetFilePath = Path.Combine(fullBaseDirectory, this.memoryPoolFilePath);
                var targetDirectory = Path.GetDirectoryName(targetFilePath);

                if (File.Exists(targetFilePath))
                {
                    var compressedMemory = File.ReadAllBytes(targetFilePath);
                    var serializedMemory = ZipHelpers.Decompress(compressedMemory);
                  
                    var x = JsonBaseConverter.DeserializeObject(serializedMemory);
                }

                logger.Information("Base memory tx data loaded.");
            }

            return true;
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

    public class JsonBaseConverter : JsonConverter
    {
        public override bool CanWrite => false;
        public override bool CanRead => true;
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IBaseItem);
        }
        public override void WriteJson(JsonWriter writer,
            object value, JsonSerializer serializer)
        {
            throw new InvalidOperationException("Use default serialization.");
        }

        public override object ReadJson(JsonReader reader,
            Type objectType, object existingValue,
            JsonSerializer serializer)
        {
           
            var jsonObject = JObject.Load(reader);
            var profession = default(IBaseItem);
            //switch (jsonObject["JobTitle"].Value())
            //{
            //    case "Software Developer":
            //        profession = new Programming();
            //        break;
            //    case "Copywriter":
            //        profession = new Writing();
            //        break;
            //}
            serializer.Populate(jsonObject.CreateReader(), profession);
            return profession;
        }
    }
}

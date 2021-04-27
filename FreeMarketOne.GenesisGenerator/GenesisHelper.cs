using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Extensions;
using Org.BouncyCastle.Crypto.Generators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FreeMarketOne.GenesisBlock
{
    public class GenesisHelper
    {
        private const string _privateBaseKey = "7dabd5472929a0c388c7d1af6e9e53848d89cb7ad844ed9d8e587aeefd749a5b";
        private const string _privateMarketKey = "24cd3da85fab65992b5d3fc313d5f6fd35879fc13d6bbf36c0d008978fd85c0b";
        private const long _chainUnixTimeMiliseconds = 1356088341000;

        public Block<BaseAction> GenerateIt(IBaseConfiguration configuration)
        {
            //get fmone keys
            var privateBaseBytesKey = ByteUtil.ParseHex(_privateBaseKey);
            var privateBaseKey = new PrivateKey(privateBaseBytesKey);
            var privateMarketBytesKey = ByteUtil.ParseHex(_privateMarketKey);
            var privateMarketKey = new PrivateKey(privateMarketBytesKey);
            DateTimeOffset _chainDateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(_chainUnixTimeMiliseconds);
       
            //make plain market chain block
            var filePath = Path.Combine(configuration.FullBaseDirectory, configuration.BlockChainMarketPath, "genesis.dat");
            if (!File.Exists(filePath))
            {
                Block<MarketAction> genesisMarket =
                    BlockChain<MarketAction>.MakeGenesisBlock(null, privateMarketKey, _chainDateTimeOffset);

                Directory.CreateDirectory(Path.Combine(configuration.FullBaseDirectory, configuration.BlockChainMarketPath));
                File.WriteAllBytes(filePath, genesisMarket.Serialize());

                //generate plain base chain block
                List<BaseAction> actionsGenesis = new List<BaseAction>();
                var newAction = new BaseAction();
                var checkPoint = new CheckPointMarketDataV1();

                checkPoint.Block = ByteUtil.Hex(genesisMarket.Serialize());
                checkPoint.CreatedUtc = new DateTime(2020, 7, 1, 12, 12, 21);
                checkPoint.Hash = checkPoint.GenerateHash();

                newAction.AddBaseItem(checkPoint);
                actionsGenesis.Add(newAction);

                Block<BaseAction> genesis =
                    BlockChain<BaseAction>.MakeGenesisBlock(actionsGenesis, privateBaseKey, _chainDateTimeOffset);

                filePath = Path.Combine(configuration.FullBaseDirectory, configuration.BlockChainBasePath, "genesis.dat");
                Directory.CreateDirectory(Path.Combine(configuration.FullBaseDirectory, configuration.BlockChainBasePath));
               
                File.WriteAllBytes(filePath, genesis.Serialize());

                return genesis;
            }

            return null;
        }

        /// <summary>
        /// Load base genesis blocks for FM.ONE
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public byte[] GetGenesis(string name)
        {
            return ExtractResource(Assembly.GetExecutingAssembly()
                                   , "FreeMarketOne.GenesisBlock.Genesis." + name
                                   );
        }

        /// <summary>
        /// Extract resource from project resources
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        private byte[] ExtractResource(Assembly assembly, string resourceName)
        {
            if (assembly == null)
            {
                return null;
            }

            using (Stream resFilestream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resFilestream == null)
                {
                    return null;
                }

                byte[] bytes = new byte[resFilestream.Length];
                resFilestream.Read(bytes, 0, bytes.Length);

                return bytes;
            }
        }

        public Block<MarketAction> GetGenesisMarketBlockByHash(
            List<IBaseItem> blockCheckPoints,
            IDefaultBlockPolicy<MarketAction> blockPolicy)
        {
            if (blockCheckPoints.Any())
            {
                //ADDDDDD STORAGE genesis chain!!!! if it is valid!!!

                var now = DateTime.UtcNow;
                now = now.AddMilliseconds(blockPolicy.ValidBlockInterval.Value.Negate().TotalMilliseconds);

                CheckPointMarketDataV1 genesisCheckPointAction = null;

                //looking for the old genesis block
                foreach (var itemBlockCheckPoint in blockCheckPoints)
                {
                    genesisCheckPointAction = (CheckPointMarketDataV1)itemBlockCheckPoint;
                    if (itemBlockCheckPoint.CreatedUtc < now) break;
                }

                if (genesisCheckPointAction == null)
                    genesisCheckPointAction = (CheckPointMarketDataV1)blockCheckPoints.First();

                var genesisBlockBytes = ByteUtil.ParseHex(genesisCheckPointAction.Block);
                var genesisBlock = new Block<MarketAction>().Deserialize(genesisBlockBytes);

                return genesisBlock;
            }
            else
            {
                return null;
            }
        }
    }
}

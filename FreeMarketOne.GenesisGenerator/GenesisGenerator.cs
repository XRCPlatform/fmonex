using FreeMarketOne.BlockChain.Actions;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using System;
using System.Collections.Generic;
using System.IO;

namespace FreeMarketOne.GenesisBlock
{
    public class GenesisGenerator
    {
        public void GenerateIt(string baseBlockChainFilePath, string marketBlockChainFilePath)
        {
            //make plain market chain block
            var filePath = Path.Combine(marketBlockChainFilePath, "genesis.dat");
            if (!File.Exists(filePath))
            {
                Block<MarketBlockChainAction> genesisMarket =
                    BlockChain<MarketBlockChainAction>.MakeGenesisBlock();

                Directory.CreateDirectory(marketBlockChainFilePath);
                File.WriteAllBytes(filePath, genesisMarket.Serialize());

                //generate plain base chain block
                List<BaseBlockChainAction> actionsGenesis = new List<BaseBlockChainAction>();
                var newAction = new BaseBlockChainAction();
                var checkPoint = new CheckPointMarketDataV1();

                checkPoint.BlockHash = ByteUtil.Hex(genesisMarket.Hash.ByteArray);
                checkPoint.CreatedUtc = DateTime.UtcNow;
                checkPoint.BlockDateTime = genesisMarket.Timestamp;
                checkPoint.Hash = checkPoint.GenerateHash();

                newAction.AddBaseItem(checkPoint);
                actionsGenesis.Add(newAction);

                Block<BaseBlockChainAction> genesis =
                    BlockChain<BaseBlockChainAction>.MakeGenesisBlock(actionsGenesis);

                filePath = Path.Combine(baseBlockChainFilePath, "genesis.dat");
                Directory.CreateDirectory(baseBlockChainFilePath);
                File.WriteAllBytes(filePath, genesis.Serialize());
            }
        }
    }
}

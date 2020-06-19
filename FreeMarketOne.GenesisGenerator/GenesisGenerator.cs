﻿using FreeMarketOne.DataStructure;
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
        public void GenerateIt(IBaseConfiguration configuration)
        {
            //make plain market chain block
            var filePath = Path.Combine(configuration.FullBaseDirectory, configuration.BlockChainMarketPath, "genesis.dat");
            if (!File.Exists(filePath))
            {
                Block<MarketAction> genesisMarket =
                    BlockChain<MarketAction>.MakeGenesisBlock();

                Directory.CreateDirectory(Path.Combine(configuration.FullBaseDirectory, configuration.BlockChainMarketPath));
                File.WriteAllBytes(filePath, genesisMarket.Serialize());

                //generate plain base chain block
                List<BaseAction> actionsGenesis = new List<BaseAction>();
                var newAction = new BaseAction();
                var checkPoint = new CheckPointMarketDataV1();

                checkPoint.Block = ByteUtil.Hex(genesisMarket.Serialize());
                checkPoint.CreatedUtc = DateTime.UtcNow;
                checkPoint.Hash = checkPoint.GenerateHash();

                newAction.AddBaseItem(checkPoint);
                actionsGenesis.Add(newAction);

                Block<BaseAction> genesis =
                    BlockChain<BaseAction>.MakeGenesisBlock(actionsGenesis);

                filePath = Path.Combine(configuration.FullBaseDirectory, configuration.BlockChainBasePath, "genesis.dat");
                Directory.CreateDirectory(Path.Combine(configuration.FullBaseDirectory, configuration.BlockChainBasePath));
                File.WriteAllBytes(filePath, genesis.Serialize());
            }
        }
    }
}

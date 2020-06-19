using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using Libplanet;
using Libplanet.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeMarketOne.ServerCore.Helpers
{
    internal static class BlockHelper
    {
        internal static Block<MarketAction> GetGenesisMarketBlockByHash(List<IBaseItem> hashCheckPoints)
        {
            if (hashCheckPoints.Any())
            {
                //validity!!!!
                var checkPointAction = (CheckPointMarketDataV1)hashCheckPoints.First();

                var genesisBlockBytes = ByteUtil.ParseHex(checkPointAction.Block);
                var genesisBlock = Block<MarketAction>.Deserialize(genesisBlockBytes);

                return genesisBlock;
            }
            else
            {
                return null;
            }
        }
    }
}

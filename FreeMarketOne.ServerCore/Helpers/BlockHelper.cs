using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using Libplanet;
using Libplanet.Blocks;
using Libplanet.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeMarketOne.ServerCore.Helpers
{
    internal static class BlockHelper
    {
        internal static Block<MarketAction> GetGenesisMarketBlockByHash(
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

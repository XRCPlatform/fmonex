using Libplanet.Extensions;

namespace FreeMarketOne.DataStructure
{
    public class ExtendedConfiguration: BaseConfiguration
    {
        public IDefaultBlockPolicy<BaseAction> BlockChainBasePolicy { get; set; }
        public IDefaultBlockPolicy<MarketAction> BlockChainMarketPolicy { get; set; }
    }
}

using FreeMarketOne.DataStructure.Objects.BaseItems;
using Libplanet.Action;
using System.Collections.Generic;

namespace FreeMarketOne.BlockChain.Actions
{
    public interface IBaseAction : IAction
    {
        List<IBaseItem> BaseItems { get; }
        void AddBaseItem(IBaseItem value);
    }
}

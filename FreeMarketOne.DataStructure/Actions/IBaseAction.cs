using FreeMarketOne.DataStructure.Objects.BaseItems;
using Libplanet.Action;
using System.Collections.Generic;

namespace FreeMarketOne.DataStructure
{
    public interface IBaseAction : IAction
    {
        List<IBaseItem> BaseItems { get; }
        void AddBaseItem(IBaseItem value);
    }
}

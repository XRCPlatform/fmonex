using Bencodex.Types;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using Libplanet.Action;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.BlockChain
{
    public interface IBaseAction : IAction
    {
        List<IBaseItem> BaseItems { get; }
        void AddBaseItem(IBaseItem value);
    }
}

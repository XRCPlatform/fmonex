using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.MarketItems
{
    public interface IMarketItem
    {
        int Version { get; set; }
        int Title { get; set; }
        string Hash { get; set; }
        string Description { get; set; }
        DateTime CreatedUtc { get; set; }
        bool IsValid();
    }
}

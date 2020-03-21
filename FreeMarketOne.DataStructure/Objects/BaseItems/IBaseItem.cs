using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.BaseItems
{
    public interface IBaseItem
    {
        int Version { get; set; }
        string Hash { get; set; }
        DateTime CreatedUtc { get; set; }
        bool IsValid();
    }
}

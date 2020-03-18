using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.Item
{
    interface IItem
    {
        int Version { get; set; }
        int Title { get; set; }
        string Hash { get; set; }
        bool IsValid();
    }
}

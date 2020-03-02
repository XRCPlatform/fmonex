using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.Item
{
    interface IItem
    {
        public int Version { get; set; }
        public int Title { get; set; }
        public string Hash { get; set; }
        public bool IsValid();
    }
}

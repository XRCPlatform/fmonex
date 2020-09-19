using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.Search
{
    public class Selector
    {
        public Selector(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }
    }
}

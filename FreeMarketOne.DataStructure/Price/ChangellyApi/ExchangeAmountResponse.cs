using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Price.ChangellyApi
{
    public class ExchangeAmountResponse
    {
        public string jsonrpc { get; set; }
        public int id { get; set; }
        public ExchangeAmountPair[] result { get; set; }
    }    

}

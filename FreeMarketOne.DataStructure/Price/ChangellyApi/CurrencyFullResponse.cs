using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Price.ChangellyApi
{
    public class CurrencyFullResponse
    {
        public string jsonrpc { get; set; }
        public int id { get; set; }
        public CurrencyFull[] result { get; set; }
    }    

}

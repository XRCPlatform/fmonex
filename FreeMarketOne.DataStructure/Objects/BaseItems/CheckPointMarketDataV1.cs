using FreeMarketOne.Utils.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.BaseItems
{
    public class CheckPointMarketDataV1 : CheckPointMarketData
    {
        public CheckPointMarketDataV1()
        {
            this.nametype = "CheckPointMarketDataV1";
        }
    }
}

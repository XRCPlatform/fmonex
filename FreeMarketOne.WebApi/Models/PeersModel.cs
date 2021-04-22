using System;
using System.Collections.Generic;

namespace FreeMarketOne.WebApi.Models
{
    public class PeersModel
    {
        

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int) (TemperatureC / 0.5556);

        public string Summary { get; set; }
    }
}
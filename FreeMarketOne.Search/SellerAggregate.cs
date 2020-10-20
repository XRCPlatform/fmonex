using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.Search
{
    public class SellerAggregate
    {
        public double TotalXRCVolume { get; set; }
        public int StarRating { get; set; }
        public List<string> PublicKeyHashes { get; set; }
        public string SellerName { get; set; }

    }
}

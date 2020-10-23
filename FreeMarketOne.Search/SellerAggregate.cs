using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.Search
{
    public class SellerAggregate
    {
        public double TotalXRCVolume { get; set; }
        public double StarRating { get; set; }
        public List<string> PublicKeyHashes { get; set; }
        public string SellerName { get; set; }
        public List<byte[]> PublicKeys { get; set; }
        public Dictionary<string,double> XRCTransactions { get; set; } = new Dictionary<string, double>();
    }
}

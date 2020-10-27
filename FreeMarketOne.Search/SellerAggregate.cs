using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.Search
{
    public class SellerAggregate
    {
        public decimal TotalXRCVolume { get; set; }
        public double StarRating { get; set; }
        public List<string> PublicKeyHashes { get; set; } = new List<string>();
        public string SellerName { get; set; }
        public List<byte[]> PublicKeys { get; set; }
        public Dictionary<string,decimal> XRCTransactions { get; set; } = new Dictionary<string, decimal>();
    }
}

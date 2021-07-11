using System;

namespace FreeMarketOne.Search
{
    public class XRCTransactionSummary
    {
        public double Total { get; set; }
        public long Confirmations { get; set; }
        public DateTimeOffset Date { get; set; }
    }
}
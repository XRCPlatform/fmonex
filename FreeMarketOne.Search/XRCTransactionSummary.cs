﻿using System;

namespace FreeMarketOne.Search
{
    public class XRCTransactionSummary
    {
        public decimal Total { get; set; }
        public int Confirmations { get; set; }
        public DateTimeOffset Date { get; set; }
    }
}
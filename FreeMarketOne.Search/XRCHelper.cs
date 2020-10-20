namespace FreeMarketOne.Search
{
    public class XRCHelper : IXRCHelper
    {
        public XRCTransactionSummary GetTransaction(string hash, string address)
        {
            return new XRCTransactionSummary()
            {
                Total = 0
            };
        }
    }
}
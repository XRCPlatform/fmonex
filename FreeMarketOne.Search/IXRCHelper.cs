using FreeMarketOne.DataStructure;

namespace FreeMarketOne.Search
{
    public interface IXRCHelper
    {
        XRCTransactionSummary GetTransaction(string hash, string address);
    }
}
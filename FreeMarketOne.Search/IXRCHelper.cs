using FreeMarketOne.DataStructure;

namespace FreeMarketOne.Search
{
    public interface IXRCHelper
    {
        XRCTransactionSummary GetTransaction(string address, string hash);
    }
}
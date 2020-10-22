using FreeMarketOne.DataStructure;

namespace FreeMarketOne.Search
{
    public interface IXRCHelper
    {
        XRCTransactionSummary GetTransaction(IBaseConfiguration baseConfiguration, string hash, string address);
    }
}
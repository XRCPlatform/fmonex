using FreeMarketOne.Search.XRCDaemon;
using System.Threading.Tasks;

namespace FreeMarketOne.Search
{
    public interface IXRCDaemonClient
    {
        Task<TransactionVerboseModel> GetTransaction(string transactionHash);
    }
}
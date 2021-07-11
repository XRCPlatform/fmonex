using ElectrumXClient.Response;
using System.Threading.Tasks;

namespace ElectrumXClient
{
    public interface IElectrumClient
    {
        Task<BlockchainTransactionBroadcastResponse> BlockchainTransactionBroadcast(string tx);
        void Dispose();
        Task<BlockchainBlockHeaderResponse> GetBlockchainBlockHeader(uint height);
        Task<BlockchainEstimatefeeResponse> GetBlockchainEstimatefee(uint number);
        Task<BlockchainScripthashListunspentResponse> GetBlockchainListunspent(string scripthash);
        Task<BlockchainNumblocksSubscribeResponse> GetBlockchainNumblocksSubscribe();
        Task<BlockchainScripthashGetBalanceResponse> GetBlockchainScripthashGetBalance(string scripthash);
        Task<BlockchainScripthashGetHistoryResponse> GetBlockchainScripthashGetHistory(string scripthash);
        Task<BlockchainTransactionGetResponse> GetBlockchainTransactionGet(string txhash, bool verbose = false);
        Task<ServerFeaturesResponse> GetServerFeatures();
        Task<ServerPeersSubscribeResponse> GetServerPeersSubscribe();
        Task<ServerVersionResponse> GetServerVersion();
        Task<BlockchainTransactionIdFromPosResponse> GetTransactionIdFromPos(uint height, uint index);
    }
}
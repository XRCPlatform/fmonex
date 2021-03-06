using ElectrumXClient.Request;
using ElectrumXClient.Response;
using FreeMarketOne.Search;
using FreeMarketOne.Search.XRCDaemon;
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace ElectrumXClient
{
    public class ElectrumClient : IDisposable, IElectrumClient
    {
        //private string _host;
        private int _port;
        private bool _useSSL;
        TcpClient _tcpClient;
        SslStream _sslStream;
        NetworkStream _tcpStream;
        Stream _stream;
        readonly int BUFFERSIZE = 32;
        private string[] _hosts;

        public ElectrumClient(string host, int port, bool useSSL)
        {
            _hosts = host.Split(",");
            _port = port;
            _useSSL = useSSL;
        }

        public async Task<ServerVersionResponse> GetServerVersion()
        {
            var request = new ServerVersionRequest();
            var requestData = request.GetRequestData<ServerVersionRequest>();
            await this.Connect();
            string response = await SendMessage(requestData);
            this.Disconnect();
            return ServerVersionResponse.FromJson(response);
        }

        public async Task<ServerPeersSubscribeResponse> GetServerPeersSubscribe()
        {
            var request = new ServerPeersSubscribeRequest();
            var requestData = request.GetRequestData<ServerPeersSubscribeRequest>();
            await this.Connect();
            string response = await SendMessage(requestData);
            this.Disconnect();
            return ServerPeersSubscribeResponse.FromJson(response);
        }

        public async Task<BlockchainNumblocksSubscribeResponse> GetBlockchainNumblocksSubscribe()
        {
            var request = new BlockchainNumblocksSubscribeRequest();
            var requestData = request.GetRequestData<BlockchainNumblocksSubscribeRequest>();
            await this.Connect();
            string response = await SendMessage(requestData);
            this.Disconnect();
            return BlockchainNumblocksSubscribeResponse.FromJson(response);
        }

        public async Task<BlockchainBlockHeaderResponse> GetBlockchainBlockHeader(uint height)
        {
            var request = new BlockchainBlockHeaderRequest();
            request.Parameters = new object[1] { height };
            var requestData = request.GetRequestData<BlockchainBlockHeaderRequest>();
            await this.Connect();
            string response = await SendMessage(requestData);
            this.Disconnect();
            return BlockchainBlockHeaderResponse.FromJson(response);
        }

        public async Task<BlockchainEstimatefeeResponse> GetBlockchainEstimatefee(uint number)
        {
            var request = new BlockchainEstimatefeeRequest();
            request.Parameters = new uint[] { number };
            var requestData = request.GetRequestData<BlockchainEstimatefeeRequest>();
            await this.Connect();
            string response = await SendMessage(requestData);
            this.Disconnect();
            return BlockchainEstimatefeeResponse.FromJson(response);
        }

        public async Task<BlockchainScripthashGetBalanceResponse> GetBlockchainScripthashGetBalance(string scripthash)
        {
            var request = new BlockchainScripthashGetBalanceRequest();
            request.Parameters = new string[] { scripthash };
            var requestData = request.GetRequestData<BlockchainScripthashGetBalanceRequest>();
            await this.Connect();
            string response = await SendMessage(requestData);
            this.Disconnect();
            return BlockchainScripthashGetBalanceResponse.FromJson(response);
        }

        public async Task<BlockchainScripthashGetHistoryResponse> GetBlockchainScripthashGetHistory(string scripthash)
        {
            var request = new BlockchainScripthashGetHistoryRequest();
            request.Parameters = new string[] { scripthash };
            var requestData = request.GetRequestData<BlockchainScripthashGetHistoryRequest>();
            await this.Connect();
            string response = await SendMessage(requestData);
            this.Disconnect();
            return BlockchainScripthashGetHistoryResponse.FromJson(response);
        }

        public async Task<BlockchainScripthashListunspentResponse> GetBlockchainListunspent(string scripthash)
        {
            var request = new BlockchainScripthashListunspentRequest();
            request.Parameters = new string[] { scripthash };
            var requestData = request.GetRequestData<BlockchainScripthashListunspentRequest>();
            await this.Connect();
            string response = await SendMessage(requestData);
            this.Disconnect();
            return BlockchainScripthashListunspentResponse.FromJson(response);
        }

        public async Task<BlockchainTransactionGetResponse> GetBlockchainTransactionGet(string txhash, bool verbose = false)
        {
            var request = new BlockchainTransactionGetRequest();
            request.Parameters = new object[] { txhash, verbose };
            var requestData = request.GetRequestData<BlockchainTransactionGetRequest>();
            await this.Connect();
            string response = await SendMessage(requestData);
            this.Disconnect();
            return BlockchainTransactionGetResponse.FromJson(response);
        }

        public async Task<BlockchainTransactionBroadcastResponse> BlockchainTransactionBroadcast(string tx)
        {
            var request = new BlockchainTransactionBroadcastRequest();
            request.Parameters = new object[] { tx };
            var requestData = request.GetRequestData<BlockchainTransactionBroadcastRequest>();
            await this.Connect();
            string response = await SendMessage(requestData);
            this.Disconnect();
            return BlockchainTransactionBroadcastResponse.FromJson(response);
        }

        public async Task<ServerFeaturesResponse> GetServerFeatures()
        {
            var request = new ServerFeaturesRequest();
            var requestData = request.GetRequestData<ServerFeaturesRequest>();
            await this.Connect();
            string response = await SendMessage(requestData);
            this.Disconnect();
            return ServerFeaturesResponse.FromJson(response);
        }

        private string GetBestHost()
        {
            Random rnd = new Random();
            int index = rnd.Next(1, _hosts.Length);
            return _hosts[index - 1];
        }

        private async Task Connect()
        {
            string _host = GetBestHost();
            _tcpClient = new TcpClient();

            await _tcpClient.ConnectAsync(_host, _port);
            if (_useSSL)
            {
                _sslStream = new SslStream(_tcpClient.GetStream(), true,
                    new RemoteCertificateValidationCallback(CertificateValidationCallback));
                await _sslStream.AuthenticateAsClientAsync(_host);
                _stream = _sslStream;
            }
            else
            {
                _tcpStream = _tcpClient.GetStream();
                _stream = _tcpStream;
            }
        }

        private void Disconnect()
        {
            _stream.Close();
            _tcpClient.Close();
        }

        private async Task<string> SendMessage(byte[] requestData)
        {
            var response = string.Empty;
            var buffer = new byte[BUFFERSIZE];
            await _stream.WriteAsync(requestData, 0, requestData.Length);

            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = await _stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                    if (read < BUFFERSIZE) break;
                }
                response = System.Text.Encoding.ASCII.GetString(ms.ToArray());
            }

            return response;
        }

        private static bool CertificateValidationCallback(object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // We don't check the validity of the certificate (yet)
            return true;
        }

        public void Dispose()
        {
            if (_sslStream != null) ((IDisposable)_sslStream).Dispose();
            ((IDisposable)_tcpClient).Dispose();
        }

        public async Task<BlockchainTransactionIdFromPosResponse> GetTransactionIdFromPos(uint height, uint index)
        {
            var request = new BlockchainTransactionIdFromPosRequest();
            request.Parameters = new object[2] { height, index };
            var requestData = request.GetRequestData<BlockchainTransactionIdFromPosRequest>();
            await this.Connect();
            string response = await SendMessage(requestData);
            this.Disconnect();
            return BlockchainTransactionIdFromPosResponse.FromJson(response);
        }


    }
}

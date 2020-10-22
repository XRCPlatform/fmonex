using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.WebSockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FreeMarketOne.DataStructure;
using FreeMarketOne.Search.XRCDaemon;
using FreeMarketOne.Search.XRCDaemon.JsonRpc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace FreeMarketOne.Search
{
    /// <summary>
    /// Provides JsonRpc based interface to a cluster of xrc daemons for improved fault tolerance
    /// </summary>
    public class XRCDaemonClient
    {
        private readonly JsonSerializerSettings serializerSettings;
        private readonly JsonSerializer serializer;
        private readonly HttpClient httpClient;
        private readonly IBaseConfiguration configuration;
        private readonly ILogger logger;
        public XRCDaemonClient(JsonSerializerSettings serializerSettings, IBaseConfiguration config, ILogger logger)
        {
            this.serializerSettings = serializerSettings;
            this.configuration = config;
            this.logger = logger;

            serializer = new JsonSerializer
            {
                ContractResolver = serializerSettings.ContractResolver
            };
            httpClient = GetHttpClient(config);
        }

        public async Task<TransactionVerboseModel> GetTransaction(string transactionHash)
        {
            string[] args = new string[]
            {
                transactionHash,
                "1"
            };
            var response = await ExecuteCmdSingleAsync<TransactionVerboseModel>("getrawtransaction", args).ConfigureAwait(false);
            return response;
        }

        /// <summary>
        /// Executes the request against all configured demons and returns the first successful response
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="method"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public async Task<T> ExecuteCmdSingleAsync<T>(string method, object payload = null, JsonSerializerSettings payloadJsonSerializerSettings = null)
        {
            return await Execute<T>( method, payload, CancellationToken.None, payloadJsonSerializerSettings).ConfigureAwait(false);
        }

        private async Task<T> Execute<T>(string method, object payload, CancellationToken ct, JsonSerializerSettings payloadJsonSerializerSettings = null)
        {
            var rpcRequestId = GetRequestId();


            // build rpc request
            var rpcRequest = new DeamonJsonRpcRequest<object>(method, payload, rpcRequestId);

            // build request url
            var protocol = (configuration.XRCDaemonUriSsl) ? "https" : "http";
            var requestUrl = $"{protocol}://{configuration.XRCDaemonUri}";//:{endPoint.Port}";


            // build http request
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            var json = JsonConvert.SerializeObject(rpcRequest, payloadJsonSerializerSettings ?? serializerSettings);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");


            // build auth header
            if (!string.IsNullOrEmpty(configuration.XRCDaemonUser))
            {
                var auth = $"{configuration.XRCDaemonUser}:{configuration.XRCDaemonPassword}";
                var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(auth));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64);
            }

            logger.Debug($"Sending RPC request to {requestUrl}: {json}");

            // send request
            using (var response = await httpClient.SendAsync(request, ct))
            {
                // check success
                if (!response.IsSuccessStatusCode)
                {
                    throw new DaemonClientException(response.StatusCode, response.ReasonPhrase);
                }

                // deserialize response
                var jsonResponse = await response.Content.ReadAsStringAsync();

               
                using (var reader = new StringReader(jsonResponse))
                {
                    using (var jreader = new JsonTextReader(reader))
                    {
                        var interimResult = serializer.Deserialize<JsonRpcResponse<T>>(jreader);
                        if (interimResult == null) return default;
                        return interimResult.Result;
                    }
                }
               
            }
        }

        protected string GetRequestId()
        {
            var rpcRequestId = (DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString();
            return rpcRequestId;
        }

        private HttpClient GetHttpClient(IBaseConfiguration config)
        {
            var handler = new SocketsHttpHandler
            {
                //Credentials = new NetworkCredential(endpoint.User, endpoint.Password),
                //PreAuthenticate = true,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            };

            if (config.XRCDaemonUriSsl)
            {
                handler.SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = ((sender, certificate, chain, errors) => true),
                };
            }

            var httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromMinutes(10);
            return httpClient;
        }

       
    }
}

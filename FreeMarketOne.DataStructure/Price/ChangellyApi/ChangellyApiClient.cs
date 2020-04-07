using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Runtime.Caching;

namespace FreeMarketOne.DataStructure.Price.ChangellyApi
{
    public class ChangellyApiClient
    {
        IBaseConfiguration configuration;
        private MemoryCache cache { get;}
        private readonly object constructorLock = new object();

        public ChangellyApiClient(IBaseConfiguration configuration)
        {
            this.configuration = configuration;

            lock (constructorLock)
            {
                if (cache == null)
                {
                    cache = new MemoryCache("ChangellyApiClient");
                }
            }
            
        }

        public ExchangeAmountResponse GetExchangeAmount(Currency baseCurrency, Currency[] currencies, double amount)
        {
            ExchangeAmountRequest request = new ExchangeAmountRequest(baseCurrency, currencies, amount);
            string message = JsonConvert.SerializeObject(request, Formatting.Indented);
            string response = ProcessRequest(message);
            //{"jsonrpc":"2.0","id":0,"error":{"code":-32600,"message":"invalid amount: minimal amount is 2.69745195"}}
            return JsonConvert.DeserializeObject<ExchangeAmountResponse>(response);
        }

        private string ProcessRequest(string message)
        {
            string signature = SignRequest(this.configuration.ChangellySecret, message);

            HttpClient httpClient = new HttpClient();

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, this.configuration.ChangellyApiBaseUrl);
            httpRequest.Content = new JsonContent(message);
            httpRequest.Headers.TryAddWithoutValidation("Content-Type", "application/json");
            httpRequest.Headers.Add("api-key", configuration.ChangellyApiKey);
            httpRequest.Headers.Add("sign", signature);

            var response = httpClient.SendAsync(httpRequest).ConfigureAwait(false).GetAwaiter().GetResult();
           
            if (response.IsSuccessStatusCode)
            {
                string rJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                
                //check for error object in body for logical errors
                dynamic jToken = JToken.Parse(rJson);
                if (jToken.error != null)
                {
                    throw new Exception(jToken.error.message.ToString());
                }
                
                return rJson;
            }
            else
            {
                throw new Exception("Error accessing changelly api, http status code:" + response.StatusCode);
            }
        }

        public string[] GetCurrencies()
        {
            throw new NotImplementedException();
        }

        public CurrencyFullResponse GetCurrenciesFull()
        {
            CurrencyFullResponse response = null;
            if (cache.Contains("GetCurrenciesFull"))
            {
                response = cache.Get("GetCurrenciesFull") as CurrencyFullResponse;
            }
            if (response == null)
            {
                CurrenciesFullRequest request = new CurrenciesFullRequest();
                string message = JsonConvert.SerializeObject(request, Formatting.Indented);
                response = JsonConvert.DeserializeObject<CurrencyFullResponse>(ProcessRequest(message));
                cache.Set("GetCurrenciesFull", response, new CacheItemPolicy()
                        {
                            SlidingExpiration = TimeSpan.FromMinutes(10)
                        }
                );
            }           
            return response;
        }


        public GetMinAmountResponse GetMinAmount(Currency from, Currency[] to)
        {
            GetMinAmountResponse response = null;            
            string key = $"GetMinAmount_{string.Join("_", to)}";
            if (cache.Contains(key))
            {
                response = cache.Get(key) as GetMinAmountResponse;
            }
            if (response == null)
            {
                GetMinAmountRequest request = new GetMinAmountRequest(from, to);
                string message = JsonConvert.SerializeObject(request, Formatting.Indented);
                response = JsonConvert.DeserializeObject<GetMinAmountResponse>(ProcessRequest(message));
                cache.Set(key, response, new CacheItemPolicy()
                {
                    SlidingExpiration = TimeSpan.FromMinutes(10)
                }
                );
            }
            return response;            
        }

        public string SignRequest(string key, string payload)
        {
            byte[] key_bytes = Encoding.UTF8.GetBytes(key);
            byte[] payload_bytes = Encoding.UTF8.GetBytes(payload);
            
            using (HMACSHA512 hmac = new HMACSHA512(key_bytes))
            {
                byte[] hashmessage = hmac.ComputeHash(payload_bytes);
                return hashmessage.ToLowerHex();
                // to lowercase hexits (slower ?? test)
                //return String.Concat(Array.ConvertAll(hashmessage, x => x.ToString("x2")));
            }
        }

        public bool ValidateAddress(Currency currency, string address)
        {
            ValidateAddressRequest request = new ValidateAddressRequest(currency.ToString().ToLower(), address);
            string message = JsonConvert.SerializeObject(request, Formatting.Indented);
            string response = ProcessRequest(message);
            return JsonConvert.DeserializeObject<ValidateAddressResponse>(response).ValidationResult.IsValid;
        }

        public ChangellyTransaction CreateTransaction(Currency from, Currency to, string address, string refundAddress, double amount)
        {
            CreateTransactionRequest request = new CreateTransactionRequest(from.ToString().ToLower(), to.ToString().ToLower(), address, refundAddress, amount);
            string message = JsonConvert.SerializeObject(request, Formatting.Indented);
            string response = ProcessRequest(message);
            /*
            error case
            {
                "jsonrpc": "2.0",
                "id": "test",
                "error": {
                    "code": -32600,
                    "message": "invalid amount: minimal amount is 2.84306295"
                }
            }
            */
            dynamic jToken = JToken.Parse(response);
            if (jToken.error != null)
            {
                throw new Exception(jToken.error.message.ToString());
            }

            return JsonConvert.DeserializeObject<CreateTransactionResponse>(response).InitiatedTransaction;
        }
        
    }
}
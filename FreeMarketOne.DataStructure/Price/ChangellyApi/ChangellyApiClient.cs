using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FreeMarketOne.DataStructure.Price.ChangellyApi
{
    public class ChangellyApiClient
    {
        IBaseConfiguration configuration;

        public ChangellyApiClient(IBaseConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public ExchangeAmountResponse GetExchangeAmount(Currency baseCurrency, Currency[] currencies, double amount)
        {
            ExchangeAmountRequest request = new ExchangeAmountRequest(baseCurrency, currencies, amount);
            string message = JsonConvert.SerializeObject(request, Formatting.Indented);
            return JsonConvert.DeserializeObject<ExchangeAmountResponse>(ProcessRequest(message));
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
                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
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
            CurrenciesFullRequest request = new CurrenciesFullRequest();
            string message = JsonConvert.SerializeObject(request, Formatting.Indented);
            return JsonConvert.DeserializeObject<CurrencyFullResponse>(ProcessRequest(message));
        }


        public GetMinamountResponse GetMinAmount(Currency from, Currency to)
        {
            GetMinAmountRequest request = new GetMinAmountRequest(from, to);
            string message = JsonConvert.SerializeObject(request, Formatting.Indented);
            string m = ProcessRequest(message);
            return JsonConvert.DeserializeObject<GetMinamountResponse>(m);
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
    }
}
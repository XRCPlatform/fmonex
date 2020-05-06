using FreeMarketOne.DataStructure;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace FreeMarketOne.Telemetry
{
    public class ElasticTelemetryHub : ITelemetryHub
    {
        private static HttpClient httpClient = new HttpClient();
        private IBaseConfiguration configuration;

        public ElasticTelemetryHub(IBaseConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async void Send(ITelemetryMeasure measure)
        {
            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, this.configuration.TelemetryServerUri);
            httpRequest.Content = new JsonContent(measure);
            httpRequest.Headers.TryAddWithoutValidation("Content-Type", "application/json");
            //recomended by elastic client
            httpRequest.Headers.TransferEncodingChunked = false;
            httpRequest.Headers.ConnectionClose = false;                
            //deliberate fire and forget to avoid negatively impacting ux
            await httpClient.SendAsync(httpRequest).ConfigureAwait(false);
        }
    }
}

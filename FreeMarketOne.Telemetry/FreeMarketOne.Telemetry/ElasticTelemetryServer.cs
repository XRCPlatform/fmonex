using FreeMarketOne.DataStructure;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace FreeMarketOne.Telemetry
{
    public class ElasticTelemetryServer : ITelemetryServer
    {
        private IBaseConfiguration configuration;

        public ElasticTelemetryServer(IBaseConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async void Send(ITemetryMeasure measure)
        {
            HttpClient httpClient = new HttpClient();
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

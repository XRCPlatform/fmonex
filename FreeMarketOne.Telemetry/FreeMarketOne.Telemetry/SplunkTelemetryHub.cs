using FreeMarketOne.DataStructure;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace FreeMarketOne.Telemetry
{
    public class SplunkTelemetryHub
    {
        //test token: 4f96cae6-581e-472e-92d1-27eeb2116290
        //
        static HttpClient httpClient = new HttpClient();
        private IBaseConfiguration configuration;

        public SplunkTelemetryHub(IBaseConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async void Send(ITelemetryMeasure measure)
        {
            TelemetryEvent telemetryEvent = new TelemetryEvent(measure);
            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, this.configuration.TelemetryServerUri);
            httpRequest.Content = new JsonContent(telemetryEvent);
            httpRequest.Headers.TryAddWithoutValidation("Content-Type", "application/json");
            httpRequest.Headers.Add("Authorization", "Splunk 1bab5569-7e28-4537-a0b2-3be52b5d40c1");
            //recomended by elastic client
            httpRequest.Headers.TransferEncodingChunked = false;
            httpRequest.Headers.ConnectionClose = false;
            //deliberate fire and forget to avoid negatively impacting ux
            await httpClient.SendAsync(httpRequest).ConfigureAwait(false);
        }
    }
}

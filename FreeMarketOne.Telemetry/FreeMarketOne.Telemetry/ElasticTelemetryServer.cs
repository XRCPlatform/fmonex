using FreeMarketOne.DataStructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.Telemetry
{
    public class ElasticTelemetryServer : ITelemetryServer
    {
        private string serverUri;
        public ElasticTelemetryServer(IBaseConfiguration configuration)
        {
            serverUri = configuration.TelemetryServerUri;
        }
        public void Send(ITemetryMeasure measure)
        {
            throw new NotImplementedException();
        }
    }
}

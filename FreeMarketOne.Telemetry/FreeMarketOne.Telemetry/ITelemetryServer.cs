using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.Telemetry
{
    interface ITelemetryServer
    {
        /// <summary>
        /// Send telemetry data to server for processing. 
        /// </summary>
        /// <param name="measure"></param>
        void Send(ITemetryMeasure measure);
    }
}

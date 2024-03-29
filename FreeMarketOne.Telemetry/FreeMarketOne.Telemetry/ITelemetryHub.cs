﻿using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.Telemetry
{
    interface ITelemetryHub
    {
        /// <summary>
        /// Send telemetry data to server for processing. Fire and forget.  
        /// </summary>
        /// <param name="measure"></param>
        void Send(ITelemetryMeasure measure);
    }
}

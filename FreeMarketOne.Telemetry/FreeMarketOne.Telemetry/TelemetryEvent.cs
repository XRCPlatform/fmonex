using Newtonsoft.Json;

namespace FreeMarketOne.Telemetry
{
    public class TelemetryEvent
    {
        public TelemetryEvent(ITelemetryMeasure measure)
        {
            Event = measure;
        }
        [JsonProperty("event")]
        public ITelemetryMeasure Event { get; set; }
    }
}

using FreeMarketOne.DataStructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace FreeMarketOne.Telemetry.Test
{
    [TestClass]
    public class SendBasicDataToSplunk
    {
        SplunkTelemetryHub hub = new SplunkTelemetryHub(new TestConfiguration());
        [TestMethod]
        public void TestMethod1()
        {
            TelemetryMeasure measure = new TelemetryMeasure() { 
                PlatformVersion = "v1",
                Time = DateTime.UtcNow,
                UserId = Guid.NewGuid().ToString(),
                Action = "search",
                ActionTarget = "Gold bulion",
                Environment = "dev",
                Outcome ="no results found"
            };            
            hub.Send(measure);
        }

        [TestMethod]
        public void TestMethod2()
        {
            TelemetryMeasure measure = new TelemetryMeasure()
            {
                PlatformVersion = "v1",
                Time = DateTime.UtcNow,
                UserId = Guid.NewGuid().ToString(),
                Action = "search",
                ActionTarget = "Gold bullion",
                Environment = "dev",
                Outcome = "500 results"
            };
           
            hub.Send(measure);
        }

        [TestMethod]
        public void TestMethodBid()
        {
            TelemetryMeasure measure = new TelemetryMeasure()
            {
                PlatformVersion = "v1",
                Time = DateTime.UtcNow,
                UserId = Guid.NewGuid().ToString(),
                Action = "post_offer_bid",
                ActionTarget = "Gold bullion",
                Environment = "dev",
                Outcome = "success"
            };
            hub.Send(measure);
        }

        [TestMethod]
        public void TestMethodAsk()
        {
            TelemetryMeasure measure = new TelemetryMeasure()
            {
                PlatformVersion = "v1",
                Time = DateTime.UtcNow,
                UserId = Guid.NewGuid().ToString(),
                Action = "post_offer_ask",
                ActionTarget = "Gold bullion",
                Environment = "dev",
                Outcome = "success"
            };

            hub.Send(measure);
        }

        [TestMethod]
        public void TestMethodTransact()
        {
            TelemetryMeasure measure = new TelemetryMeasure()
            {
                PlatformVersion = "v1",
                Time = DateTime.UtcNow,
                UserId = Guid.NewGuid().ToString(),
                Action = "transact",
                ActionTarget = Guid.NewGuid().ToString(),
                Environment = "dev",
                Outcome = "success"
            };

            hub.Send(measure);
        }
    }
}

using Libplanet.Net.Messages;
using System;
using Xunit;

namespace Libplanet.Tests.Net.Messages
{
    public class TxBroadcastTests
    {
        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            var random = new Random();
            var buffer = new byte[500];
            random.NextBytes(buffer);
            TxBroadcast msg = new TxBroadcast(buffer, false);
            var ben = msg.SerializeToBen();
            var result = new TxBroadcast(ben);
            Assert.Equal(msg.Payload, result.Payload);
        }
    }
}

using Libplanet.Net.Messages;
using System;
using Xunit;

namespace Libplanet.Tests.Net.Messages
{
	//FMONECHANGE added new test class to verify message serialziation handling
    public class TxBroadcastTests
    {
        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            var random = new Random();
            var buffer = new byte[500];
            random.NextBytes(buffer);
            Libplanet.Net.Messages.Tx msg = new Libplanet.Net.Messages.Tx(buffer, false);
            var ben = msg.SerializeToBen();
            var result = new Libplanet.Net.Messages.Tx(ben);
            Assert.Equal(msg.Payload, result.Payload);
        }
    }
}

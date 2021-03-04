using Libplanet.Net.Messages;
using Xunit;

namespace Libplanet.Tests.Net.Messages
{
    public class PingTests
    {
        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            var msg = new Ping();

            var ben = msg.SerializeToBen();
            var result = new Ping(ben);

            Assert.Equal(msg.GetType(), result.GetType());
        }
    }
}

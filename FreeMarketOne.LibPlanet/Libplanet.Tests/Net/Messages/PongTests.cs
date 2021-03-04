using Libplanet.Net.Messages;
using Xunit;

namespace Libplanet.Tests.Net.Messages
{
    public class PongTests
    {
        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            var msg = new Pong();

            var ben = msg.SerializeToBen();
            var result = new Pong(ben);

            Assert.Equal(msg.GetType(), result.GetType());
        }
    }
}

using Libplanet.Net.Messages;
using Xunit;

namespace Libplanet.Tests.Net.Messages
{
    public class DifferentVersionTests
    {
        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            var msg = new DifferentVersion();

            var ben = msg.SerializeToBen();
            var result = new DifferentVersion(ben);

            Assert.Equal(msg.GetType(), result.GetType());
        }
    }
}

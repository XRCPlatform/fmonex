using Libplanet.Net.Messages;
using Xunit;

namespace Libplanet.Tests.Net.Messages
{
	//FMONECHANGE - added missing test
    public class GetChainStatusTests
    {
        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            var msg = new GetChainStatus();

            var ben = msg.SerializeToBen();
            var result = new GetChainStatus(ben);

            Assert.Equal(msg.GetType(), result.GetType());
        }
    }
}

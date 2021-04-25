using Libplanet.Net.Messages;
using System;
using System.Security.Cryptography;
using Xunit;

namespace Libplanet.Tests.Net.Messages
{
	//FMONECHANGE - added missing test
    public class FindNeighborsTests
    {
        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            var random = new Random();
            var buffer = new byte[Address.Size];
            random.NextBytes(buffer);

            var msg = new FindNeighbors(new Address(buffer));

            var ben = msg.SerializeToBen();
            var result = new FindNeighbors(ben);

            Assert.Equal(msg.Target, result.Target);
        }
    }
}

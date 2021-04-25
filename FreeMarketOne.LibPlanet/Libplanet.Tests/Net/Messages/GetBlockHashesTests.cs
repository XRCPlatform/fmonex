using Libplanet.Net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Xunit;


namespace Libplanet.Tests.Net.Messages
{
	//FMONECHANGE - added missing test
    public class GetBlockHashesTests
    {

        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            HashDigest<SHA256>[] blockHashes = GenerateRandomBlockHashes(100L).ToArray();

            var random = new Random();
            var buffer = new byte[HashDigest<SHA256>.Size];            
            random.NextBytes(buffer);
            var stop = new HashDigest<SHA256>(buffer);

            var msg = new GetBlockHashes(new Libplanet.Blockchain.BlockLocator(blockHashes), stop);

            Assert.Equal(stop, msg.Stop);
            Assert.Equal(blockHashes, msg.Locator);

            var ben = msg.SerializeToBen();
            var result = new GetBlockHashes(ben);

            Assert.Equal(msg.Stop, result.Stop);
            Assert.Equal(msg.Locator, result.Locator);
        }

        private static IEnumerable<HashDigest<SHA256>> GenerateRandomBlockHashes(long count)
        {
            var random = new Random();
            var buffer = new byte[HashDigest<SHA256>.Size];
            for (long i = 0; i < count; i++)
            {
                random.NextBytes(buffer);
                yield return new HashDigest<SHA256>(buffer);
            }
        }
    }
}

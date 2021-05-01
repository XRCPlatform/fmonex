using Libplanet.Net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Xunit;


namespace Libplanet.Tests.Net.Messages
{
	//FMONECHANGE - added missing test
    public class GetRecentStatesTests
    {

        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            HashDigest<SHA256>[] blockHashes = GenerateRandomBlockHashes(100L).ToArray();

            var random = new Random();
            var buffer = new byte[HashDigest<SHA256>.Size];            
            random.NextBytes(buffer);
            var target =new HashDigest<SHA256>(buffer);

            var msg = new GetRecentStates(new Libplanet.Blockchain.BlockLocator(blockHashes), target, 123);
            Assert.Equal(123, msg.Offset);
            Assert.Equal(blockHashes, msg.BaseLocator);

            var ben = msg.SerializeToBen();
            var result = new GetRecentStates(ben);

            Assert.Equal(msg.Offset, result.Offset);
            Assert.Equal(msg.BaseLocator, result.BaseLocator);
            Assert.Equal(msg.TargetBlockHash, result.TargetBlockHash);
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

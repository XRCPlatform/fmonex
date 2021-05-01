using Libplanet.Net.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Xunit;


namespace Libplanet.Tests.Net.Messages
{
	//FMONECHANGE - added missing test
    public class GetBlocksTest
    {
        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            HashDigest<SHA256>[] blockHashes = GenerateRandomBlockHashes(100L).ToArray();

            var msg = new GetBlocks(blockHashes, 100);

            Assert.Equal(100, msg.ChunkSize);
            Assert.Equal(blockHashes, msg.BlockHashes);

            var ben = msg.SerializeToBen();
            var result = new GetBlocks(ben);

            Assert.Equal(msg.ChunkSize, result.ChunkSize);
            Assert.Equal(msg.BlockHashes, result.BlockHashes);
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

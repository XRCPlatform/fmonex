using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Xunit;

namespace Libplanet.Tests.Net.Messages
{
	//FMONECHANGE - added missing test
    public class BlocksTest
    {
        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            var payloads = GenerateRandomBlockPayloads(100);
            var random = new Random();
            var buffer = new byte[HashDigest<SHA256>.Size];
            random.NextBytes(buffer);
            var genesis = new HashDigest<SHA256>(buffer);

            Libplanet.Net.Messages.Blocks msg = new Libplanet.Net.Messages.Blocks(payloads, genesis);

            Assert.Equal(genesis, msg.GenesisHash);
            
            var ben = msg.SerializeToBen();
            var result = new Libplanet.Net.Messages.Blocks(ben);

            Assert.Equal(msg.GenesisHash, result.GenesisHash);
            Assert.Equal(msg.Payloads, result.Payloads);
        }

        private static IEnumerable<byte[]> GenerateRandomBlockPayloads(long count)
        {
            var random = new Random();
            var buffer = new byte[500];
            for (long i = 0; i < count; i++)
            {
                random.NextBytes(buffer);
                yield return buffer;
            }
        }
    }
}

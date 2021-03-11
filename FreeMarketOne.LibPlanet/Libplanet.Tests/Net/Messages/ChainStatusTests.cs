using Libplanet.Net.Messages;
using System;
using System.Numerics;
using System.Security.Cryptography;
using Xunit;


namespace Libplanet.Tests.Net.Messages
{
    public class ChainStatusTests
    {
        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            var random = new Random();
            var buffer = new byte[HashDigest<SHA256>.Size];
            random.NextBytes(buffer);
            var genesis = new HashDigest<SHA256>(buffer);
            var difficulty = new BigInteger(buffer);
            ChainStatus msg = new ChainStatus(genesis, 123, difficulty);

            Assert.Equal(genesis, msg.GenesisHash);
            Assert.Equal(123, msg.TipIndex);
            Assert.Equal(difficulty, msg.TotalDifficulty);

            var ben = msg.SerializeToBen();
            var result = new ChainStatus(ben);

            Assert.Equal(result.GenesisHash, msg.GenesisHash);
            Assert.Equal(result.TipIndex, msg.TipIndex);
            Assert.Equal(result.TotalDifficulty, msg.TotalDifficulty);
        }
    }
}

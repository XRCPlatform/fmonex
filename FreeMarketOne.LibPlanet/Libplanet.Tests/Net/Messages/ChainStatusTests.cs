using Libplanet.Net.Messages;
using System;
using System.Numerics;
using System.Security.Cryptography;
using Xunit;


namespace Libplanet.Tests.Net.Messages
{
    //FMONECHANGE - added missing test
    public class ChainStatusTests
    {
        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            var random = new Random();
            var buffer = new byte[HashDigest<SHA256>.Size];
            random.NextBytes(buffer);
            var buffer2 = new byte[HashDigest<SHA256>.Size];
            random.NextBytes(buffer2);
            var genesis = new HashDigest<SHA256>(buffer);
            var tipHash = new HashDigest<SHA256>(buffer2);
            var difficulty = new BigInteger(buffer);
            ChainStatus msg = new ChainStatus(1, genesis, 123, tipHash, difficulty);

            Assert.Equal(genesis, msg.GenesisHash);
            Assert.Equal(123, msg.TipIndex);
            Assert.Equal(difficulty, msg.TotalDifficulty);

            var ben = msg.SerializeToBen();
            var result = new ChainStatus(ben);

            Assert.Equal(result.GenesisHash, msg.GenesisHash);
            Assert.Equal(result.TipIndex, msg.TipIndex);
            Assert.Equal(result.TipHash, msg.TipHash);
            Assert.Equal(result.ProtocolVersion, msg.ProtocolVersion);
            Assert.Equal(result.Timestamp, msg.Timestamp);
            Assert.Equal(result.TotalDifficulty, msg.TotalDifficulty);
        }
    }
}

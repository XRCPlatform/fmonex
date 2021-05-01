using Libplanet.Blocks;
using Libplanet.Net.Messages;
using System;
using System.Collections.Immutable;
using System.Security.Cryptography;
using Xunit;


namespace Libplanet.Tests.Net.Messages
{
    //FMONECHANGE - added missing test
    public class BlockHeaderMessageTests
    {
        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            var random = new Random();
            var buffer = new byte[HashDigest<SHA256>.Size];
            random.NextBytes(buffer);
            var genesis = new HashDigest<SHA256>(buffer);
            BlockHeader header = new BlockHeader(
                1,
                123,
                DateTime.UtcNow.ToString(),
                buffer.ToImmutableArray(),
                buffer.ToImmutableArray(),
                1000L,
                10000L,
                buffer.ToImmutableArray(),
                buffer.ToImmutableArray(),
                buffer.ToImmutableArray(),
                buffer.ToImmutableArray(),
                buffer.ToImmutableArray());
            var msg = new BlockHeaderMessage(genesis, header);
            Assert.Equal(genesis, msg.GenesisHash);
            Assert.Equal(header, msg.Header);

            var ben = msg.SerializeToBen();
            var result = new BlockHeaderMessage(ben);

            Assert.Equal(msg.GenesisHash, result.GenesisHash);
            Assert.Equal(msg.Header.Index, result.Header.Index);
            Assert.Equal(msg.Header.Difficulty, result.Header.Difficulty);
            Assert.Equal(msg.Header.TotalDifficulty, result.Header.TotalDifficulty);
            Assert.Equal(msg.Header.Timestamp, result.Header.Timestamp);
            Assert.Equal(msg.Header.ToBencodex(), result.Header.ToBencodex());
        }

       

    }
}

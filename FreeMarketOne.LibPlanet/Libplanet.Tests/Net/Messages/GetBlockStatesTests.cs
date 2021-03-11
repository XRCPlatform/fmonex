using Libplanet.Net.Messages;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Xunit;

namespace Libplanet.Tests.Net.Messages
{
    public class GetBlockStatesTests
    {
        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            
            var random = new Random();
            var buffer = new byte[HashDigest<SHA256>.Size];
            random.NextBytes(buffer);
            var hash = new HashDigest<SHA256>(buffer);

            var msg = new GetBlockStates(hash);

            Assert.Equal(hash, msg.BlockHash);
            
            var ben = msg.SerializeToBen();
            var result = new GetBlockStates(ben);

            Assert.Equal(msg.BlockHash, result.BlockHash);

        }

      
    }
}

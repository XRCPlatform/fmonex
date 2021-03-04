using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Net.Messages;
using Serilog;
using System;
using System.Collections.Immutable;
using System.Net;
using System.Security.Cryptography;
using Xunit;

namespace Libplanet.Tests.Net.Messages
{
    public class BlockStatesTest
    {
        [Fact]
        public void BlockStates_serialize_and_deserialize_withValueNull()
        {
            HashDigest<SHA256> blockHash = new Random().NextHashDigest<SHA256>();
            var blockStates = new BlockStates(blockHash, null);
            var bytes = blockStates.SerializeToBen();
            //call constructor directly
            var result = new BlockStates(bytes);
            Assert.Equal(blockStates.BlockHash, result.BlockHash);

        }

        [Fact]
        public void BlockStates_serialize_and_deserialize_WithNonEmpty()
        {
            // Note that here Unicode strings are used on purpose:
            IImmutableDictionary<string, IValue> states = ImmutableDictionary<string, IValue>.Empty
                .Add("foo甲", null)
                .Add("bar乙", default(Null))
                .Add("baz丙", new Text("a value 값"));

            HashDigest<SHA256> blockHash = new Random().NextHashDigest<SHA256>();
            var blockStates = new BlockStates(blockHash, states);
            var bytes = blockStates.SerializeToBen();

            //test that has bytes constructor
            var result = (BlockStates) Activator.CreateInstance(blockStates.GetType(), new[] { bytes });
            
            //call constructor directly
            var result2 = new BlockStates(bytes);

            Assert.Equal(blockStates.BlockHash, result.BlockHash);
            Assert.Equal(states, result.States);

            Assert.Equal(blockStates.BlockHash, result2.BlockHash);
            Assert.Equal(states, result2.States);
        }
     
    }
}

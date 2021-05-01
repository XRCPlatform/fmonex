using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Net.Messages;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Xunit;

namespace Libplanet.Tests.Net.Messages
{
	//FMONECHANGE added new test class to verify message serialziation handling
    public class RecentStatesTest
    {
        [Fact]
        public void Constructor()
        {
            var emptyBlockStates = ImmutableDictionary<
                HashDigest<SHA256>,
                IImmutableDictionary<string, IValue>
            >.Empty;
            Assert.Throws<ArgumentNullException>(() =>
                new RecentStates(
                    default,
                    default,
                    default,
                    null,
                    ImmutableDictionary<string, IImmutableList<HashDigest<SHA256>>>.Empty
                )
            );
            Assert.Throws<ArgumentNullException>(() =>
                new RecentStates(
                    default,
                    default,
                    default,
                    emptyBlockStates,
                    null
                )
            );
        }

        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            // This test lengthens long... Please read the brief description of the entire payload
            // structure from the comment in the RecentStates.DataFrames property code.
            ISet<Address> accounts = Enumerable.Repeat(0, 5).Select(_ =>
                new PrivateKey().ToAddress()
            ).ToHashSet();
            int accountsCount = accounts.Count;
            var privKey = new PrivateKey();

            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            var randomBytesBuffer = new byte[HashDigest<SHA256>.Size];
            (HashDigest<SHA256>, IImmutableDictionary<string, IValue>)[] blockStates =
                accounts.SelectMany(address =>
                {
                    rng.GetNonZeroBytes(randomBytesBuffer);
                    var blockHash1 = new HashDigest<SHA256>(randomBytesBuffer);
                    rng.GetNonZeroBytes(randomBytesBuffer);
                    var blockHash2 = new HashDigest<SHA256>(randomBytesBuffer);
                    IImmutableDictionary<string, IValue> emptyState =
                        ImmutableDictionary<string, IValue>.Empty;
                    return new[]
                    {
                        (
                            blockHash1,
                            emptyState.Add(
                                address.ToHex().ToLowerInvariant(),
                                (Text)$"A:{blockHash1}:{address}")
                        ),
                        (
                            blockHash2,
                            emptyState.Add(
                                address.ToHex().ToLowerInvariant(),
                                (Text)$"B:{blockHash2}:{address}")
                        ),
                    };
                }).ToArray();
            IImmutableDictionary<HashDigest<SHA256>, IImmutableDictionary<string, IValue>>
                compressedBlockStates = blockStates.Where(
                    (_, i) => i % 2 == 1
                ).ToImmutableDictionary(p => p.Item1, p => p.Item2);
            HashDigest<SHA256> blockHash = blockStates.Last().Item1;

            IImmutableDictionary<string, IImmutableList<HashDigest<SHA256>>> stateRefs =
                accounts.Select(a =>
                {
                    var states = blockStates
                        .Where(pair => pair.Item2.ContainsKey(a.ToHex().ToLowerInvariant()))
                        .Select(pair => pair.Item1)
                        .ToImmutableList();
                    return new KeyValuePair<string, IImmutableList<HashDigest<SHA256>>>(
                        a.ToHex().ToLowerInvariant(), states);
                }).ToImmutableDictionary();

            RecentStates original =
                new RecentStates(blockHash, -1, 1, compressedBlockStates, stateRefs);


            var ben = original.SerializeToBen();
            //var r1 = (RecentStates) original.FromBenBytes(ben);
            var r2 = new RecentStates(ben);

            Assert.Equal(original.BlockHash, r2.BlockHash);
            Assert.Equal(original.BlockStates, r2.BlockStates);
            Assert.Equal(original.Iteration, r2.Iteration);
            Assert.Equal(original.Offset, r2.Offset);
            Assert.Equal(original.Missing, r2.Missing);


            //RecentStates missing = new RecentStates(blockHash, -1, 1, null, null);
            //msg = missing.ToNetMQMessage(privKey, peer, version);
            //Assert.Equal(blockHash, new HashDigest<SHA256>(msg[headerSize].Buffer));
            //Assert.Equal(-1, msg[stateRefsOffset].ConvertToInt32());

            //Assert.Equal(blockHash, parsed.BlockHash);
            //Assert.True(parsed.Missing);
        }    
    }
}

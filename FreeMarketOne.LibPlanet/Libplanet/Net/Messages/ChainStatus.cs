using Bencodex;
using Bencodex.Types;
using System.Collections.Immutable;
using System.Numerics;
using System.Security.Cryptography;

namespace Libplanet.Net.Messages
{
    public class ChainStatus : IBenEncodeable
    {
        private static readonly byte[] GenesisHashKey = { 0x47 }; // 'G'
        private static readonly byte[] TipIndexKey = { 0x54 }; // 'T'
        private static readonly byte[] TotalDifficultyKey = { 0x44 }; // 'D

        public ChainStatus(HashDigest<SHA256> genesisHash, long tipIndex, BigInteger totalDifficulty)
        {
            GenesisHash = genesisHash;
            TipIndex = tipIndex;
            TotalDifficulty = totalDifficulty;
        }

        public ChainStatus(Bencodex.Types.Dictionary dict)
        {
            GenesisHash = new HashDigest<SHA256>(dict.GetValue<Binary>(GenesisHashKey).ToImmutableArray());
            TipIndex = dict.GetValue<Integer>(TipIndexKey);
            //TotalDifficulty = new BigInteger(dict.GetValue<Binary>(TotalDifficultyKey));
            TotalDifficulty = dict.GetValue<Integer>(TotalDifficultyKey);
        }

        public ChainStatus(byte[] bytes) : this(DecodeBytesToBen(bytes))
        {

        }

        public static BlockStates Deserialize(byte[] bytes)
        {
            return new BlockStates(DecodeBytesToBen(bytes));
        }

        private static Bencodex.Types.Dictionary DecodeBytesToBen(byte[] bytes)
        {
            IValue value = new Codec().Decode(bytes);
            if (!(value is Dictionary dict))
            {
                throw new DecodingException(
                    $"Expected {typeof(Dictionary)} but " +
                    $"{value.GetType()}");
            }
            return dict;
        }

        public byte[] SerializeToBen()
        {
            return new Codec().Encode(ToBencodex());
        }

        public Dictionary ToBencodex()
        {
            var dict = Dictionary.Empty
                .Add(GenesisHashKey, GenesisHash.ToByteArray())
                .Add(TipIndexKey,TipIndex)
                .Add(TotalDifficultyKey, (IValue)(Bencodex.Types.Integer)TotalDifficulty);
            return dict;
        }

        public object FromBenBytes(byte[] bytes)
        {
            return Deserialize(bytes);
        }

        public HashDigest<SHA256> GenesisHash { get; set; }

        public long TipIndex { get; set; }

        public BigInteger TotalDifficulty { get; set; }
    }
}

using Bencodex;
using Bencodex.Types;
using System.Collections.Immutable;
using System.Numerics;
using System.Security.Cryptography;
using Libplanet.Blocks;
using System;

namespace Libplanet.Net.Messages
{
    //FMONECHANGE -  changed message serialization from NetMQMessage to Bencoded message
    public class ChainStatus : IBenEncodeable, IBlockExcerpt
    {
        private static readonly byte[] GenesisHashKey = { 0x47 }; // 'G'
        private static readonly byte[] TipIndexKey = { 0x54 }; // 'T'
        private static readonly byte[] TotalDifficultyKey = { 0x44 }; // 'D
        private static readonly byte[] ProtocolVersionKey = { 0x56 }; // 'v
        private static readonly byte[] TipHashKey = { 0x48 }; // 'h
        private static readonly byte[] TimeStampKey = { 0x53 }; // 's

        public ChainStatus(
           int protocolVersion,
           HashDigest<SHA256> genesisHash,
           long tipIndex,
           HashDigest<SHA256> tipHash,
           BigInteger totalDifficulty)
        {
            ProtocolVersion = protocolVersion;
            GenesisHash = genesisHash;
            TipIndex = tipIndex;
            TipHash = tipHash;
            TotalDifficulty = totalDifficulty;
            Timestamp = DateTimeOffset.Now;
        }

        public ChainStatus(Bencodex.Types.Dictionary dict)
        {
            ProtocolVersion = dict.GetValue<Integer>(ProtocolVersionKey);
            GenesisHash = new HashDigest<SHA256>(dict.GetValue<Binary>(GenesisHashKey).ToImmutableArray());
            TipIndex = dict.GetValue<Integer>(TipIndexKey);
            TipHash = new HashDigest<SHA256>(dict.GetValue<Binary>(TipHashKey).ToImmutableArray());
            TotalDifficulty = dict.GetValue<Integer>(TotalDifficultyKey);
            Timestamp = new DateTimeOffset(dict.GetValue<Integer>(TimeStampKey), TimeSpan.Zero);
        }

        public int ProtocolVersion { get; }
        public HashDigest<SHA256> GenesisHash { get; set; }

        public long TipIndex { get; set; }

        public HashDigest<SHA256> TipHash { get; }
        public BigInteger TotalDifficulty { get; set; }
		/// <summary>
        /// The timestamp of the message is created.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        long IBlockExcerpt.Index => TipIndex;

        HashDigest<SHA256> IBlockExcerpt.Hash => TipHash;


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
                .Add(TipHashKey, TipHash.ToByteArray())
                .Add(TipIndexKey, (IValue)(Bencodex.Types.Integer)TipIndex)
                .Add(ProtocolVersionKey, (IValue)(Bencodex.Types.Integer)ProtocolVersion)
                .Add(TotalDifficultyKey, (IValue)(Bencodex.Types.Integer)TotalDifficulty)
                .Add(TimeStampKey, (IValue)(Bencodex.Types.Integer)Timestamp.Ticks);
            return dict;
        }

        public object FromBenBytes(byte[] bytes)
        {
            return Deserialize(bytes);
        }


    }
}

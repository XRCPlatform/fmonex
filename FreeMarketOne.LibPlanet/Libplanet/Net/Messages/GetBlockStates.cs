using Bencodex;
using Bencodex.Types;
using System.Collections.Immutable;
using System.Security.Cryptography;


namespace Libplanet.Net.Messages
{
    public class GetBlockStates : IBenEncodeable
    {
        private static readonly byte[] BlockHeaderKey = { 0x42 };    // 'B'
        public GetBlockStates(HashDigest<SHA256> blockHash)
        {
            BlockHash = blockHash;
        }

        public GetBlockStates(byte[] bytes) : this(DecodeBytesToBen(bytes))
        {
        }

        public GetBlockStates(Dictionary dict)
        {
            BlockHash = new HashDigest<SHA256>(dict.ContainsKey((IKey)(Binary)BlockHeaderKey)
            ? dict.GetValue<Binary>(BlockHeaderKey).ToImmutableArray()
            : ImmutableArray<byte>.Empty);
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

        public HashDigest<SHA256> BlockHash { get; set; }

        public Dictionary ToBencodex()
        {
            var dict = Dictionary.Empty
                .Add(BlockHeaderKey, BlockHash.ToByteArray());
            return dict;
        }

        public static GetBlockStates Deserialize(byte[] bytes)
        {
            return new GetBlockStates(DecodeBytesToBen(bytes));
        }
        public byte[] SerializeToBen()
        {
            return new Codec().Encode(ToBencodex());
        }
        public object FromBenBytes(byte[] bytes)
        {
            return Deserialize(bytes);
        }
    }
}

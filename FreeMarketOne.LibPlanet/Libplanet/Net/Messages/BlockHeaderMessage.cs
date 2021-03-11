using Bencodex;
using Bencodex.Types;
using Libplanet.Blocks;
using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Libplanet.Net.Messages
{
    internal class BlockHeaderMessage : IBenEncodeable
    {
        private static readonly byte[] BlockHeaderKey = { 0x42 };    // 'B'
        private static readonly byte[] GenesisHashKey = { 0x47 };    // 'G'
        public BlockHeaderMessage(HashDigest<SHA256> genesisHash, BlockHeader header)
        {
            GenesisHash = genesisHash;
            Header = header;
        }

        public BlockHeaderMessage(byte[] bytes) : this(DecodeBytesToBen(bytes))
        {
        }

        public BlockHeaderMessage(Dictionary dict)
        {
            if (dict.ContainsKey((IKey)(Binary)BlockHeaderKey))
            {
                Header = new BlockHeader(dict.GetValue<Bencodex.Types.Dictionary>(BlockHeaderKey));
            }

            GenesisHash = new HashDigest<SHA256>(dict.ContainsKey((IKey)(Binary)GenesisHashKey)
              ? dict.GetValue<Binary>(GenesisHashKey).ToImmutableArray()
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

        public HashDigest<SHA256> GenesisHash { get; set; }

        public BlockHeader Header { get; set; }

        public byte[] SerializeToBen()
        {
            return new Codec().Encode(ToBencodex());
        }


        public Dictionary ToBencodex()
        {
            var dict = Dictionary.Empty
                .Add(GenesisHashKey, GenesisHash.ToByteArray());
            dict = dict.Add(BlockHeaderKey, Header.ToBencodex());
            return dict;
        }

        public static BlockHeader Deserialize(byte[] bytes)
        {
            return new BlockHeader(DecodeBytesToBen(bytes));
        }

        public object FromBenBytes(byte[] bytes)
        {
            return Deserialize(bytes);
        }
    }
}

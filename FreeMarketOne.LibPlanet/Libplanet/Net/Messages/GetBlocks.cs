using Bencodex;
using Bencodex.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Libplanet.Net.Messages
{
    public class GetBlocks : IBenEncodeable
    {
        private static readonly byte[] ChunkSizeKey = { 0x53 }; // 'S'
        private static readonly byte[] BlockHashesKey = { 0x42 }; // 'B'
        public GetBlocks(IEnumerable<HashDigest<SHA256>> hashes, int chunkSize = 100)
        {
            if (chunkSize <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(chunkSize),
                    "Chunk size must be greater than 0.");
            }

            BlockHashes = hashes;
            ChunkSize = chunkSize;
        }
        public GetBlocks(byte[] bytes) : this(DecodeBytesToBen(bytes))
        {
        }

       
        public GetBlocks(Bencodex.Types.Dictionary dict)
        {
            if (dict.ContainsKey((IKey)(Binary)ChunkSizeKey))
            {
                ChunkSize = dict.GetValue<Integer>(ChunkSizeKey);
            }

            var temp = new List<HashDigest<SHA256>>();
            if (dict.ContainsKey((IKey)(Binary)BlockHashesKey))
            {
                var list = dict.GetValue<Bencodex.Types.List>(BlockHashesKey)
                    .Select(hash => ((Binary)hash).Value);
                foreach (var item in list)
                {
                    temp.Add(new HashDigest<SHA256>(item));
                }
            }

            BlockHashes = temp;
        }

       
        public IEnumerable<HashDigest<SHA256>> BlockHashes { get; set; }

        public int ChunkSize { get; set; }

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


        public static GetBlocks Deserialize(byte[] bytes)
        {
            return new GetBlocks(DecodeBytesToBen(bytes));
        }

        public object FromBenBytes(byte[] bytes)
        {
            return Deserialize(bytes);
        }

        /// <summary>
        /// Gets serialized byte array of the <see cref="BlockStates"/>.
        /// </summary>
        /// <returns>Serialized byte array of <see cref="BlockStates"/>.</returns>
        public byte[] SerializeToBen()
        {
            return new Codec().Encode(ToBencodex());
        }

        /// <summary>
        /// Gets <see cref="Bencodex.Types.Dictionary"/> representation of
        /// <see cref="BlockStates"/>.
        /// </summary>
        /// <returns><see cref="Bencodex.Types.Dictionary"/> representation of
        /// <see cref="BlockStates"/>.</returns>
        public Dictionary ToBencodex()
        {
            var dict = Dictionary.Empty;
            if (ChunkSize is int offset)
            {
                dict = dict.Add(ChunkSizeKey, offset);
            }
            if (BlockHashes.Any())
            {
                dict = dict.Add(
                    BlockHashesKey,
                    BlockHashes.Select(hash => (IValue)(Binary)hash.ToByteArray()));
            }
            return dict;
        }

    }
}

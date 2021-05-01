using Bencodex;
using Bencodex.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;


namespace Libplanet.Net.Messages
{
	//FMONECHANGE -  changed message serialization from NetMQMessage to Bencoded message
    public class BlockHashes : IBenEncodeable
    {
        private static readonly byte[] StartIndexKey = { 0x53 }; // 'S'
        private static readonly byte[] BlockHashesKey = { 0x42 }; // 'B'

        public BlockHashes(byte[] bytes) : this(DecodeBytesToBen(bytes))
        {
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

        public BlockHashes(long? startIndex, IEnumerable<HashDigest<SHA256>> hashes)
        {
            StartIndex = startIndex;
            Hashes = hashes;

            if (StartIndex is null && Hashes.Any())
            {
                throw new ArgumentNullException(
                    nameof(startIndex),
                    "The startIndex can be null iff hashes are empty."
                );
            }
            else if (!Hashes.Any() && !(StartIndex is null))
            {
                throw new ArgumentException(
                    "The startIndex has to be null if hashes are empty.",
                    nameof(startIndex)
                );
            }
        }

        public BlockHashes(Bencodex.Types.Dictionary dict)
        {
            if (dict.ContainsKey((IKey)(Binary)StartIndexKey))
            {
                StartIndex = dict.GetValue<Integer>(StartIndexKey);
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

            Hashes = temp;
        }

        /// <summary>
        /// The block index of the first hash in <see cref="Hashes"/>.
        /// It is <c>null</c> iff <see cref="Hashes"/> are empty.
        /// </summary>
        public long? StartIndex { get; set; }

        /// <summary>
        /// The continuous block hashes, from the lowest index to the highest index.
        /// </summary>
        public IEnumerable<HashDigest<SHA256>> Hashes { get; set; }
      
        /// <summary>
        /// Gets <see cref="BlockHashes"/> instance from serialized <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">Serialized <see cref="BlockHashes"/>.</param>
        /// <returns>Deserialized <see cref="BlockHashes"/>.</returns>
        /// <exception cref="DecodingException">Thrown when decoded value is not
        /// <see cref="Bencodex.Types.Dictionary"/> type.</exception>
        public static BlockHashes Deserialize(byte[] bytes)
        {
            return new BlockHashes(DecodeBytesToBen(bytes));
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
            if (StartIndex is long offset)
            {
                dict = dict.Add(StartIndexKey, offset);
                if (Hashes.Any())
                {
                    dict = dict.Add(
                        BlockHashesKey,
                        Hashes.Select(hash => (IValue)(Binary)hash.ToByteArray()));
                }
            }
            return dict;
        }
    }
}

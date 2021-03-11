using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bencodex;
using Bencodex.Types;

namespace Libplanet.Net.Messages
{
    public class BlockStates: IBenEncodeable
    {
        private static readonly byte[] BlockHashKey = { 0x42 }; // 'B'
        private static readonly byte[] StatesKey = { 0x53 }; // 'S'

        public BlockStates(byte[] bytes) : this(DecodeBytesToBen(bytes))
        {
        }

        public BlockStates(HashDigest<SHA256> blockHash, IImmutableDictionary<string, IValue> states)
        {
            BlockHash = blockHash;
            States = states;
        }

        /// <summary>
        /// Creates <see cref="BlockStates"/> instance from
        /// <see cref="Bencodex.Types.Dictionary"/> representation of the <see cref="BlockStates"/>.
        /// </summary>
        /// <param name="dict">
        /// <see cref="Bencodex.Types.Dictionary"/> representation of the <see cref="BlockStates"/>.
        /// </param>
        public BlockStates(Bencodex.Types.Dictionary dict)
        {
            BlockHash = new HashDigest<SHA256>(dict.ContainsKey((IKey)(Binary)BlockHashKey)
              ? dict.GetValue<Binary>(BlockHashKey).ToImmutableArray()
              : ImmutableArray<byte>.Empty);

            var states = new Dictionary<string, IValue>();
            if (dict.ContainsKey((IKey)(Binary)StatesKey))
            {
                var dic = dict.GetValue<Bencodex.Types.Dictionary>((Binary)StatesKey);
                foreach (var kv in dic)
                {
                    if (kv.Value == null || kv.Value.Equals(new byte[0]))
                    {
                        states.Add(Encoding.UTF8.GetString(kv.Key.EncodeAsByteArray()), null as IValue);
                    }
                    else
                    {
                        states.Add(Encoding.UTF8.GetString(kv.Key.EncodeAsByteArray()), kv.Value); 
                    }
                    
                }
            }

            States = states.ToImmutableDictionary();
           
        }

        public HashDigest<SHA256> BlockHash { get; set; }

        public IImmutableDictionary<string, IValue> States { get; set; }

        /// <summary>
        /// Gets <see cref="BlockStates"/> instance from serialized <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">Serialized <see cref="BlockStates"/>.</param>
        /// <returns>Deserialized <see cref="BlockStates"/>.</returns>
        /// <exception cref="DecodingException">Thrown when decoded value is not
        /// <see cref="Bencodex.Types.Dictionary"/> type.</exception>
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
            var dict = Dictionary.Empty
                .Add(BlockHashKey, BlockHash.ToByteArray());

            if (States != null && States.Any())
            {
                var dictStates = new Bencodex.Types.Dictionary();
                foreach (KeyValuePair<string, IValue> kv in States)
                {
                    if (kv.Value != null)
                    {
                        dictStates = dictStates.Add(kv.Key, kv.Value);
                    }
                    else
                    {
                        dictStates = dictStates.Add(kv.Key, new byte[0]);
                    }
                }
                dict = dict.Add(StatesKey, dictStates);
            }

            return dict;
        }


        public object FromBenBytes(byte[] bytes)
        {
            return Deserialize(bytes);
        }
    }
}

using Bencodex;
using Bencodex.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Libplanet.Net.Messages
{
    internal class FindNeighbors : IBenEncodeable
    {
        private static readonly byte[] StartIndexKey = { 0x53 }; // 'S'
        private static readonly byte[] BlockHashesKey = { 0x42 }; // 'B'

        public FindNeighbors(byte[] bytes) : this(DecodeBytesToBen(bytes))
        {
        }

        public FindNeighbors(Bencodex.Types.Dictionary dict)
        {
            if (dict.ContainsKey((IKey)(Binary)StartIndexKey))
            {
                Target = new Address(dict.GetValue<Binary>(StartIndexKey));
            }

        }

        // TODO: This message may request exact peer instead of its neighbors.
        public FindNeighbors(Address target)
        {
            Target = target;
        }

        public Address Target { get; set; }

        public static FindNeighbors Deserialize(byte[] bytes)
        {
            return new FindNeighbors(DecodeBytesToBen(bytes));
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
            var dict = Dictionary.Empty
                .Add(StartIndexKey, Target.ToByteArray());
            return dict;
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
    }
}

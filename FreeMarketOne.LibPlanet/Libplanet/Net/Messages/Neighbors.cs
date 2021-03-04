using Bencodex;
using Bencodex.Types;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Libplanet.Net.Messages
{
    internal class Neighbors : IBenEncodeable
    {
        private static readonly byte[] FoundKey = { 0x46 };    // 'F'

        public Neighbors(IEnumerable<BoundPeer> found)
        {
            Found = found.ToImmutableList();
        }

        public Neighbors(byte[] bytes) : this(DecodeBytesToBen(bytes))
        {
        }

        //public Neighbors(NetMQFrame[] body)
        //{
        //    int foundCount = body[0].ConvertToInt32();
        //    Found = body.Skip(1).Take(foundCount)
        //        .Select(f => DeserializePeer(f.ToByteArray()) as BoundPeer)
        //        .ToImmutableList();
        //}

        //protected override IEnumerable<NetMQFrame> DataFrames
        //{
        //    get
        //    {
        //        yield return new NetMQFrame(
        //            NetworkOrderBitsConverter.GetBytes(Found.Count));

        //        foreach (BoundPeer peer in Found)
        //        {
        //            yield return new NetMQFrame(SerializePeer(peer));
        //        }
        //    }
        //}

        public IImmutableList<BoundPeer> Found { get; }


        public Neighbors(Dictionary dict)
        {
            var temp = new List<BoundPeer>();
            if (dict.ContainsKey((IKey)(Binary)FoundKey))
            {
                var list = dict.GetValue<Bencodex.Types.List>(FoundKey)
                    .Select(peer => ((Binary)peer).Value);
                foreach (var item in list)
                {
                    temp.Add(BoundPeer.DeserializePeer(item));
                }
            }

            Found = temp.ToImmutableList();
        }

        public Dictionary ToBencodex()
        {
            var dict = Dictionary.Empty;
            if (Found.Any())
            {
                dict = dict.Add(
                    FoundKey,
                    Found.Select(peer => (IValue)(Binary)BoundPeer.SerializePeer(peer)));
            }
            return dict;
        }

        public byte[] SerializeToBen()
        {
            return new Codec().Encode(ToBencodex());
        }

        public object FromBenBytes(byte[] bytes)
        {
            return new Neighbors(DecodeBytesToBen(bytes));
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

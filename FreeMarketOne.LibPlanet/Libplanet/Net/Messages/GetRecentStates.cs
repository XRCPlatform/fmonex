using Bencodex;
using Bencodex.Types;
using Libplanet.Blockchain;
using System.Linq;
using System.Security.Cryptography;

namespace Libplanet.Net.Messages
{
    public class GetRecentStates : IBenEncodeable
    {
        private static readonly byte[] BaseLocatorKey = { 0x47 }; // 'G'
        private static readonly byte[] OffsetKey = { 0x54 }; // 'T'
        private static readonly byte[] TargetHashKey = { 0x44 }; // 'D

        public GetRecentStates(BlockLocator baseLocator, HashDigest<SHA256> target, long offset)
        {
            BaseLocator = baseLocator;
            TargetBlockHash = target;
            Offset = offset;
        }

        public GetRecentStates(byte[] bytes) : this(DecodeBytesToBen(bytes))
        {

        }
        public GetRecentStates(Dictionary dict)
        {
            var list = dict.GetValue<List>(BaseLocatorKey).Select(f => new HashDigest<SHA256>((Binary)f));
            BaseLocator = new BlockLocator(list);
            Offset = dict.GetValue<Integer>(OffsetKey);
            TargetBlockHash = new HashDigest<SHA256>(dict.GetValue<Binary>(TargetHashKey));
        }
                   
        public Dictionary ToBencodex()
        {
            var dict = Dictionary.Empty
                .Add(OffsetKey, Offset)
                .Add(TargetHashKey, TargetBlockHash.ToByteArray());
            if (BaseLocator.Any())
            {
                dict = dict.Add(
                    BaseLocatorKey,
                    BaseLocator.Select(b => (IValue)(Binary)b.ToByteArray()));
            }            
            return dict;
        }

        public BlockLocator BaseLocator { get; set; }

        public HashDigest<SHA256> TargetBlockHash { get; set; }

        public long Offset { get; set; }


        public static GetRecentStates Deserialize(byte[] bytes)
        {
            return new GetRecentStates(DecodeBytesToBen(bytes));
        }

        public object FromBenBytes(byte[] bytes)
        {
            return Deserialize(bytes);
        }

        public byte[] SerializeToBen()
        {
            return new Codec().Encode(ToBencodex());
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

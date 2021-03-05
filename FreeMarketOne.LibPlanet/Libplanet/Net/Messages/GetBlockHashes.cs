using Bencodex;
using Bencodex.Types;
using Libplanet.Blockchain;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Libplanet.Net.Messages
{
    public class GetBlockHashes : IBenEncodeable
    {
        private static readonly byte[] LocatorKey = { 0x47 }; // 'G'
        private static readonly byte[] StopKey = { 0x54 }; // 'T'
        
 
        public GetBlockHashes(BlockLocator locator, HashDigest<SHA256>? stop)
        {
            Locator = locator;
            Stop = stop;
        }

        public GetBlockHashes(byte[] bytes) : this(DecodeBytesToBen(bytes))
        {
        }


        public GetBlockHashes(Bencodex.Types.Dictionary dict)
        {
            if (dict.ContainsKey((IKey)(Binary)StopKey))
            {
                Stop = new HashDigest<SHA256>(dict.GetValue<Binary>(StopKey));
            }

            var temp = new List<HashDigest<SHA256>>();
            if (dict.ContainsKey((IKey)(Binary)LocatorKey))
            {
                var list = dict.GetValue<Bencodex.Types.List>(LocatorKey)
                    .Select(hash => ((Binary)hash).Value);
                foreach (var item in list)
                {
                    temp.Add(new HashDigest<SHA256>(item));
                }
            }

            Locator = new BlockLocator(temp);
        }

        public BlockLocator Locator { get; set; }

        public HashDigest<SHA256>? Stop { get; set; }

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

        public static GetBlockHashes Deserialize(byte[] bytes)
        {
            return new GetBlockHashes(DecodeBytesToBen(bytes));
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
            if (Stop.HasValue)
            {
                dict = dict.Add(StopKey, Stop.Value.ToByteArray());
            }
            if (Locator.Any())
            {
                dict = dict.Add(
                    LocatorKey,
                    Locator.Select(hash => (IValue)(Binary)hash.ToByteArray()));
            }
            return dict;
        }

        //public GetBlockHashes(NetMQFrame[] frames)
        //{
        //    int requestedHashCount = frames[0].ConvertToInt32();
        //    Locator = new BlockLocator(
        //        frames.Skip(1).Take(requestedHashCount)
        //        .Select(f => f.ConvertToHashDigest<SHA256>()));
        //    Stop = frames[1 + requestedHashCount].IsEmpty
        //        ? default(HashDigest<SHA256>?)
        //        : frames[1 + requestedHashCount].ConvertToHashDigest<SHA256>();
        //}

        //protected override IEnumerable<NetMQFrame> DataFrames
        //{
        //    get
        //    {
        //        yield return new NetMQFrame(
        //            NetworkOrderBitsConverter.GetBytes(Locator.Count()));

        //        foreach (HashDigest<SHA256> hash in Locator)
        //        {
        //            yield return new NetMQFrame(hash.ToByteArray());
        //        }

        //        if (Stop is HashDigest<SHA256> stop)
        //        {
        //            yield return new NetMQFrame(stop.ToByteArray());
        //        }
        //        else
        //        {
        //            yield return NetMQFrame.Empty;
        //        }
        //    }
        //}


        

       
    }
}

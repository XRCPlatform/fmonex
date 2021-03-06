using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Bencodex;
using Bencodex.Types;

namespace Libplanet.Net.Messages
{
	//FMONECHANGE -  changed message serialization from NetMQMessage to Bencoded message
    public class Blocks : IBenEncodeable
    {
        private static readonly byte[] GenesisKey = { 0x47 };   // 'G'
        private static readonly byte[] BlocksKey = { 0x42 };    // 'B'

        public Blocks(IEnumerable<byte[]> payloads, HashDigest<SHA256> genesisHash)
        {
            GenesisHash = genesisHash;
            if (payloads.Count() > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(payloads),
                    $"The number of payloads can't exceed {int.MaxValue}.");
            }

            Payloads = payloads.ToList();
        }

        public Blocks(byte[] bytes) : this(DecodeBytesToBen(bytes))
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
		//FMONECHANGE -  added genesis attribute for extra validation
        public HashDigest<SHA256> GenesisHash { get; }

        public Blocks(Dictionary dict)
        {
            if (dict.ContainsKey((IKey)(Binary)GenesisKey))
            {
                GenesisHash = new HashDigest<SHA256>(dict.GetValue<Binary>(GenesisKey));
            }

            var temp = new List<byte[]>();
            if (dict.ContainsKey((IKey)(Binary)BlocksKey))
            {
                Payloads = dict.GetValue<Bencodex.Types.List>(BlocksKey)
                    .Select(hash => ((Binary)hash).Value);                
            }
        }

        public IEnumerable<byte[]> Payloads { get; set; }


        public byte[] SerializeToBen()
        {
            return new Codec().Encode(ToBencodex());
        }

        public object FromBenBytes(byte[] bytes)
        {
            return new Blocks(DecodeBytesToBen(bytes));
        }
        
        public Dictionary ToBencodex()
        {
            var dict = Dictionary.Empty;
            dict = dict.Add(GenesisKey, GenesisHash.ToByteArray());
            if (Payloads.Any())
            {
                dict = dict.Add(
                    BlocksKey,
                    Payloads.Select(b => (IValue)(Binary)b));
            }
            return dict;
        }
        
    }
}

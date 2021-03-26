using Bencodex;
using Bencodex.Types;
using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Libplanet.Net.Messages
{
    public class ChatItem: IBenEncodeable
    {
        [JsonProperty("m")]
        public string Message { get; set; }

        [JsonProperty("x")]
        public string ExtraMessage { get; set; }

        [JsonProperty("d")]
        public DateTime DateCreated { get; set; }

        [JsonProperty("t")]
        public int Type { get; set; }
        
        [JsonProperty("h")]
        public string MarketItemHash { get; set; }

        [JsonProperty("p")]
        public bool Propagated { get; set; }

        [JsonProperty("s")]
        public HashDigest<SHA256> Digest { get; set; }

        private static readonly byte[] MessageKey = { 0x41 }; 
        private static readonly byte[] ExtraMessageKey = { 0x42 }; 
        private static readonly byte[] DateCreatedKey = { 0x43 }; 
        private static readonly byte[] TypeKey = { 0x44 };
        private static readonly byte[] MarketItemHashKey = { 0x45 };
        private static readonly byte[] PropagatedKey = { 0x46 };
        private static readonly byte[] DigestKey = { 0x47 };
        public ChatItem()
        {

        }

        public ChatItem(byte[] bytes) : this(DecodeBytesToBen(bytes))
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

        public ChatItem(Bencodex.Types.Dictionary dict)
        {
            if (dict.ContainsKey((IKey)(Binary)MessageKey))
            {
                Message = Encoding.UTF8.GetString(dict.GetValue<Binary>(MessageKey));
            }
            if (dict.ContainsKey((IKey)(Binary)ExtraMessageKey))
            {
                ExtraMessage = Encoding.UTF8.GetString(dict.GetValue<Binary>(ExtraMessageKey));
            }
            if (dict.ContainsKey((IKey)(Binary)DateCreatedKey))
            {
                DateCreated = DateTime.FromBinary(dict.GetValue<Integer>(DateCreatedKey));
            }
            if (dict.ContainsKey((IKey)(Binary)TypeKey))
            {
                Type = dict.GetValue<Integer>(TypeKey);
            }
            if (dict.ContainsKey((IKey)(Binary)MarketItemHashKey))
            {
                MarketItemHash = Encoding.UTF8.GetString(dict.GetValue<Binary>(MarketItemHashKey));
            }
            if (dict.ContainsKey((IKey)(Binary)PropagatedKey))
            {
                Propagated = (dict.GetValue<Integer>(PropagatedKey) == (Integer)1) ? true : false;
            }
            if (dict.ContainsKey((IKey)(Binary)DigestKey))
            {
                Digest = new HashDigest<SHA256>(dict.GetValue<Binary>(DigestKey));
            }            
        }


        public static ChatItem Deserialize(byte[] bytes)
        {
            return new ChatItem(DecodeBytesToBen(bytes));
        }

        public object FromBenBytes(byte[] bytes)
        {
            return Deserialize(bytes);
        }


        public byte[] SerializeToBen()
        {
            return new Codec().Encode(ToBencodex());
        }

        public Dictionary ToBencodex()
        {
            var dict = Dictionary.Empty;
            dict = dict.Add(MessageKey, (IValue)(Binary)Encoding.UTF8.GetBytes(Message));
            dict = dict.Add(ExtraMessageKey, (IValue)(Binary)Encoding.UTF8.GetBytes(ExtraMessage));
            dict = dict.Add(DateCreatedKey, (IValue)(Integer)DateCreated.ToBinary());
            dict = dict.Add(TypeKey, (IValue)(Integer)Type);
            dict = dict.Add(MarketItemHashKey, (IValue)(Binary)Encoding.UTF8.GetBytes(MarketItemHash));
            int prop = Propagated ? 1 : 0;
            dict = dict.Add(PropagatedKey, (IValue)(Integer)prop);
            dict = dict.Add(DigestKey, (IValue)(Binary)Digest.ToByteArray());          
            return dict;
        }
    }
}

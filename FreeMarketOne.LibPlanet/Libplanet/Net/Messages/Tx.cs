using Bencodex;
using Bencodex.Types;

namespace Libplanet.Net.Messages
{
	//FMONECHANGE -  changed message serialization from NetMQMessage to Bencoded message
    public class Tx : IBenEncodeable
    {
        private static readonly byte[] key = { 0x47 };   // 'G'

        /// <summary>
        /// This constuctor will be called by BEN deserilizer who only has 1 thing 
        /// </summary>
        /// <param name="payload"></param>
        public Tx(byte[] payload):this (payload,true)
        {

        }
        /// <summary>
        /// This could be called by deserializer and normal constructor, one is just binary another ben binary
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="deserializingBen"></param>
        public Tx(byte[] payload, bool deserializingBen = true)
        {
            if (!deserializingBen)
            {
                Payload = payload;
            }
            else
            {
                var dict = DecodeBytesToBen(payload);
                if (dict.ContainsKey((IKey)(Binary)key))
                {
                    Payload = dict.GetValue<Binary>(key);
                }
            }
            
        }

        public byte[] Payload { get; set; }

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

        public Tx(Dictionary dict)
        {
            if (dict.ContainsKey((IKey)(Binary)key))
            {
                Payload = dict.GetValue<Binary>(key);
            }
        }
        
        public byte[] SerializeToBen()
        {
            return new Codec().Encode(ToBencodex());
        }

        public object FromBenBytes(byte[] bytes)
        {
            return new Tx(DecodeBytesToBen(bytes));
        }

        public Dictionary ToBencodex()
        {
            var dict = Dictionary.Empty;
            dict = dict.Add(key, Payload);
            return dict;
        }
    }
}

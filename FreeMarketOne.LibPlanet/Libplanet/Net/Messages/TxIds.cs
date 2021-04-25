using Bencodex;
using Bencodex.Types;
using Libplanet.Tx;
using System.Collections.Generic;
using System.Linq;

namespace Libplanet.Net.Messages
{
	//FMONECHANGE -  changed message serialization from NetMQMessage to Bencoded message
    public class TxIds : IBenEncodeable
    {
        private static readonly byte[] senderKey = { 0x49 }; // 'I'
        private static readonly byte[] idsKey = { 0x43 };    // 'S'

        public TxIds(Address sender, IEnumerable<TxId> txIds)
        {
            Sender = sender;
            Ids = txIds;
        }
        public TxIds(byte[] bytes) : this(DecodeBytesToBen(bytes))
        {
        }

        public TxIds(Dictionary dict)
        {
            
            if (dict.ContainsKey((IKey)(Binary)senderKey))
            {
                Sender = new Address(dict.GetValue<Binary>(senderKey));
            }

            var temp = new List<TxId>();
            if (dict.ContainsKey((IKey)(Binary)idsKey))
            {
                var list = dict.GetValue<Bencodex.Types.List>(idsKey)
                    .Select(hash => ((Binary)hash).Value);
                foreach (var item in list)
                {
                    temp.Add(new TxId(item));
                }
            }

            Ids = temp;
        }

        public Dictionary ToBencodex()
        {
            var dict = Dictionary.Empty;
            dict = dict.Add(senderKey, Sender.ToByteArray());
            if (Ids.Any())
            {
                dict = dict.Add(
                    idsKey,
                    Ids.Select(txid => (IValue)(Binary)txid.ToByteArray()));
            }
            return dict;
        }


        public IEnumerable<TxId> Ids { get; }

        public Address Sender { get; }

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

        public byte[] SerializeToBen()
        {
            return new Codec().Encode(ToBencodex());
        }

        public object FromBenBytes(byte[] bytes)
        {
            return new TxIds(DecodeBytesToBen(bytes));
        }

        public static TxIds Deserialize(byte[] bytes)
        {
            return new TxIds(DecodeBytesToBen(bytes));
        }
    }
}

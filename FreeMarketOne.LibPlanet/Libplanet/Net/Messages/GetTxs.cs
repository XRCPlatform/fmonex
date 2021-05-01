using Bencodex;
using Bencodex.Types;
using Libplanet.Tx;
using System.Collections.Generic;
using System.Linq;

namespace Libplanet.Net.Messages
{
	//FMONECHANGE -  changed message serialization from NetMQMessage to Bencoded message
    internal class GetTxs : IBenEncodeable
    {
        private static readonly byte[] idsKey = { 0x47 };   // 'G'
        public GetTxs(IEnumerable<TxId> txIds)
        {
            TxIds = txIds;
        }
        public GetTxs(byte[] bytes) : this(DecodeBytesToBen(bytes))
        {

        }
        public GetTxs(Dictionary dict)
        {
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

            TxIds = temp;
        }

        public IEnumerable<TxId> TxIds { get; set; }

        public static GetTxs Deserialize(byte[] bytes)
        {
            return new GetTxs(DecodeBytesToBen(bytes));
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

            if (TxIds.Any())
            {
                dict = dict.Add(
                    idsKey,
                    TxIds.Select(txid => (IValue)(Binary)txid.ToByteArray()));
            }
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

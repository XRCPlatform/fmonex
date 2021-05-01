using Bencodex;
using Bencodex.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Libplanet.Net.Messages
{
	//FMONECHANGE -  added new message
    public class Transactions : IBenEncodeable
    {
        private static readonly byte[] TransactionsKey = { 0x42 };    // 'B'

        public Transactions(IEnumerable<byte[]> payloads)
        {
            if (payloads.Count() > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(payloads),
                    $"The number of payloads can't exceed {int.MaxValue}.");
            }

            Payloads = payloads.ToList();
        }

        public Transactions(byte[] bytes) : this(DecodeBytesToBen(bytes))
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

        public Transactions(Dictionary dict)
        {
            var temp = new List<byte[]>();
            if (dict.ContainsKey((IKey)(Binary)TransactionsKey))
            {
                Payloads = dict.GetValue<Bencodex.Types.List>(TransactionsKey)
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
            return new Transactions(DecodeBytesToBen(bytes));
        }

        public Dictionary ToBencodex()
        {
            var dict = Dictionary.Empty;
            if (Payloads.Any())
            {
                dict = dict.Add(
                    TransactionsKey,
                    Payloads.Select(b => (IValue)(Binary)b));
            }
            return dict;
        }
    }
}
using Libplanet.Net.Messages;
using Libplanet.Tx;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Xunit;

namespace Libplanet.Tests.Net.Messages
{
	//FMONECHANGE - added missing test
    public class GetTxsTests
    {
        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            List<TxId> txIds = GenerateTxIds(100L);

            var msg = new GetTxs(txIds);

            var ben = msg.SerializeToBen();
            var result = new GetTxs(ben);

            Assert.Equal(msg.TxIds, result.TxIds);
            
        }


        private static List<TxId> GenerateTxIds(long count)
        {
            List<TxId> list = new List<TxId>();
            var random = new Random();
            var buffer = new byte[HashDigest<SHA256>.Size];
            for (long i = 0; i < count; i++)
            {
                random.NextBytes(buffer);
                list.Add(new TxId(buffer));
            }
            return list;
        }
    }
}

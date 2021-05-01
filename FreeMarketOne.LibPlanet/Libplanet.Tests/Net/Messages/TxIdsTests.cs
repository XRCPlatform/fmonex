using Libplanet.Net.Messages;
using Libplanet.Tx;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Xunit;

namespace Libplanet.Tests.Net.Messages
{
	//FMONECHANGE added new test class to verify message serialziation handling
    public class TxIdsTests
    {
        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            List<TxId> txIds = GenerateTxIds(100L);
            var random = new Random();
            var buffer = new byte[Address.Size];
            random.NextBytes(buffer);            
            Address address = new Address(buffer);

            TxIds msg = new TxIds(address, txIds);

            var ben = msg.SerializeToBen();
            var result = new TxIds(ben);

            Assert.Equal(msg.Ids, result.Ids);
            Assert.Equal(msg.Sender, result.Sender);
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

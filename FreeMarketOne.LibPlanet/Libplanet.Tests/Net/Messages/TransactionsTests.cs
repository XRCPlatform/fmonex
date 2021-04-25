using Libplanet.Net.Messages;
using System;
using System.Collections.Generic;
using Xunit;


namespace Libplanet.Tests.Net.Messages
{
	//FMONECHANGE added new test class to verify message serialziation handling
    public class TransactionsTests
    {
        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            var payloads = GenerateRandomBlockPayloads(100);
            Transactions msg = new Transactions(payloads);
            var ben = msg.SerializeToBen();
            var result = new Transactions(ben);
            Assert.Equal(msg.Payloads, result.Payloads);
        }

        private static IEnumerable<byte[]> GenerateRandomBlockPayloads(long count)
        {
            var random = new Random();
            var buffer = new byte[500];
            for (long i = 0; i < count; i++)
            {
                random.NextBytes(buffer);
                yield return buffer;
            }
        }
    }
}

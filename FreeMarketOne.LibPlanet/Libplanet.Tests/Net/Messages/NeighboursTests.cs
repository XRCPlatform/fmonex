using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Net.Messages;
using Libplanet.Tx;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using Xunit;

namespace Libplanet.Tests.Net.Messages
{
    public class NeighboursTests
    {
        [Fact]
        public void SerializesAndDesrializeFromBen()
        {
            List<BoundPeer> peers = GeneratePeers(100L);
            
            var msg = new Neighbors(peers);
            var ben = msg.SerializeToBen();
            
            var result = new Neighbors(ben);

            Assert.Equal(msg.Found, result.Found);

        }


        private static List<BoundPeer> GeneratePeers(long count)
        {
            List<BoundPeer> list = new List<BoundPeer>();
            var random = new Random();
            var buffer = new byte[HashDigest<SHA256>.Size];
            for (int i = 0; i < count; i++)
            {
                random.NextBytes(buffer);
                var privateKey = new PrivateKey(buffer);                
                DnsEndPoint endppoint = new DnsEndPoint($"freeMarket{i}.onion", i);
                list.Add(new BoundPeer(privateKey.PublicKey, endppoint));
            }
            return list;
        }
    }
}

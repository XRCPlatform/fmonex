using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Net.Messages;
using NetMQ;
using Xunit;

namespace Libplanet.Tests.Net.Messages
{
    public class EnvelopeTests
    {
        [Fact]
        public void CanSerializeAndRestoreMessageToEnvelope()
        {
            HashDigest<SHA256>[] blockHashes = GenerateRandomBlockHashes(100L).ToArray();
            var msg = new BlockHashes(123, blockHashes);
            Assert.Equal(123, msg.StartIndex);
            Assert.Equal(blockHashes, msg.Hashes);
            var privKey = new PrivateKey();
            AppProtocolVersion ver = AppProtocolVersion.Sign(privKey, 3);
            BoundPeer peer = new BoundPeer(privKey.PublicKey, new DnsEndPoint("0.0.0.0", 1234));

            var envelope = new Envelope(peer, ver);
            envelope.Initialize(privKey, msg);

            Assert.Equal(msg.GetType(),envelope.GetMessageType());
            var restored = envelope.GetBody<BlockHashes>();

            Assert.Equal(msg.StartIndex, restored.StartIndex);
            Assert.Equal(msg.Hashes, restored.Hashes);
        }

        private static IEnumerable<HashDigest<SHA256>> GenerateRandomBlockHashes(long count)
        {
            var random = new Random();
            var buffer = new byte[HashDigest<SHA256>.Size];
            for (long i = 0; i < count; i++)
            {
                random.NextBytes(buffer);
                yield return new HashDigest<SHA256>(buffer);
            }
        }
    }
}

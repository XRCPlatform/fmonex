using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Net.Messages;
using Nito.AsyncEx;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Libplanet.Tests
{
    public class TorSocks5TransportTest
    {
        private static readonly PrivateKey VersionSigner = new PrivateKey();
        private static readonly AppProtocolVersion AppProtocolVersion =
            AppProtocolVersion.Sign(VersionSigner, 1);
        private ILogger _logger = null;
        private PrivateKey _privateKey = new PrivateKey();
        private TorSocks5Transport torSocks5Transport = null;

        public TorSocks5TransportTest()
        {
            _logger = Log.ForContext<TorSocks5TransportTest>();
            torSocks5Transport = new TorSocks5Transport(VersionSigner, AppProtocolVersion, ImmutableHashSet<PublicKey>.Empty, 10, 10, 0, "127.0.0.1", 9114, null, null, _logger, null, null);

        }

        [Fact]
        public void PacksLibPlanetMessageIntoTransportMessage()
        {
      
            HashDigest<SHA256>[] blockHashes = GenerateRandomBlockHashes(100L).ToArray();
            var msg = new BlockHashes(123, blockHashes);
            Assert.Equal(123, msg.StartIndex);
            Assert.Equal(blockHashes, msg.Hashes);
            var privKey = new PrivateKey();
            BoundPeer peer = new BoundPeer(privKey.PublicKey, new DnsEndPoint("freemarket.onion", 9114));

            var envelope = new Envelope(peer, AppProtocolVersion);
            envelope.Initialize(privKey, msg);

            Assert.Equal(msg.GetType(), envelope.GetMessageType());
            
            byte[] pak = torSocks5Transport.PackEnvelope(envelope);
            var return_envelope = torSocks5Transport.UnPackEnvelope(pak);
            return_envelope.IsValid(AppProtocolVersion, null, null);
            var result = return_envelope.GetBody<BlockHashes>();
            Assert.Equal(msg.StartIndex, result.StartIndex);
            Assert.Equal(msg.Hashes, result.Hashes);
        }

        [Fact]
        public void PerfomsMessageSignatureValidation_RetrunsIsValid()
        {
            HashDigest<SHA256>[] blockHashes = GenerateRandomBlockHashes(100L).ToArray();
            var msg = new BlockHashes(123, blockHashes);
            Assert.Equal(123, msg.StartIndex);
            Assert.Equal(blockHashes, msg.Hashes);
            var privKey = new PrivateKey();
            BoundPeer peer = new BoundPeer(privKey.PublicKey, new DnsEndPoint("freemarket.onion", 9114));

            var envelope = new Envelope(peer, AppProtocolVersion);
            envelope.Initialize(privKey, msg);
            Assert.Equal(msg.GetType(), envelope.GetMessageType());

            byte[] pak = torSocks5Transport.PackEnvelope(envelope);
            var return_envelope = torSocks5Transport.UnPackEnvelope(pak);
            Assert.True(return_envelope.IsValid(AppProtocolVersion, null, null));

            var result = return_envelope.GetBody<BlockHashes>();
            Assert.Equal(msg.StartIndex, result.StartIndex);
            Assert.Equal(msg.Hashes, result.Hashes);
        }


        [Fact]
        public void PerfomsAppProtocolVersionValidation_ThrowsExceptionIfInvalid()
        {
            HashDigest<SHA256>[] blockHashes = GenerateRandomBlockHashes(100L).ToArray();
            var msg = new BlockHashes(123, blockHashes);
            Assert.Equal(123, msg.StartIndex);
            Assert.Equal(blockHashes, msg.Hashes);
            var privKey = new PrivateKey();
            AppProtocolVersion ver = AppProtocolVersion.Sign(privKey, 1);
            BoundPeer peer = new BoundPeer(privKey.PublicKey, new DnsEndPoint("freemarket.onion", 9114));

            var envelope = new Envelope(peer, AppProtocolVersion);
            envelope.Initialize(privKey, msg);
            Assert.Equal(msg.GetType(), envelope.GetMessageType());

            byte[] pak = torSocks5Transport.PackEnvelope(envelope);
            var return_envelope = torSocks5Transport.UnPackEnvelope(pak);
            
            var result = return_envelope.GetBody<BlockHashes>();
            Assert.Equal(msg.StartIndex, result.StartIndex);
            Assert.Equal(msg.Hashes, result.Hashes);

            Assert.Throws<DifferentAppProtocolVersionException>(() => return_envelope.IsValid(ver, ImmutableHashSet<PublicKey>.Empty, null));
        }

        [Fact]
        public void PacksLibPlanetMessageIntoTransportMessage_WithNonEmpty()
        {
            // Note that here Unicode strings are used on purpose:
            IImmutableDictionary<string, IValue> states = ImmutableDictionary<string, IValue>.Empty
                .Add("foo甲", null)
                .Add("bar乙", default(Null))
                .Add("baz丙", new Text("a value 값"));

            HashDigest<SHA256> blockHash = new Random().NextHashDigest<SHA256>();
            var blockStates = new BlockStates(blockHash, states);
            var privKey = new PrivateKey();
            BoundPeer peer = new BoundPeer(privKey.PublicKey, new DnsEndPoint("freemarket.onion", 9114));

            var envelope = new Envelope(peer, AppProtocolVersion);
            envelope.Initialize(privKey, blockStates);

            Assert.Equal(blockStates.GetType(), envelope.GetMessageType());

            byte[] pak = torSocks5Transport.PackEnvelope(envelope);
            var return_envelope = torSocks5Transport.UnPackEnvelope(pak);
            return_envelope.IsValid(AppProtocolVersion, null, null);
            var result = return_envelope.GetBody<BlockStates>();
            Assert.Equal(blockStates.BlockHash, result.BlockHash);
            Assert.Equal(states, result.States);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using NetMQ;

namespace Libplanet.Net.Messages
{
    internal class BlockBroadcast : Message
    {
        public BlockBroadcast(IEnumerable<byte[]> payloads, HashDigest<SHA256> genesisHash)
        {
            GenesisHash = genesisHash;
            if (payloads.Count() > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(payloads),
                    $"The number of payloads can't exceed {int.MaxValue}.");
            }

            Payloads = payloads.ToList();
        }

        public HashDigest<SHA256> GenesisHash { get; }

        public BlockBroadcast(NetMQFrame[] body)
        {
            GenesisHash = new HashDigest<SHA256>(body[0].Buffer);
            int payloadCount = body[1].ConvertToInt32();
            Payloads = body.Skip(2).Take(payloadCount)
                .Select(f => f.ToByteArray())
                .ToList();
        }

        public List<byte[]> Payloads { get; }

        protected override MessageType Type => MessageType.BlockBroadcast;

        protected override IEnumerable<NetMQFrame> DataFrames
        {
            get
            {
                yield return new NetMQFrame(GenesisHash.ToByteArray());
                yield return new NetMQFrame(
                    NetworkOrderBitsConverter.GetBytes(Payloads.Count));

                foreach (var payload in Payloads)
                {
                    yield return new NetMQFrame(payload);
                }
            }
        }
    }
}

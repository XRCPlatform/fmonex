using System.Collections.Generic;
using NetMQ;

namespace Libplanet.Net.Messages
{
    internal class TxBroadcast : Message
    {
        public TxBroadcast(byte[] payload)
        {
            Payload = payload;
        }

        public TxBroadcast(NetMQFrame[] body)
        {
            Payload = body.ToByteArray();
        }

        public byte[] Payload { get; }

        protected override MessageType Type => MessageType.TxBroadcast;

        protected override IEnumerable<NetMQFrame> DataFrames
        {
            get
            {
                yield return new NetMQFrame(Payload);
            }
        }
    }
}

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
//FMONECHANGE added binary serialzation required for TorSocks5Transport
using System.Runtime.Serialization.Formatters.Binary;
using Libplanet.Crypto;

namespace Libplanet.Net
{
    [Serializable]
    [Equals]
    public sealed class BoundPeer : Peer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoundPeer"/> class.
        /// </summary>
        /// <param name="publicKey">A <see cref="PublicKey"/> of the
        /// <see cref="Peer"/>.</param>
        /// <param name="endPoint">A <see cref="DnsEndPoint"/> consisting of the
        /// host and port of the <see cref="Peer"/>.</param>
        public BoundPeer(
            PublicKey publicKey,
            DnsEndPoint endPoint)
        : this(publicKey, endPoint, null)
        {
        }

        internal BoundPeer(
            PublicKey publicKey,
            DnsEndPoint endPoint,
            IPAddress publicIPAddress)
        : base(publicKey, publicIPAddress)
        {
            EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        }

        private BoundPeer(SerializationInfo info, StreamingContext context)
        : base(info, context)
        {
            EndPoint = new DnsEndPoint(
                info.GetString("end_point_host"),
                info.GetInt32("end_point_port"));
        }

        /// <summary>
        /// The corresponding <see cref="DnsEndPoint"/> of this peer.
        /// </summary>
        [Pure]
        public DnsEndPoint EndPoint { get; }

        public static bool operator ==(BoundPeer left, BoundPeer right) =>
            Operator.Weave(left, right);

        public static bool operator !=(BoundPeer left, BoundPeer right) =>
            Operator.Weave(left, right);

        /// <inheritdoc/>
        public override void GetObjectData(
            SerializationInfo info,
            StreamingContext context
        )
        {
            base.GetObjectData(info, context);
            info.AddValue("end_point_host", EndPoint.Host);
            info.AddValue("end_point_port", EndPoint.Port);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Address}.{EndPoint}.{PublicIPAddress}";
        }
		//FMONECHANGE added binary serialzation required for TorSocks5Transport
        public static BoundPeer DeserializePeer(byte[] bytes)
        {
            var formatter = new BinaryFormatter();
            using MemoryStream stream = new MemoryStream(bytes);
            return (BoundPeer)formatter.Deserialize(stream);
        }
		//FMONECHANGE added binary serialzation required for TorSocks5Transport
        public static byte[] SerializePeer(BoundPeer peer)
        {
            var formatter = new BinaryFormatter();
            using MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, peer);
            return stream.ToArray();
        }

    }
}

using Libplanet.Crypto;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Libplanet.Net.Messages
{
    public class Envelope
    {
        private static readonly Dictionary<MessageType, Type> types = new Dictionary<MessageType, Type>
        {
            { MessageType.Ping, typeof(Ping) },
            { MessageType.Pong, typeof(Pong) },
            { MessageType.GetBlockHashes, typeof(GetBlockHashes) },
            { MessageType.BlockHashes, typeof(BlockHashes) },
            { MessageType.TxIds, typeof(TxIds) },
            { MessageType.GetBlocks, typeof(GetBlocks) },
            { MessageType.GetTxs, typeof(GetTxs) },
            { MessageType.Blocks, typeof(Blocks) },
            { MessageType.Transactions, typeof(Transactions) },
            { MessageType.FindNeighbors, typeof(FindNeighbors) },
            { MessageType.Neighbors, typeof(Neighbors) },
            { MessageType.GetRecentStates, typeof(GetRecentStates) },
            { MessageType.RecentStates, typeof(RecentStates) },
            { MessageType.BlockHeaderMessage, typeof(BlockHeaderMessage) },
            { MessageType.GetChainStatus, typeof(GetChainStatus) },
            { MessageType.ChainStatus, typeof(ChainStatus) },
            { MessageType.GetBlockStates, typeof(GetBlockStates) },
            { MessageType.BlockStates, typeof(BlockStates) },
            { MessageType.DifferentVersion, typeof(DifferentVersion) },
            { MessageType.BlockBroadcast, typeof(Blocks) },
            { MessageType.TxBroadcast, typeof(TxBroadcast) },
            { MessageType.ChatItem, typeof(ChatItem) },
        };
        public Envelope(){}

        public Envelope(BoundPeer fromPeer, AppProtocolVersion version)
        {
            FromPeer = fromPeer;
            PublicKey = Convert.ToBase64String(fromPeer.PublicKey.Format(false));
            Version = version;            
        }
        
        public void Initialize<T>(PrivateKey key, T body)
        {
            MessageType = MapTo(body);
            if (body is IBenEncodeable serializable)
            {
                byte[] bytes = serializable.SerializeToBen();
                Body = Convert.ToBase64String(bytes.ToArray());
                Signature = Convert.ToBase64String(key.Sign(bytes.ToArray()));
            }
            else 
            {
                throw new Exception("Item to be sent does not implement IBenEncodeable");
            }
        }

        /// <summary>
        /// Returns a message body that was earlier initialized.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetBody<T>()
        {
            if (Body == null) {
                throw new Exception("Body is only available on Initialized message.");
            }
            var retval = Activator.CreateInstance(GetMessageType(), new[] 
            { 
                Convert.FromBase64String(Body) 
            });
            return (T)retval;
        }

        public MessageType MessageType { get; set; }
        public string Body { get; set; }
        public string Signature { get; set; }
        public BoundPeer FromPeer { get; set; }
        public string PublicKey { get; set; }
        public AppProtocolVersion Version { get; set; }

        public bool IsValid(AppProtocolVersion localVersion, IImmutableSet<PublicKey> trustedAppProtocolVersionSigners, DifferentAppProtocolVersionEncountered differentAppProtocolVersionEncountered)
        {
            var remotePubKey = new PublicKey(Convert.FromBase64String(PublicKey));
            Peer remotePeer = new Peer(remotePubKey);

            if (!IsAppProtocolVersionValid(
              remotePeer,
              localVersion,
              Version,
              trustedAppProtocolVersionSigners,
              differentAppProtocolVersionEncountered))
            {
                throw new DifferentAppProtocolVersionException(
                    "Received message's version is not valid.",
                    null,
                    localVersion,
                    Version);
            }

            if (!remotePubKey.Verify(Convert.FromBase64String(Body), Convert.FromBase64String(Signature)))
            {
                throw new Exception("The message signature is invalid");
                //throw new InvalidMessageException("The message signature is invalid", this.MessageType);
            }

            return true;
        }

        public Type GetMessageType()
        {
            if (!types.TryGetValue(MessageType, out Type type))
            {
                throw new ArgumentException($"Can't map recieved message type: {MessageType}.");
            }
            return type;
        }

        public static MessageType MapTo<T>(T Body)
        {
            foreach (var item in types)
            {
                if (item.Value == Body.GetType())
                {
                    return item.Key;
                }
            }
            return MessageType.Unrecognized;
        }

        public static Type MapTo(MessageType messageType)
        {
            return types[messageType];
        }

        private static bool IsAppProtocolVersionValid(
           Peer remotePeer,
           AppProtocolVersion localVersion,
           AppProtocolVersion remoteVersion,
           IImmutableSet<PublicKey> trustedAppProtocolVersionSigners,
           DifferentAppProtocolVersionEncountered differentAppProtocolVersionEncountered)
        {
            if (remoteVersion.Equals(localVersion))
            {
                return true;
            }

            if (!(trustedAppProtocolVersionSigners is null) &&
                !trustedAppProtocolVersionSigners.Any(remoteVersion.Verify))
            {
                return false;
            }

            if (differentAppProtocolVersionEncountered is null)
            {
                return false;
            }

            return differentAppProtocolVersionEncountered(remotePeer, remoteVersion, localVersion);
        }

    }
}

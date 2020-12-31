using Libplanet;
using Libplanet.Crypto;
using Libplanet.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.ProtocolVersions
{
    public class DebugNetworkProtocolVersion : IProtocolVersion
    {
        private const string PROTOCOL_PRIVATEKEY = "4e38e00a5099f41ef238e8ca6431632e9de4de3259fcee1bb00e4234054877fe";
        private const int PROTOCOL_CURRENTVERSION = 1;
        public bool DifferentAppProtocolVersionEncountered(Peer peer, AppProtocolVersion peerVersion, AppProtocolVersion localVersion)
        {
            return false;
        }

        public IEnumerable<PublicKey> GetProtocolSigners()
        {
            var signers = new List<PublicKey>();
            var privateKey = new PrivateKey(ByteUtil.ParseHex(PROTOCOL_PRIVATEKEY));

            signers.Add(privateKey.PublicKey);

            return signers;
        }

        public AppProtocolVersion GetProtocolVersion()
        {
            var privateKey = new PrivateKey(ByteUtil.ParseHex(PROTOCOL_PRIVATEKEY));

            return AppProtocolVersion.Sign(privateKey, PROTOCOL_CURRENTVERSION);
        }
    }
}

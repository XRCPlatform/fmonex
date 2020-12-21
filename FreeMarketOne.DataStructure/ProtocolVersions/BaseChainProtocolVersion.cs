using Libplanet;
using Libplanet.Crypto;
using Libplanet.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.ProtocolVersions
{
    public class BaseChainProtocolVersion : IProtocolVersion
    {
        private const string PROTOCOL_PRIVATEKEY = "5454cd24321bcc98e656b17c0c6cc0868e777502b69016e38b93d847e132cc16";
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

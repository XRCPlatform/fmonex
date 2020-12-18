using Libplanet.Crypto;
using Libplanet.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.ProtocolVersions
{
    public interface IProtocolVersion
    {
        //how to generate key
        //var newKey = new PrivateKey();
        //var newKeyHex = ByteUtil.Hex(newKey.ByteArray);

        bool DifferentAppProtocolVersionEncountered(
            Peer peer,
            AppProtocolVersion peerVersion,
            AppProtocolVersion localVersion);

        IEnumerable<PublicKey> GetProtocolSigners();
        AppProtocolVersion GetProtocolVersion();
    }
}

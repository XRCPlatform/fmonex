﻿using Libplanet;
using Libplanet.Crypto;
using Libplanet.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.ProtocolVersions
{
    public class MarketChainProtocolVersion : IProtocolVersion
    {
        private const string _privateKey = "837e81505ec81b456e04ed943c0bc7d3ee77254bf9cbe7add85a08d87e3d82d8";

        public bool DifferentAppProtocolVersionEncountered(Peer peer, AppProtocolVersion peerVersion, AppProtocolVersion localVersion)
        {
            return false;
        }

        public IEnumerable<PublicKey> GetProtocolSigners()
        {
            var signers = new List<PublicKey>();
            var privateKey = new PrivateKey(ByteUtil.ParseHex(_privateKey));

            signers.Add(privateKey.PublicKey);

            return signers;
        }

        public AppProtocolVersion GetProtocolVersion()
        {
            var privateKey = new PrivateKey(ByteUtil.ParseHex(_privateKey));

            return AppProtocolVersion.Sign(privateKey, 1);
        }
    }
}
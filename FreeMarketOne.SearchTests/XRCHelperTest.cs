using FreeMarketOne.Search;
using FreeMarketOne.Search.XRCDaemon;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeMarketOne.SearchTests
{
    [TestClass()]
    public class XRCHelperTest
    {
        [TestInitialize]
        public void TestInitialize()
        {

        }

        [TestMethod()]
        public void TransactionSummaryIsReturned()
        {
            string txHash = "5c20ecff8c0a517b9770784198f56cacac212f5f057329388240686af0134039";
            string model = "{\r\n        \"hex\": \"01000000010000000000000000000000000000000000000000000000000000000000000000ffffffff025100ffffffff010090b3377f5f00001976a914cf93c457a91bc9ad797560928622a58d19ce190288ac00000000\",\r\n        \"txid\": \"5c20ecff8c0a517b9770784198f56cacac212f5f057329388240686af0134039\",\r\n        \"size\": 87,\r\n        \"version\": 1,\r\n        \"locktime\": 0,\r\n        \"vin\": [\r\n            {\r\n                \"coinbase\": \"5100\",\r\n                \"sequence\": 4294967295\r\n            }\r\n        ],\r\n        \"vout\": [\r\n            {\r\n                \"value\": 1050000.00000000,\r\n                \"n\": 0,\r\n                \"scriptPubKey\": {\r\n                    \"asm\": \"OP_DUP OP_HASH160 cf93c457a91bc9ad797560928622a58d19ce1902 OP_EQUALVERIFY OP_CHECKSIG\",\r\n                    \"hex\": \"76a914cf93c457a91bc9ad797560928622a58d19ce190288ac\",\r\n                    \"reqSigs\": 1,\r\n                    \"type\": \"pubkeyhash\",\r\n                    \"addresses\": [\r\n                        \"RsYMxMxMrW7KngFEq9jWfmuHakYL3pY8f8\"\r\n                    ]\r\n                }\r\n            }\r\n        ],\r\n        \"blockhash\": \"785793c0c87e83ed9cf3851359210c03123aa6a43d2d0a96ee000c4373e24274\",\r\n        \"confirmations\": 12171,\r\n        \"time\": 1540066578,\r\n        \"blocktime\": 1540066578\r\n    }";
            TransactionVerboseModel transaction = JsonConvert.DeserializeObject<TransactionVerboseModel>(model);
            IXRCDaemonClient client = Substitute.For<IXRCDaemonClient>();
            client.GetTransaction(txHash).Returns(transaction);
            XRCHelper helper = new XRCHelper(client);
            var summary = helper.GetTransaction(txHash, "RsYMxMxMrW7KngFEq9jWfmuHakYL3pY8f8");

            Assert.AreEqual(summary.Confirmations, 12171);
            Assert.AreEqual(summary.Total, 1050000.00000000m);
            Assert.AreEqual(summary.Date.UtcDateTime, DateTime.Parse("Saturday, 20 October 2018 20:16:18")); 


        }

        [TestMethod()]
        public void TransactionTotalIsCaluculated()
        {
            string txHash = "5c20ecff8c0a517b9770784198f56cacac212f5f057329388240686af0134039";
            string model = "{\r\n        \"hex\": \"01000000010000000000000000000000000000000000000000000000000000000000000000ffffffff025100ffffffff010090b3377f5f00001976a914cf93c457a91bc9ad797560928622a58d19ce190288ac00000000\",\r\n        \"txid\": \"5c20ecff8c0a517b9770784198f56cacac212f5f057329388240686af0134039\",\r\n        \"size\": 87,\r\n        \"version\": 1,\r\n        \"locktime\": 0,\r\n        \"vin\": [\r\n            {\r\n                \"coinbase\": \"5100\",\r\n                \"sequence\": 4294967295\r\n            }\r\n        ],\r\n        \"vout\": [\r\n          {\r\n            \"value\": 1050000.00000000,\r\n            \"n\": 0,\r\n            \"scriptPubKey\": {\r\n              \"asm\": \"OP_DUP OP_HASH160 cf93c457a91bc9ad797560928622a58d19ce1902 OP_EQUALVERIFY OP_CHECKSIG\",\r\n              \"hex\": \"76a914cf93c457a91bc9ad797560928622a58d19ce190288ac\",\r\n              \"reqSigs\": 1,\r\n              \"type\": \"pubkeyhash\",\r\n              \"addresses\": [\r\n                \"RsYMxMxMrW7KngFEq9jWfmuHakYL3pY8f8\"\r\n              ]\r\n            }\r\n          },\r\n          {\r\n            \"value\": 1050000.00000000,\r\n            \"n\": 0,\r\n            \"scriptPubKey\": {\r\n              \"asm\": \"fake\",\r\n              \"hex\": \"fake\",\r\n              \"reqSigs\": 1,\r\n              \"type\": \"pubkeyhash\",\r\n              \"addresses\": [\r\n                \"RsYMxMxMrW7KngFEq9jWfmuHakYL3pY8f8\"\r\n              ]\r\n            }\r\n          }\r\n        ],\r\n        \"blockhash\": \"785793c0c87e83ed9cf3851359210c03123aa6a43d2d0a96ee000c4373e24274\",\r\n        \"confirmations\": 12171,\r\n        \"time\": 1540066578,\r\n        \"blocktime\": 1540066578\r\n    }";
            TransactionVerboseModel transaction = JsonConvert.DeserializeObject<TransactionVerboseModel>(model);
            IXRCDaemonClient client = Substitute.For<IXRCDaemonClient>();
            client.GetTransaction(txHash).Returns(transaction);
            XRCHelper helper = new XRCHelper(client);
            var summary = helper.GetTransaction(txHash, "RsYMxMxMrW7KngFEq9jWfmuHakYL3pY8f8");

            Assert.AreEqual(summary.Confirmations, 12171);
            Assert.AreEqual(summary.Total, 2100000.00000000m);
            Assert.AreEqual(summary.Date.UtcDateTime, DateTime.Parse("Saturday, 20 October 2018 20:16:18"));


        }
    }
}

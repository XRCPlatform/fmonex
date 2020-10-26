using Castle.Core.Logging;
using FreeMarketOne.DataStructure;
using FreeMarketOne.Search;
using FreeMarketOne.Search.XRCDaemon;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using System.Linq;
using System.Transactions;

namespace FreeMarketOne.SearchTests
{
    [TestClass()]
    public class XRCDaemonTests
    {
        private static IBaseConfiguration config;
        [TestInitialize]
        public void TestInitialize()
        {

        }

        [TestMethod()]
        public void TransactionModelCanBeDeserialized()
        {
            string model = "{\r\n        \"hex\": \"01000000010000000000000000000000000000000000000000000000000000000000000000ffffffff025100ffffffff010090b3377f5f00001976a914cf93c457a91bc9ad797560928622a58d19ce190288ac00000000\",\r\n        \"txid\": \"5c20ecff8c0a517b9770784198f56cacac212f5f057329388240686af0134039\",\r\n        \"size\": 87,\r\n        \"version\": 1,\r\n        \"locktime\": 0,\r\n        \"vin\": [\r\n            {\r\n                \"coinbase\": \"5100\",\r\n                \"sequence\": 4294967295\r\n            }\r\n        ],\r\n        \"vout\": [\r\n            {\r\n                \"value\": 1050000.00000000,\r\n                \"n\": 0,\r\n                \"scriptPubKey\": {\r\n                    \"asm\": \"OP_DUP OP_HASH160 cf93c457a91bc9ad797560928622a58d19ce1902 OP_EQUALVERIFY OP_CHECKSIG\",\r\n                    \"hex\": \"76a914cf93c457a91bc9ad797560928622a58d19ce190288ac\",\r\n                    \"reqSigs\": 1,\r\n                    \"type\": \"pubkeyhash\",\r\n                    \"addresses\": [\r\n                        \"RsYMxMxMrW7KngFEq9jWfmuHakYL3pY8f8\"\r\n                    ]\r\n                }\r\n            }\r\n        ],\r\n        \"blockhash\": \"785793c0c87e83ed9cf3851359210c03123aa6a43d2d0a96ee000c4373e24274\",\r\n        \"confirmations\": 12171,\r\n        \"time\": 1540066578,\r\n        \"blocktime\": 1540066578\r\n    }";
            TransactionVerboseModel transaction = JsonConvert.DeserializeObject<TransactionVerboseModel>(model);
            Assert.AreEqual(transaction.TxId, "5c20ecff8c0a517b9770784198f56cacac212f5f057329388240686af0134039");
            Assert.AreEqual(transaction.VOut.FirstOrDefault().Value, 1050000.00000000m);
            Assert.AreEqual(transaction.VOut.FirstOrDefault().ScriptPubKey.Addresses.FirstOrDefault(), "RsYMxMxMrW7KngFEq9jWfmuHakYL3pY8f8");
        }

        [TestMethod()]
        public void CanRetrieveATransactionFromRPCCall()
        {
            JsonSerializerSettings serializerSettings = new JsonSerializerSettings();
            
            config = Substitute.For<IBaseConfiguration>();
            config.XRCDaemonUriSsl.Returns(false);
            config.XRCDaemonUri.Returns("188.127.231.159:16661");
            config.XRCDaemonUser.Returns("fm1_xrc_testnet_user");
            config.XRCDaemonPassword.Returns("fm1_xrc_testnet_password");
            
            var logger = Substitute.For<Serilog.ILogger>();

            XRCDaemonClient client = new XRCDaemonClient(serializerSettings, config, logger);

            var transaction = client.GetTransaction("0830cfff8e379e60fb56049811ef3fb332e505068a3a7e4edd9764d7721e98be").ConfigureAwait(false).GetAwaiter().GetResult();
            Assert.AreEqual(transaction.TxId, "0830cfff8e379e60fb56049811ef3fb332e505068a3a7e4edd9764d7721e98be");
            Assert.AreEqual(transaction.VOut.FirstOrDefault().Value, 2.50000000m);
            Assert.AreEqual(transaction.VOut.FirstOrDefault().ScriptPubKey.Addresses.FirstOrDefault(), "TRbPDkwa8a73TS2FwGJv9ZhmTgnjtdPSkm");
        }

        
    }
}

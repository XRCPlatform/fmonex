using Castle.Core.Logging;
using ElectrumXClient;
using ElectrumXClient.Response;
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
            string model = "{'jsonrpc': '2.0', 'result': {'hex': '0100000001fcb70e8303e5e787a414c027bb045cde48628b3d3377210e77053683d59f6e52000000006a4730440220291df88846a5d5d074bcbbd5f42d537283e549a76d5e98d09d4c26c877f2db9902201fed295f3eb905bd27604ce484eec7b153cae0b93dbf805c98cbe16bfcf0c51501210202ca1d15f7ad59c7bd84ba9bcc82be89e0bc629201dea7cb4578db422ea594e6ffffffff02c8263900000000001976a914a5b8c4e950013ceb22f7f2f2fb43e24793f92e0188ac107aad0e000000001976a914cf345d14cbf5ca8bee2fc3b429c09cc08dc3073288ac00000000', 'txid': 'ce64713852d70c1e6bead818edad6c7b6dd0b4d13be48fbb3f2888f3cd286bc8', 'size': 225, 'version': 1, 'locktime': 0, 'vin': [{'txid': '526e9fd5833605770e2177333d8b6248de5c04bb27c014a487e7e503830eb7fc', 'vout': 0, 'scriptSig': {'asm': '30440220291df88846a5d5d074bcbbd5f42d537283e549a76d5e98d09d4c26c877f2db9902201fed295f3eb905bd27604ce484eec7b153cae0b93dbf805c98cbe16bfcf0c51501 0202ca1d15f7ad59c7bd84ba9bcc82be89e0bc629201dea7cb4578db422ea594e6', 'hex': '4730440220291df88846a5d5d074bcbbd5f42d537283e549a76d5e98d09d4c26c877f2db9902201fed295f3eb905bd27604ce484eec7b153cae0b93dbf805c98cbe16bfcf0c51501210202ca1d15f7ad59c7bd84ba9bcc82be89e0bc629201dea7cb4578db422ea594e6'}, 'sequence': 4294967295}], 'vout': [{'value': 0.0374548, 'n': 0, 'scriptPubKey': {'asm': 'OP_DUP OP_HASH160 a5b8c4e950013ceb22f7f2f2fb43e24793f92e01 OP_EQUALVERIFY OP_CHECKSIG', 'hex': '76a914a5b8c4e950013ceb22f7f2f2fb43e24793f92e0188ac', 'reqSigs': 1, 'type': 'pubkeyhash', 'addresses': ['TR5TqhVu4pmVdqDG8YwrPwkgbioiHNjFnX']}}, {'value': 2.4625, 'n': 1, 'scriptPubKey': {'asm': 'OP_DUP OP_HASH160 cf345d14cbf5ca8bee2fc3b429c09cc08dc30732 OP_EQUALVERIFY OP_CHECKSIG', 'hex': '76a914cf345d14cbf5ca8bee2fc3b429c09cc08dc3073288ac', 'reqSigs': 1, 'type': 'pubkeyhash', 'addresses': ['TUroc47d4tFPueYdBc7vX9BmtAN16jC7Yw']}}], 'blockhash': '3f0934dc7505e40a440ba97e3cdc6c69b6e77a47e493e0f1ef4549f071b49fc5', 'confirmations': 1, 'time': 1623970068, 'blocktime': 1623970068}, 'id': 0}";
            BlockchainTransactionGetResponse transaction = JsonConvert.DeserializeObject<BlockchainTransactionGetResponse>(model);
            Assert.AreEqual(transaction.Result.Txid, "ce64713852d70c1e6bead818edad6c7b6dd0b4d13be48fbb3f2888f3cd286bc8");
            Assert.AreEqual(transaction.Result.VoutValue.FirstOrDefault().Value, 0.03745480d);
            Assert.AreEqual(transaction.Result.VoutValue.FirstOrDefault().ScriptPubKey.Addresses.FirstOrDefault(), "TR5TqhVu4pmVdqDG8YwrPwkgbioiHNjFnX");
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


        [TestMethod()]
        public void CanRetrieveATransactionFromElectrumX()
        {
            JsonSerializerSettings serializerSettings = new JsonSerializerSettings();

            config = Substitute.For<IBaseConfiguration>();
            config.XRCDaemonUriSsl.Returns(false);
            config.XRCDaemonUri.Returns("telectrum.xrhodium.org");
            config.XRCDaemonPort.Returns(51002);
            config.XRCDaemonUriSsl.Returns(true);

            var logger = Substitute.For<Serilog.ILogger>();

            ElectrumClient client = new ElectrumClient(config.XRCDaemonUri, config.XRCDaemonPort, config.XRCDaemonUriSsl);

            var response = client.GetBlockchainTransactionGet("0830cfff8e379e60fb56049811ef3fb332e505068a3a7e4edd9764d7721e98be", true).ConfigureAwait(false).GetAwaiter().GetResult();
            Assert.IsNotNull(response);
            Assert.AreEqual(response.Result.Txid, "0830cfff8e379e60fb56049811ef3fb332e505068a3a7e4edd9764d7721e98be");
            Assert.AreEqual(response.Result.VoutValue.FirstOrDefault().Value, 2.5d);
            Assert.AreEqual(response.Result.VoutValue.FirstOrDefault().ScriptPubKey.Addresses.FirstOrDefault(), "TRbPDkwa8a73TS2FwGJv9ZhmTgnjtdPSkm");
        }

    }
}

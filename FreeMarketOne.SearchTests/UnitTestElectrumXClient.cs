using ElectrumXClient;
using ElectrumXClient.Response;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace ElectrumXClient.Tests
{
    [TestClass()]
    public class UnitTestElectrumXClient
    {
        private ElectrumClient _client;

        [TestInitialize]
        public void Setup()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("client-secrets.json")
                .Build();
            var host = "telectrum.xrhodium.org,telectrum1.xrhodium.org";
            var port = 51002;
            var useSSL = true;

            _client = new ElectrumClient(host, port, useSSL);
        }

        [TestMethod()]
        public async Task Test_CanGetBlockchainTransactionGet()
        {
           var response = await _client.GetBlockchainTransactionGet("ce64713852d70c1e6bead818edad6c7b6dd0b4d13be48fbb3f2888f3cd286bc8", true);
            Assert.AreEqual(response.Result.Txid, "ce64713852d70c1e6bead818edad6c7b6dd0b4d13be48fbb3f2888f3cd286bc8");
            Assert.AreEqual(response.Result.VoutValue.FirstOrDefault().Value, 0.03745480d);
            Assert.AreEqual(response.Result.VoutValue.FirstOrDefault().ScriptPubKey.Addresses.FirstOrDefault(), "TR5TqhVu4pmVdqDG8YwrPwkgbioiHNjFnX");
        }
      
    }
}

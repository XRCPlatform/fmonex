using FreeMarketOne.DataStructure;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Linq;

namespace FreeMarketOne.Search
{
    public class XRCHelper : IXRCHelper
    {
        IXRCDaemonClient _client;
        public XRCHelper(IXRCDaemonClient client)
        {
            _client = client;
        }

        public XRCTransactionSummary GetTransaction(string address, string hash)
        {
            var xrcTransaction = _client.GetTransaction(hash).ConfigureAwait(false).GetAwaiter().GetResult();
            if (xrcTransaction == null){
                return null;
            }

            decimal total = 0;
            foreach (var vout in xrcTransaction.VOut)
            {
                foreach (var taddress in vout.ScriptPubKey.Addresses)
                {
                    if (taddress.Equals(address))
                    {
                        total += vout.Value;
                    }
                }                
            }

            //var total = xrcTransaction.VOut.Where(x => x.ScriptPubKey.Addresses.Equals(address)).Sum(x => x.Value);

            return new XRCTransactionSummary()
            {
                Confirmations = xrcTransaction.Confirmations.GetValueOrDefault(),
                Date = DateTimeOffset.FromUnixTimeSeconds((long)xrcTransaction.BlockTime.GetValueOrDefault()),
                Total = total
            };
        }
    }
}
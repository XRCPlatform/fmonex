using FreeMarketOne.DataStructure;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Linq;
using ElectrumXClient;

namespace FreeMarketOne.Search
{
    public class XRCHelper : IXRCHelper
    {
        IElectrumClient _client;
        public XRCHelper(IElectrumClient client)
        {
            _client = client;
        }

        public XRCTransactionSummary GetTransaction(string address, string hash)
        {
            try
            {
                var result = _client.GetBlockchainTransactionGet(hash, true).ConfigureAwait(false).GetAwaiter().GetResult();
                var xrcTransaction = result.Result;
                if (xrcTransaction == null)
                {
                    return new XRCTransactionSummary()
                    {
                        Confirmations = 0,
                        Date = DateTimeOffset.UtcNow,
                        Total = 0
                    };
                }

                double total = 0;
                foreach (var vout in xrcTransaction.VoutValue)
                {
                    if (vout.ScriptPubKey.Addresses != null)
                    {
                        foreach (var taddress in vout.ScriptPubKey.Addresses)
                        {
                            if (taddress.Equals(address))
                            {
                                total += vout.Value;
                            }
                        }
                    }
                }
                //var total = xrcTransaction.VOut.Where(x => x.ScriptPubKey.Addresses.Equals(address)).Sum(x => x.Value);

                return new XRCTransactionSummary()
                {
                    Confirmations = xrcTransaction.Confirmations,
                    Date = DateTimeOffset.FromUnixTimeSeconds(xrcTransaction.Blocktime),
                    Total = total
                };
            }
            catch (Exception)
            {
                
            }
            //should only hit this is exeptional cirumstance
            return new XRCTransactionSummary()
            {
                Confirmations = 0,
                Date = DateTimeOffset.UtcNow,
                Total = 0
            };
        }


    }
}
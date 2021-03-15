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
            try
            {
                var xrcTransaction = _client.GetTransaction(hash).ConfigureAwait(false).GetAwaiter().GetResult();
                if (xrcTransaction == null)
                {
                    return new XRCTransactionSummary()
                    {
                        Confirmations = 0,
                        Date = DateTimeOffset.UtcNow,
                        Total = 0
                    };
                }

                decimal total = 0;
                foreach (var vout in xrcTransaction.VOut)
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
                    Confirmations = xrcTransaction.Confirmations.GetValueOrDefault(),
                    Date = DateTimeOffset.FromUnixTimeSeconds((long)xrcTransaction.BlockTime.GetValueOrDefault()),
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
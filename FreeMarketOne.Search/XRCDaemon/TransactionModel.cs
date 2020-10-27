using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.Search.XRCDaemon
{

    public class TransactionVerboseModel 
    {
        [JsonProperty(Order = 1, PropertyName = "txid")]
        public string TxId { get; set; }

        [JsonProperty(Order = 2, PropertyName = "size")]
        public int Size { get; set; }

        [JsonProperty(Order = 3, PropertyName = "version")]
        public uint Version { get; set; }

        [JsonProperty(Order = 4, PropertyName = "locktime")]
        public uint LockTime { get; set; }

        [JsonProperty(Order = 5, PropertyName = "vin")]
        public List<Vin> VIn { get; set; }

        [JsonProperty(Order = 6, PropertyName = "vout")]
        public List<Vout> VOut { get; set; }

        [JsonProperty(Order = 7, PropertyName = "blockhash", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string BlockHash { get; set; }

        [JsonProperty(Order = 8, PropertyName = "confirmations", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? Confirmations { get; set; }

        [JsonProperty(Order = 9, PropertyName = "time", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public uint? Time { get; set; }

        [JsonProperty(Order = 10, PropertyName = "blocktime", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public uint? BlockTime { get; set; }
    }

    public class Vin
    {
        [JsonProperty(Order = 0, PropertyName = "coinbase", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Coinbase { get; set; }

        [JsonProperty(Order = 1, PropertyName = "txid", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TxId { get; set; }

        [JsonProperty(Order = 2, PropertyName = "vout", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public uint? VOut { get; set; }

        [JsonProperty(Order = 3, PropertyName = "scriptSig", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Script ScriptSig { get; set; }

        [JsonProperty(Order = 4, PropertyName = "sequence")]
        public uint Sequence { get; set; }
    }

    public class Vout
    {

        [JsonProperty(Order = 0, PropertyName = "value")]
        public decimal Value { get; set; }

        [JsonProperty(Order = 1, PropertyName = "n")]
        public int N { get; set; }

        [JsonProperty(Order = 2, PropertyName = "scriptPubKey")]
        public ScriptPubKey ScriptPubKey { get; set; }
    }

    public class Script
    {
        [JsonProperty(Order = 0, PropertyName = "asm")]
        public string Asm { get; set; }

        [JsonProperty(Order = 1, PropertyName = "hex")]
        public string Hex { get; set; }
    }

    public class ScriptPubKey : Script
    {
        [JsonProperty(Order = 2, PropertyName = "reqSigs", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? ReqSigs { get; set; }

        [JsonProperty(Order = 3, PropertyName = "type", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty(Order = 4, PropertyName = "addresses", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<string> Addresses { get; set; }
       
    }
}

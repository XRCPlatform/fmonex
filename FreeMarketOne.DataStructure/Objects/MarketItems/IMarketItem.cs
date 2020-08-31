using JsonSubTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.MarketItems
{
    [JsonConverter(typeof(JsonSubtypes), "_nt")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(MarketItemV1), "MarketItemV1")]
    public interface IMarketItem
    {
        string nametype { get; set; }
        
        string Title { get; set; }

        /// <summary>
        /// Unique Hash of content of market item created by GenerateHash() function manually for verification of data integrity
        /// </summary>
        string Hash { get; set; }

        /// <summary>
        /// Signature is connection to user to check integrity and user ownership
        /// </summary>
        string Signature { get; set; }

        /// <summary>
        /// BaseSignature is connection to chain of product update.
        /// </summary>
        string BaseSignature { get; set; }

        string Description { get; set; }
        string Shipping { get; set; }
        string DealType { get; set; }
        string Category { get; set; }
        List<string> Photos { get; set; }
        DateTime CreatedUtc { get; set; }
        
        bool IsValid();

        /// <summary>
        /// Generate market item hash based on unique content
        /// </summary>
        /// <returns></returns>
        string GenerateHash();

        byte[] ToByteArrayForSign();
    }
}

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace FreeMarketOne.Search
{
    public class SearchHelper
    {
        public static List<string> GenerateSellerPubKeyHashes(List<byte[]> pubKeys)
        {
            List<string> list = new List<string>();
            if (pubKeys == null)
            {
                return list;
            }
            foreach (var pubKey in pubKeys)
            {
                list.Add(Hash(pubKey));
            }
            return list;
        }


        /// <summary>
        /// Choosing lower grade hash for speed and disk space efficiency as colisions are irrelevant in this context
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static string Hash(byte[] rawData)
        {
            using (SHA1 shaHash = SHA1.Create())
            {
                byte[] bytes = shaHash.ComputeHash(rawData);
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}

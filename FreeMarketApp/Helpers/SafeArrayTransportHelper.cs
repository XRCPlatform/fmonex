using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeMarketApp.Helpers
{
    internal class SafeArrayTransportHelper
    {
        internal string GetString(string[] values)
        {
            string output = string.Empty;

            if (values.Any())
            {
                foreach (var itemValue in values)
                {
                    var safeValue = Base64Encode(itemValue);

                    if (!string.IsNullOrEmpty(output))
                    {
                        output = output + "|";
                    }

                    output = output + safeValue;
                }
            }

            return output;
        }

        internal string[] GetArray(string value)
        {
            var output = new List<string>();

            if (!string.IsNullOrEmpty(value))
            {
                var values = value.Split("|");
                foreach (var itemValue in values)
                {
                    var safeValue = Base64Decode(itemValue);
                    output.Add(safeValue);
                }
            }

            return output.ToArray();
        }

        private string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        private string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

    }
}

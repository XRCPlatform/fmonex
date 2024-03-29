﻿using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace FreeMarketOne.Telemetry
{
    public class JsonContent : StringContent
    {
        /// <summary>
        /// Wraps Object to JSON Content with apropriate content type.
        /// </summary>
        /// <param name="message">indented json message string</param>
        public JsonContent(object obj) :
            base(JsonConvert.SerializeObject(obj, Formatting.Indented), Encoding.UTF8, "application/json")
        {
        }

        /// <summary>
        /// Wraps JSON message from string to Content with apropriate content type.
        /// </summary>
        /// <param name="message">indented json message string</param>
        public JsonContent(string message) :
            base(message, Encoding.UTF8, "application/json")
        {

        }
    }
}

using System;
using System.Net;

namespace FreeMarketOne.Search
{
    public class DaemonClientException : Exception
    {
        public DaemonClientException(string msg) : base(msg)
        {
        }

        public DaemonClientException(HttpStatusCode code, string msg) : base(msg)
        {
            Code = code;
        }

        public HttpStatusCode Code { get; set; }
    }
}
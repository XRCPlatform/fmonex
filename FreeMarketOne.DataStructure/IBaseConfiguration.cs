using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace FreeMarketOne.DataStructure
{
    public interface IBaseConfiguration
    {
        EndPoint TorEndPoint { get; set; }
        string OnionSeedsEndPoint { get; set; }
        string LogFilePath { get; set; }
        string Version { get; set; }
        int Environment { get; set; }
        string ChangellyApiKey { get; set; }
        string ChangellySecret { get; set; }
        string ChangellyApiBaseUrl { get; set; }
    }
}

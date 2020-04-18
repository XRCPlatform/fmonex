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
        string MemoryBasePoolPath { get; set; }
        string MemoryMarketPoolPath { get; set; }
        string BlockChainPath { get; set; }
    }
}

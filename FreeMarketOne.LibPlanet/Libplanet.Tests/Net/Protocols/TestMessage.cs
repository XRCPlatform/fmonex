using System.Collections.Generic;
using Libplanet.Net.Messages;
using NetMQ;

namespace Libplanet.Tests.Net.Protocols
{
	//FMONECHANGE -  no longer inherits from base class because we moved away from netmq
    internal class TestMessage 
    {
        public TestMessage(string data)
        {
            Data = data;
        }

        

        public string Data { get; }

       

    }
}

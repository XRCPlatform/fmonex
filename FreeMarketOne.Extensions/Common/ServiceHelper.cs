using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.Extensions.Common
{
    public class ServiceHelper
    {
        public enum CommonStates
        {
            NotStarted = 0,
            Running = 1,
            Stopping = 2,
            Stopped = 3
        }
    }
}

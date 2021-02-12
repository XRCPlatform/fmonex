using System;
using System.Runtime.Serialization;

namespace Libplanet.Net
{
    [Serializable]
    internal class PooledSocketUnavailableException : Exception
    {
        public PooledSocketUnavailableException()
        {
        }

        public PooledSocketUnavailableException(string message) : base(message)
        {
        }

        public PooledSocketUnavailableException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PooledSocketUnavailableException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
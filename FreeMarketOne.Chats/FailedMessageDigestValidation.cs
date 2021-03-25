using System;
using System.Runtime.Serialization;

namespace FreeMarketOne.Chats
{
    [Serializable]
    internal class FailedMessageDigestValidation : Exception
    {
        public FailedMessageDigestValidation()
        {
        }

        public FailedMessageDigestValidation(string message) : base(message)
        {
        }

        public FailedMessageDigestValidation(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FailedMessageDigestValidation(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
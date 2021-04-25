#nullable enable
using System;
using System.Runtime.Serialization;
using Libplanet.Net.Messages;

namespace Libplanet.Net
{
	//FMONECHANGE refactored this calls to stop using Message class which was NetMq centric
    public class InvalidMessageException : Exception
    {
        internal InvalidMessageException(string message, MessageType receivedMessage, Exception innerException)
            : base(message, innerException)
        {
            ReceivedMessage = receivedMessage;
        }

        internal InvalidMessageException(string message, MessageType receivedMessage)
            : base(message)
        {
            ReceivedMessage = receivedMessage;
        }

        internal MessageType ReceivedMessage { get; }
       
    }
}

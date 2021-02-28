using Libplanet.Net.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Libplanet.Net
{
    internal readonly struct MessageRequest
    {
        private readonly int _retried;

        public MessageRequest(
            in Guid id,
            Message message,
            BoundPeer peer,
            DateTimeOffset requestedTime,
            in TimeSpan? timeout,
            in int expectedResponses,
            TaskCompletionSource<IEnumerable<Message>> taskCompletionSource)
            : this(
                  id,
                  message,
                  peer,
                  requestedTime,
                  timeout,
                  expectedResponses,
                  taskCompletionSource,
                  0
                )
        {
        }

        internal MessageRequest(
            in Guid id,
            Message message,
            BoundPeer peer,
            DateTimeOffset requestedTime,
            in TimeSpan? timeout,
            in int expectedResponses,
            TaskCompletionSource<IEnumerable<Message>> taskCompletionSource,
            int retried)
        {
            Id = id;
            Message = message;
            Peer = peer;
            RequestedTime = requestedTime;
            Timeout = timeout;
            ExpectedResponses = expectedResponses;
            TaskCompletionSource = taskCompletionSource;
            _retried = retried;
        }

        public Guid Id { get; }

        public Message Message { get; }

        public BoundPeer Peer { get; }

        public DateTimeOffset RequestedTime { get; }

        public TimeSpan? Timeout { get; }

        public int ExpectedResponses { get; }

        public TaskCompletionSource<IEnumerable<Message>> TaskCompletionSource { get; }

        public bool Retryable => _retried < 3;

        public MessageRequest Retry()
        {
            return new MessageRequest(
                Id,
                Message,
                Peer,
                RequestedTime,
                Timeout,
                ExpectedResponses,
                TaskCompletionSource,
                _retried + 1
            );
        }
    }
}

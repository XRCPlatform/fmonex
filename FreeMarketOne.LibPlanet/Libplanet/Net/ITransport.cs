using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FreeMarketOne.Tor;
using FreeMarketOne.Tor.TorOverTcp.Models.Messages;
using Libplanet.Net.Messages;

namespace Libplanet.Net
{
    internal interface ITransport : IDisposable
    {
        BoundPeer AsPeer { get; }

        IEnumerable<BoundPeer> Peers { get; }

        DateTimeOffset? LastMessageTimestamp { get; }

        Task StartAsync(CancellationToken cancellationToken = default(CancellationToken));

        Task RunAsync(CancellationToken cancellationToken = default(CancellationToken));

        Task StopAsync(
            TimeSpan waitFor,
            CancellationToken cancellationToken = default(CancellationToken));

        Task BootstrapAsync(
            IEnumerable<BoundPeer> bootstrapPeers,
            TimeSpan? pingSeedTimeout,
            TimeSpan? findNeighborsTimeout,
            int depth,
            CancellationToken cancellationToken);

        Task<TResponse> SendMessageWithReplyAsync<TRequest, TResponse>(
            BoundPeer peer,
            TRequest message,
            TimeSpan? timeout);

        Task BroadcastMessage<T>(BoundPeer except, T message);

        void ReplyMessage<TResponse>(TotRequest request, TotClient client, TResponse responseMessage);
    }
}

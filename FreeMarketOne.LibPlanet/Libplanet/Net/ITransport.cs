using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using FreeMarketOne.Tor;
using FreeMarketOne.Tor.TorOverTcp.Models.Messages;
using Libplanet.Net.Messages;

namespace Libplanet.Net
{
    /// <summary>
    /// An interface to handle peer-to-peer networking
    /// and <see cref="Peer"/> managing.
    /// </summary>
    public interface ITransport : IDisposable
    {

        // FMONECHANGE removed event EventHandler<Message> ProcessMessageHandler as TorSocks5Transport does not need this.

        /// <summary>
        /// <see cref="Peer"/> representation of <see cref="ITransport"/>.
        /// </summary>
        // FMONECHANGE changed from Peer to Bound peer as TorSocks5Transport only works with BoundPeers
        [Pure]
        BoundPeer AsPeer { get; }

        /// <summary>
        /// The <see cref="DateTimeOffset"/> of the last message was received.
        /// </summary>
        [Pure]
        DateTimeOffset? LastMessageTimestamp { get; }

        /// <summary>
        /// Whether this <see cref="ITransport"/> instance is running.
        /// </summary>
        /// <value>Gets the value indicates whether the instance is running.</value>
        [Pure]
        bool Running { get; }

        /// <summary>
        /// Initiates transport layer.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token used to propagate notification that this
        /// operation should be canceled.</param>
        /// <returns>An awaitable task without value.</returns>
        Task StartAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Starts running transport layer. To <see cref="RunAsync"/>, you should call
        /// <see cref="StartAsync"/> first.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token used to propagate notification that this
        /// operation should be canceled.</param>
        /// <returns>An awaitable task without value.</returns>
        Task RunAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Stops running transport layer.
        /// </summary>
        /// <param name="waitFor">The <see cref="TimeSpan"/> of delay
        /// before actual stopping.</param>
        /// <param name="cancellationToken">
        /// A cancellation token used to propagate notification that this
        /// operation should be canceled.</param>
        /// <returns>An awaitable task without value.</returns>
        Task StopAsync(
            TimeSpan waitFor,
            CancellationToken cancellationToken = default(CancellationToken));

        // FMONECHANGE added BootstrapAsync to support TorSocks5Transport
        Task BootstrapAsync(
            IEnumerable<BoundPeer> bootstrapPeers,
            TimeSpan? pingSeedTimeout,
            TimeSpan? findNeighborsTimeout,
            int depth,
            CancellationToken cancellationToken);

        // FMONECHANGE added SendMessageWithReplyAsync to support TorSocks5Transport
        Task<TResponse> SendMessageWithReplyAsync<TRequest, TResponse>(
            BoundPeer peer,
            TRequest message,
            TimeSpan? timeout);

        // FMONECHANGE added BroadcastMessage to support TorSocks5Transport
        Task BroadcastMessage<T>(BoundPeer except, T message);

        // FMONECHANGE added ReplyMessage to support TorSocks5Transport
        void ReplyMessage<TResponse>(TotRequest request, TotClient client, TResponse responseMessage);
    }
}

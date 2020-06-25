using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Net.Protocols;
using Serilog;
using System;
using System.Collections;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using FreeMarketOne.DataStructure;
using Libplanet.Extensions.Helpers;

namespace FreeMarketOne.BlockChain
{
    internal class PeerBootstrapWorker<T> : IDisposable where T : IBaseAction, new()
    {
        private ILogger _logger { get; set; }
        private CancellationTokenSource _cancellationToken { get; set; }

        private const int SwarmDialTimeout = 5000;

        private PrivateKey _privateKey { get; set; }
        private BlockChain<T> _blockChain;
        private Swarm<T> _swarmServer;
        private ImmutableList<Peer> _seedPeers;
        private IImmutableSet<Address> _trustedPeers;
        private EventHandler _bootstrapStarted { get; set; }
        private EventHandler _preloadStarted { get; set; }
        private EventHandler<PreloadState> _preloadProcessed { get; set; }
        private EventHandler _preloadEnded { get; set; }

        internal PeerBootstrapWorker(
            ILogger serverLogger,
            Swarm<T> swarmServer,
            BlockChain<T> blockChain,
            ImmutableList<Peer> seedPeers,
            IImmutableSet<Address> trustedPeers,
            PrivateKey privateKey,
            EventHandler bootstrapStarted = null,
            EventHandler preloadStarted = null,
            EventHandler<PreloadState> preloadProcessed = null,
            EventHandler preloadEnded = null
            )
        {
            _logger = serverLogger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                string.Format("{0}.{1}.{2}", typeof(PeerBootstrapWorker<T>).Namespace, typeof(PeerBootstrapWorker<T>).Name.Replace("`1", string.Empty), typeof(T).Name));

            _blockChain = blockChain;
            _swarmServer = swarmServer;
            _seedPeers = seedPeers;
            _trustedPeers = trustedPeers;
            _privateKey = privateKey;

            _bootstrapStarted = bootstrapStarted;
            _preloadStarted = preloadStarted;
            _preloadProcessed = preloadProcessed;
            _preloadEnded = preloadEnded;

            _cancellationToken = new CancellationTokenSource();

            _logger.Information("Initializing Peer Bootstrap Worker");
        }

        internal IEnumerator GetEnumerator()
        {
            if (_swarmServer == null)
            {
                _logger.Error("Swarm listener is dead.");
            }
            else
            {
                _bootstrapStarted?.Invoke(this, null);

                var bootstrapTask = Task.Run(async () =>
                {
                    try
                    {
                        await _swarmServer.BootstrapAsync(
                            _seedPeers,
                            5000,
                            5000,
                            cancellationToken: _cancellationToken.Token
                        );
                    }
                    catch (TimeoutException e)
                    {
                        _logger.Error(e.Message);
                    }
                    catch (PeerDiscoveryException e)
                    {
                        _logger.Error(e.Message);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(string.Format("Exception occurred during bootstrap {0}", e));
                    }
                });

                yield return new WaitUntil(() => bootstrapTask.IsCompleted);

                _preloadStarted?.Invoke(this, null);
                _logger.Information("PreloadingStarted event was invoked");

                DateTimeOffset started = DateTimeOffset.UtcNow;
                long existingBlocks = _blockChain?.Tip?.Index ?? 0;
                _logger.Information("Preloading starts");

                var swarmPreloadTask = Task.Run(async () =>
                {
                    await _swarmServer.PreloadAsync(
                        TimeSpan.FromMilliseconds(SwarmDialTimeout),
                        new Progress<PreloadState>(state =>
                            _preloadProcessed?.Invoke(this, state)
                        ),
                        trustedStateValidators: _trustedPeers,
                        cancellationToken: _cancellationToken.Token
                    );
                });

                yield return new WaitUntil(() => swarmPreloadTask.IsCompleted);

                DateTimeOffset ended = DateTimeOffset.UtcNow;
                if (swarmPreloadTask.Exception is Exception e)
                {
                    _logger.Error(string.Format("Preloading terminated with an exception: {0}", e));
                    throw e;
                }

                var index = _blockChain?.Tip?.Index ?? 0;

                _logger.Information("Preloading finished; elapsed time: {0}; blocks: {1}",
                    ended - started,
                    index - existingBlocks
                );

                _preloadEnded?.Invoke(this, null);

                var swarmStartTask = Task.Run(async () =>
                {
                    try
                    {
                        await _swarmServer.StartAsync();
                    }
                    catch (TaskCanceledException e)
                    {
                        _logger.Error(e.Message);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(string.Format("Swarm terminated with an exception: {0}", e));
                        throw e;
                    }
                });

                Task.Run(async () =>
                {
                    await _swarmServer.WaitForRunningAsync();

                    _logger.Information(
                        "The address of this node: {0},{1},{2}",
                        ByteUtil.Hex(_privateKey.PublicKey.Format(true)),
                        _swarmServer.EndPoint.Host,
                        _swarmServer.EndPoint.Port
                    );
                });

                yield return new WaitUntil(() => swarmStartTask.IsCompleted);
            }
        }
        public void Dispose()
        {
            _logger.Information("Peer Bootstrap stopping.");

            _cancellationToken.Cancel();

            _logger.Information("Peer Bootstrap stopped.");
        }
    }
}

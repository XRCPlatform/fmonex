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
using Libplanet.Blocks;

namespace FreeMarketOne.BlockChain
{
    internal class PeerBootstrapWorker<T> : IDisposable where T : IBaseAction, new()
    {
        private ILogger _logger { get; set; }
        private CancellationTokenSource _cancellationToken { get; set; }

        private const int SwarmDialTimeout = 300000;

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
                var swarmStartTask = Task.Run(async () =>
                {
                    try
                    {
                        await _swarmServer.StartAsync(cancellationToken: _cancellationToken.Token);
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
                    try
                    {
                        await _swarmServer.WaitForRunningAsync();
                        await _swarmServer.AddPeersAsync(
                            _seedPeers,
                            TimeSpan.FromMinutes(2),
                            _cancellationToken.Token);
                    }
                    catch (TimeoutException e)
                    {
                        _logger.Error(e.Message);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(string.Format("Exception occurred during AddPeers {0}", e));
                    }

                    var bootstrapTask = Task.Run(async () =>
                    {
                        try
                        {
                            await _swarmServer.BootstrapAsync(
                                _seedPeers,
                                SwarmDialTimeout,
                                SwarmDialTimeout,
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

                    await PreloadAsync();

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

        private async Task PreloadAsync()
        {
            DateTimeOffset started = DateTimeOffset.UtcNow;
            long existingBlocks = _blockChain?.Tip?.Index ?? 0;
            _logger.Information("Preloading starts");
            _preloadStarted?.Invoke(this, null);

            try
            {
                await _swarmServer.PreloadAsync(
                    TimeSpan.FromMilliseconds(SwarmDialTimeout),
                    new Progress<PreloadState>(state =>
                        _preloadProcessed?.Invoke(this, state)
                    ),
                    trustedStateValidators: _trustedPeers,
                    cancellationToken: _cancellationToken.Token
                );
            }
            catch (AggregateException e)
            {
                if (e.InnerException is InvalidGenesisBlockException)
                {
                    _logger.Error(string.Format("Preloading terminated with silence exception: {0}", e));
                } 
                else
                {
                    _logger.Error(string.Format("Preloading terminated with an exception: {0}", e));
                    throw e;
                }
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("Preloading terminated with an exception: {0}", e));
                throw e;
            }

            DateTimeOffset ended = DateTimeOffset.UtcNow;
            var index = _blockChain?.Tip?.Index ?? 0;
            _logger.Information("Preloading finished; elapsed time: {0}; blocks: {1}",
                ended - started,
                index - existingBlocks
            );
            _preloadEnded?.Invoke(this, null);
        }

        public void Dispose()
        {
            _logger.Information("Peer Bootstrap stopping.");

            _cancellationToken.Cancel();

            _logger.Information("Peer Bootstrap stopped.");
        }
    }
}

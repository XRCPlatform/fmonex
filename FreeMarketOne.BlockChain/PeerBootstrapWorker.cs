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
using FreeMarketOne.BlockChain.Helpers;
using FreeMarketOne.BlockChain.Actions;

namespace FreeMarketOne.BlockChain
{
    internal class PeerBootstrapWorker<T> : IDisposable where T : IBaseAction, new()
    {
        private ILogger logger { get; set; }
        private CancellationTokenSource cancellationToken { get; set; }

        private const int SwarmDialTimeout = 5000;

        private PrivateKey privateKey { get; set; }
        private BlockChain<T> blockChain;
        private Swarm<T> swarmServer;
        private ImmutableList<Peer> seedPeers;
        private IImmutableSet<Address> trustedPeers;
        private EventHandler bootstrapStarted { get; set; }
        private EventHandler preloadStarted { get; set; }
        private EventHandler<PreloadState> preloadProcessed { get; set; }
        private EventHandler preloadEnded { get; set; }

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
            this.logger = serverLogger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, typeof(T).FullName);

            this.blockChain = blockChain;
            this.swarmServer = swarmServer;
            this.seedPeers = seedPeers;
            this.trustedPeers = trustedPeers;
            this.privateKey = privateKey;

            this.bootstrapStarted = bootstrapStarted;
            this.preloadStarted = preloadStarted;
            this.preloadProcessed = preloadProcessed;
            this.preloadEnded = preloadEnded;

            this.cancellationToken = new CancellationTokenSource();

            this.logger.Information("Initializing Peer Bootstrap Worker");
        }

        internal IEnumerator GetEnumerator()
        {
            if (this.swarmServer == null)
            {
                this.logger.Error("Swarm listener is dead.");
            }
            else
            {
                this.bootstrapStarted?.Invoke(this, null);

                var bootstrapTask = Task.Run(async () =>
                {
                    try
                    {
                        await this.swarmServer.BootstrapAsync(
                            this.seedPeers,
                            5000,
                            5000,
                            cancellationToken: this.cancellationToken.Token
                        );
                    }
                    catch (PeerDiscoveryException e)
                    {
                        this.logger.Error(e.Message);
                    }
                    catch (Exception e)
                    {
                        this.logger.Error(string.Format("Exception occurred during bootstrap {0}", e));
                    }
                });

                yield return new WaitUntil(() => bootstrapTask.IsCompleted);

                this.preloadStarted?.Invoke(this, null);
                this.logger.Information("PreloadingStarted event was invoked");

                DateTimeOffset started = DateTimeOffset.UtcNow;
                long existingBlocks = this.blockChain?.Tip?.Index ?? 0;
                this.logger.Information("Preloading starts");

                var swarmPreloadTask = Task.Run(async () =>
                {
                    await this.swarmServer.PreloadAsync(
                        TimeSpan.FromMilliseconds(SwarmDialTimeout),
                        new Progress<PreloadState>(state =>
                            this.preloadProcessed?.Invoke(this, state)
                        ),
                        trustedStateValidators: this.trustedPeers,
                        cancellationToken: this.cancellationToken.Token
                    );
                });

                yield return new WaitUntil(() => swarmPreloadTask.IsCompleted);

                DateTimeOffset ended = DateTimeOffset.UtcNow;
                if (swarmPreloadTask.Exception is Exception e)
                {
                    this.logger.Error(string.Format("Preloading terminated with an exception: {0}", e));
                    throw e;
                }

                var index = blockChain?.Tip?.Index ?? 0;

                this.logger.Information("Preloading finished; elapsed time: {0}; blocks: {1}",
                    ended - started,
                    index - existingBlocks
                );

                this.preloadEnded?.Invoke(this, null);

                var swarmStartTask = Task.Run(async () =>
                {
                    try
                    {
                        await swarmServer.StartAsync();
                    }
                    catch (TaskCanceledException e)
                    {
                        this.logger.Error(e.Message);
                    }
                    catch (Exception e)
                    {
                        this.logger.Error(string.Format("Swarm terminated with an exception: {0}", e));
                        throw e;
                    }
                });

                Task.Run(async () =>
                {
                    await this.swarmServer.WaitForRunningAsync();

                    this.logger.Information(
                        "The address of this node: {0},{1},{2}",
                        ByteUtil.Hex(this.privateKey.PublicKey.Format(true)),
                        this.swarmServer.EndPoint.Host,
                        this.swarmServer.EndPoint.Port
                    );
                });

                yield return new WaitUntil(() => swarmStartTask.IsCompleted);
            }
        }
        public void Dispose()
        {
            logger.Information("Peer Bootstrap stopping.");

            cancellationToken.Cancel();

            logger.Information("Peer Bootstrap stopped.");
        }
    }
}

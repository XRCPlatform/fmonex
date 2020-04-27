using FreeMarketOne.Extensions.Helpers;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Net;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeMarketOne.BlockChain
{
    internal class PeerBootstrapWorker<T> : IDisposable where T : IBaseAction, new()
    {
        private ILogger logger { get; set; }
        private IAsyncLoopFactory asyncLoopFactory { get; set; }
        private CancellationTokenSource cancellationToken { get; set; }
        private const int SwarmDialTimeout = 5000;

        internal PeerBootstrapWorker(
            ILogger serverLogger,
            Swarm<T> swarm,
            BlockChain<T> blocks, 
            ImmutableList<Peer> seedPeers,
            IImmutableSet<Address> trustedPeers,
            PrivateKey privateKey)
        {
            this.logger = serverLogger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, typeof(T).FullName);

            logger.Information("Initializing Peer Bootstrap Worker");

            cancellationToken = new CancellationTokenSource();
            asyncLoopFactory = new AsyncLoopFactory(serverLogger);

            //Run one time only
            var periodicLogLoop = this.asyncLoopFactory.Run("PeerBootstrap" + typeof(T).Name, (cancellation) =>
            {
                if (swarm == null)
                {
                    logger.Error("Swarm listener is dead.");
                } 
                else
                {
                    var bootstrapTask = Task.Run(async () =>
                    {
                        try
                        {
                            await swarm.BootstrapAsync(
                                seedPeers,
                                5000,
                                5000,
                                cancellationToken: this.cancellationToken.Token
                            );
                        }
                        catch (Exception e)
                        {
                            logger.Error(string.Format("Exception occurred during bootstrap {0}", e));
                        }
                    });

                    bootstrapTask.Wait();

                    logger.Information("PreloadingStarted event was invoked");

                    DateTimeOffset started = DateTimeOffset.UtcNow;
                    long existingBlocks = blocks?.Tip?.Index ?? 0;

                    var swarmPreloadTask = Task.Run(async () =>
                    {
                        await swarm.PreloadAsync(
                            TimeSpan.FromMilliseconds(SwarmDialTimeout),
                            null,
                            trustedStateValidators: trustedPeers,
                            cancellationToken: this.cancellationToken.Token
                        );
                    });

                    swarmPreloadTask.Wait();

                    DateTimeOffset ended = DateTimeOffset.UtcNow;

                    if (swarmPreloadTask.Exception is Exception e)
                    {
                        logger.Error(string.Format("Preloading terminated with an exception: {0}", e));
                        throw e;
                    }

                    var index = blocks?.Tip?.Index ?? 0;

                    logger.Information("Preloading finished; elapsed time: {0}; blocks: {1}",
                        ended - started,
                        index - existingBlocks
                    );

                    var swarmStartTask = Task.Run(async () =>
                    {
                        try
                        {
                            await swarm.StartAsync();
                        }
                        catch (TaskCanceledException)
                        {
                        }
                        catch (Exception e)
                        {
                            logger.Error(string.Format("Swarm terminated with an exception: {0}", e));
                            throw e;
                        }
                    });

                    Task.Run(async () =>
                    {
                        await swarm.WaitForRunningAsync();

                        logger.Information(
                            "The address of this node: {0},{1},{2}",
                            ByteUtil.Hex(privateKey.PublicKey.Format(true)),
                            swarm.EndPoint.Host,
                            swarm.EndPoint.Port
                        );
                    });

                    swarmStartTask.Wait();
                }

                return Task.CompletedTask;
            },
                cancellationToken.Token,
                repeatEvery: TimeSpans.RunOnce);
        }

        public void Dispose()
        {
            logger.Information("Peer Bootstrap stopping.");

            cancellationToken.Cancel();

            logger.Information("Peer Bootstrap stopped.");
        }
    }
}

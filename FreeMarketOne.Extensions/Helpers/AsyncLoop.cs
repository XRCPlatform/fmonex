using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace FreeMarketOne.Extensions.Helpers
{
    public interface IAsyncLoop : IDisposable
    {
        string Name { get; }
        TimeSpan RepeatEvery { get; set; }
        IAsyncLoop Run(TimeSpan? repeatEvery = null, TimeSpan? startAfter = null);
        IAsyncLoop Run(CancellationToken cancellation, TimeSpan? repeatEvery = null, TimeSpan? startAfter = null);
        Task RunningTask { get; }
    }

    public class AsyncLoop : IAsyncLoop
    {
        private readonly ILogger logger;

        private readonly Func<CancellationToken, Task> loopAsync;

        public string Name { get; }

        public Task RunningTask { get; private set; }

        public TimeSpan RepeatEvery { get; set; }

        public AsyncLoop(string name, ILogger logger, Func<CancellationToken, Task> loop)
        {
            Guard.NotEmpty(name, nameof(name));
            Guard.NotNull(nameof(loop), loop);

            this.Name = name;
            this.logger = logger;
            this.loopAsync = loop;
            this.RepeatEvery = TimeSpan.FromMilliseconds(1000);
        }

        /// <inheritdoc />
        public IAsyncLoop Run(TimeSpan? repeatEvery = null, TimeSpan? startAfter = null)
        {
            return this.Run(CancellationToken.None, repeatEvery, startAfter);
        }

        /// <inheritdoc />
        public IAsyncLoop Run(CancellationToken cancellation, TimeSpan? repeatEvery = null, TimeSpan? startAfter = null)
        {
            Guard.NotNull(nameof(cancellation), cancellation);

            if (repeatEvery != null)
                this.RepeatEvery = repeatEvery.Value;

            this.RunningTask = this.StartAsync(cancellation, startAfter);

            return this;
        }

        /// <summary>
        /// Starts an application defined task inside the async loop.
        /// </summary>
        /// <param name="cancellation">Cancellation token that triggers when the task and the loop should be cancelled.</param>
        /// <param name="delayStart">Delay before the first run of the task, or null if no startup delay is required.</param>
        private Task StartAsync(CancellationToken cancellation, TimeSpan? delayStart = null)
        {
            return Task.Run(async () =>
            {
                Exception uncaughtException = null;
                this.logger.Information(this.Name + " starting");
                try
                {
                    if (delayStart != null)
                        await Task.Delay(delayStart.Value, cancellation).ConfigureAwait(false);

                    if (this.RepeatEvery == TimeSpans.RunOnce)
                    {
                        if (cancellation.IsCancellationRequested)
                            return;

                        await this.loopAsync(cancellation).ConfigureAwait(false);

                        return;
                    }

                    while (!cancellation.IsCancellationRequested)
                    {
                        await this.loopAsync(cancellation).ConfigureAwait(false);
                        if (!cancellation.IsCancellationRequested)
                            await Task.Delay(this.RepeatEvery, cancellation).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    if (!cancellation.IsCancellationRequested)
                        uncaughtException = ex;
                }
                catch (Exception ex)
                {
                    uncaughtException = ex;
                }
                finally
                {
                    this.logger.Information(this.Name + " stopping");
                }

                if (uncaughtException != null)
                {
                    // WARNING: Do NOT touch this line unless you want to fix weird AsyncLoop tests.
                    // The following line has to be called EXACTLY as it is.
                    this.logger.Error(uncaughtException, this.Name + " threw an unhandled exception");

                    // You can touch this one.
                    this.logger.Error("{0} threw an unhandled exception: {1}", this.Name, uncaughtException.ToString());
                }
            }, cancellation);
        }

        /// <summary>
        /// Wait for the loop task to complete.
        /// </summary>
        public void Dispose()
        {
            if (!this.RunningTask.IsCanceled)
            {
                this.logger.Information("Waiting for {0} to finish.", this.Name);
                this.RunningTask.Wait();
            }
        }
    }
}

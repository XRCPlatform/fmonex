using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Extensions.Models;
using FreeMarketOne.Tor.Exceptions;
using Serilog;
using Serilog.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeMarketOne.Tor
{
	public class TorProcessManager : IDisposable
	{
		/// <summary>
		/// If null then it's just a mock, clearnet is used.
		/// </summary>
		public EndPoint TorSocks5EndPoint { get; }

        public string TorOnionEndPoint { get; private set; }

        private ILogger logger { get; set; }

        public static bool RequestFallbackAddressUsage { get; private set; } = false;

		private Process torProcess { get; set; }

        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped
        /// </summary>
        private long running;

        public bool IsRunning => Interlocked.Read(ref running) == 1;

        private CancellationTokenSource stop { get; set; }

        /// <param name="torSocks5EndPoint">Opt out Tor with null.</param>
        /// <param name="logFile">Opt out of logging with null.</param>
        public TorProcessManager(Logger serverLogger, BaseConfiguration configuration)
        {
            logger = serverLogger.ForContext<TorProcessManager>();
            logger.Information("Initializing Tor Process Manager");

            TorSocks5EndPoint = configuration.TorEndPoint;
            running = 0;
			stop = new CancellationTokenSource();
			torProcess = null;
		}

        public bool Start()
        {
            if (TorSocks5EndPoint is null)
            {
                return false;
            }

            //new Thread(delegate () // Do not ask. This is the only way it worked on Win10/Ubuntu18.04/Manjuro(1 processor VM)/Fedora(1 processor VM)
            //{
                try
                {
                    try
                    {
                        var toolsDir = "tools";
                        var torPath = "";
                        var fullBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);

                        if (IsTorRunningAsync(TorSocks5EndPoint).GetAwaiter().GetResult())
                        {
                            logger.Warning("Tor is already running.");
                            GetOnionEndPoint(fullBaseDirectory, toolsDir);
                            return true;
                        }

                        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            if (!fullBaseDirectory.StartsWith('/'))
                            {
                                fullBaseDirectory.Insert(0, "/");
                            }

                            torPath = $@"{toolsDir}/Tor/tor";
                        }
                        else // If Windows
                        {
                            torPath = $@"{toolsDir}\Tor\tor.exe";
                        }

                        if (!File.Exists(torPath))
                        {
                            logger.Error($"Tor instance NOT found at {torPath}. Attempting to acquire it...");
                            InstallTor(fullBaseDirectory, toolsDir);

                            //copy configuration
                            CopyDefaultConfig(fullBaseDirectory, toolsDir);

                            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                // Make sure there's sufficient permission.
                                string chmodTorDirCmd = $"chmod -R 750 {toolsDir}";
                                var result = EnvironmentHelpers.ShellExec(chmodTorDirCmd);
                                if (result > 0)
                                {
                                    logger.Error($"Command: {chmodTorDirCmd} exited with exit code: {result}, instead of 0.");
                                }
                                else
                                {
                                    logger.Information($"Shell command executed: {chmodTorDirCmd}.");
                                }
                            }
                        }
                        else
                        {
                            logger.Information($"Tor instance found at {torPath}.");
                        }

                        string torArguments = $" -f torrc";
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            torProcess = Process.Start(new ProcessStartInfo
                            {
                                WorkingDirectory = $@"{toolsDir}/Tor/",
                                FileName = torPath,
                                Arguments = torArguments,
                                UseShellExecute = false,
                                CreateNoWindow = false,
                                RedirectStandardOutput = false
                            });
                            logger.Information($"Starting Tor process with Process.Start.");
                        }
                        else // Linux and OSX
                        {
                            string runTorCmd = $"LD_LIBRARY_PATH=$LD_LIBRARY_PATH:={toolsDir}/Tor && export LD_LIBRARY_PATH && cd {toolsDir}/Tor && ./tor {torArguments}";
                            EnvironmentHelpers.ShellExec(runTorCmd, false);
                            logger.Information($"Started Tor process with shell command: {runTorCmd}.");
                        }

                        //check if TOR is online
                        Task.Delay(3000).ConfigureAwait(false).GetAwaiter().GetResult(); // dotnet brainfart, ConfigureAwait(false) IS NEEDED HERE otherwise (only on) Manjuro Linux fails, WTF?!!
                        if (!IsTorRunningAsync(TorSocks5EndPoint).GetAwaiter().GetResult())
                        {
                            throw new TorException("Attempted to start Tor, but it is not running.");
                        }
                        else
                        {
                            GetOnionEndPoint(fullBaseDirectory, toolsDir);
                        }
                        logger.Information("Tor is running.");
                    }
                    catch (Exception ex)
                    {
                        throw new TorException("Could not automatically start Tor. Try running Tor manually.", ex);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message + " " + ex.StackTrace);

                    return false;
                }
            //}).Start();

            return true;
        }

        private void CopyDefaultConfig(string fullBaseDirectory, string torDir)
        {
            string torConfigDir = Path.Combine(fullBaseDirectory, torDir);
            string sourceConfig = Path.Combine(torConfigDir, "torrc-default");
            string targetConfig = Path.Combine(torConfigDir, "Tor", "torrc");

            File.Copy(sourceConfig, targetConfig);
        }

        private void GetOnionEndPoint(string fullBaseDirectory, string torDir)
        {
            string torHiddenServiceDir = Path.Combine(fullBaseDirectory, torDir, "Tor", "hidden_service");

            if (Directory.Exists(torHiddenServiceDir))
            {
                logger.Information($"Tor instance found at {torHiddenServiceDir}. Attempting to acquire it...");

                var hostNameFile = Path.Combine(torHiddenServiceDir, "hostname");

                try
                {
                    var onionEndPoint = File.ReadAllText(hostNameFile);
                    this.TorOnionEndPoint = onionEndPoint.Replace(Environment.NewLine, string.Empty);
                }
                catch (Exception)
                {
                    logger.Error($"Tor onion endpoint cant be loaded from {hostNameFile}.");
                }
            }
            else
            {
                logger.Error($"Tor instance not found at {torHiddenServiceDir}.");
            }
        }

        private void InstallTor(string fullBaseDirectory, string torDir)
        {
            string torDaemonsDir = Path.Combine(fullBaseDirectory, torDir);

            string dataZip = Path.Combine(torDaemonsDir, "data-folder.zip");
            IoHelpers.BetterExtractZipToDirectoryAsync(dataZip, torDir).GetAwaiter().GetResult();
            logger.Information($"Extracted {dataZip} to {torDir}.");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string torWinZip = Path.Combine(torDaemonsDir, "tor-win32.zip");
                IoHelpers.BetterExtractZipToDirectoryAsync(torWinZip, torDir).GetAwaiter().GetResult();
                logger.Information($"Extracted {torWinZip} to {torDir}.");
            }
            else // Linux or OSX
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    string torLinuxZip = Path.Combine(torDaemonsDir, "tor-linux64.zip");
                    IoHelpers.BetterExtractZipToDirectoryAsync(torLinuxZip, torDir).GetAwaiter().GetResult();
                    logger.Information($"Extracted {torLinuxZip} to {torDir}.");
                }
                else // OSX
                {
                    string torOsxZip = Path.Combine(torDaemonsDir, "tor-osx64.zip");
                    IoHelpers.BetterExtractZipToDirectoryAsync(torOsxZip, torDir).GetAwaiter().GetResult();
                    logger.Information($"Extracted {torOsxZip} to {torDir}.");
                }                
            }
        }

        /// <param name="torSocks5EndPoint">Opt out Tor with null.</param>
        public static async Task<bool> IsTorRunningAsync(EndPoint torSocks5EndPoint)
        {
            using (var client = new TorSocks5Client(torSocks5EndPoint))
            {
                try
                {
                    await client.ConnectAsync().ConfigureAwait(false);
                    await client.HandshakeAsync().ConfigureAwait(false);
                }
                catch (ConnectionException)
                {
                    return false;
                }
                return true;
            }
        }

        public async Task<bool> IsTorRunningAsync()
        {
            if (TorSocks5EndPoint is null)
            {
                return true;
            }

            using (var client = new TorSocks5Client(TorSocks5EndPoint))
            {
                try
                {
                    await client.ConnectAsync().ConfigureAwait(false);
                    await client.HandshakeAsync().ConfigureAwait(false);
                }
                catch (ConnectionException)
                {
                    return false;
                }
                return true;
            }
        }

        //      public void StartMonitor(TimeSpan torMisbehaviorCheckPeriod, TimeSpan checkIfRunningAfterTorMisbehavedFor, string dataDirToStartWith, Uri fallBackTestRequestUri)
        //      {
        //          if (TorSocks5EndPoint is null)
        //          {
        //              return;
        //          }

        //          Logger.LogInfo("Starting Tor monitor...");
        //          if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        //          {
        //              return;
        //          }

        //          Task.Run(async () =>
        //          {
        //              try
        //              {
        //                  while (IsRunning)
        //                  {
        //                      try
        //                      {
        //                          await Task.Delay(torMisbehaviorCheckPeriod, Stop.Token).ConfigureAwait(false);

        //                          if (TorHttpClient.TorDoesntWorkSince != null) // If Tor misbehaves.
        //                          {
        //                              TimeSpan torMisbehavedFor = (DateTimeOffset.UtcNow - TorHttpClient.TorDoesntWorkSince) ?? TimeSpan.Zero;

        //                              if (torMisbehavedFor > checkIfRunningAfterTorMisbehavedFor)
        //                              {
        //                                  if (TorHttpClient.LatestTorException is TorSocks5FailureResponseException torEx)
        //                                  {
        //                                      if (torEx.RepField == RepField.HostUnreachable)
        //                                      {
        //                                          Uri baseUri = new Uri($"{fallBackTestRequestUri.Scheme}://{fallBackTestRequestUri.DnsSafeHost}");
        //                                          using (var client = new TorHttpClient(baseUri, TorSocks5EndPoint))
        //                                          {
        //                                              var message = new HttpRequestMessage(HttpMethod.Get, fallBackTestRequestUri);
        //                                              await client.SendAsync(message, Stop.Token).ConfigureAwait(false);
        //                                          }

        //                                          // Check if it changed in the meantime...
        //                                          if (TorHttpClient.LatestTorException is TorSocks5FailureResponseException torEx2 && torEx2.RepField == RepField.HostUnreachable)
        //                                          {
        //                                              // Fallback here...
        //                                              RequestFallbackAddressUsage = true;
        //                                          }
        //                                      }
        //                                  }
        //                                  else
        //                                  {
        //                                      Logger.LogInfo($"Tor did not work properly for {(int)torMisbehavedFor.TotalSeconds} seconds. Maybe it crashed. Attempting to start it...");
        //                                      Start(true, dataDirToStartWith); // Try starting Tor, if it does not work it'll be another issue.
        //                                      await Task.Delay(14000, Stop.Token).ConfigureAwait(false);
        //                                  }
        //                              }
        //                          }
        //                      }
        //                      catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException || ex is TimeoutException)
        //                      {
        //                          Logger.LogTrace(ex);
        //                      }
        //                      catch (Exception ex)
        //                      {
        //                          Logger.LogDebug(ex);
        //                      }
        //                  }
        //              }
        //              finally
        //              {
        //                  Interlocked.CompareExchange(ref _running, 3, 2); // If IsStopping, make it stopped.
        //              }
        //          });
        //      }

        public async Task StopAsync()
        {
            Interlocked.CompareExchange(ref running, 2, 1); // If running, make it stopping.

            if (TorSocks5EndPoint is null)
            {
                Interlocked.Exchange(ref running, 3);
            }

            stop?.Cancel();
            while (Interlocked.CompareExchange(ref running, 3, 0) == 2)
            {
                await Task.Delay(50).ConfigureAwait(false);
            }
            stop?.Dispose();
            stop = null;
            torProcess?.Kill();
            torProcess?.Dispose();
            torProcess = null;
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }
    }
}

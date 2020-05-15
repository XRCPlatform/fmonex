using FreeMarketOne.DataStructure;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Tor.Exceptions;
using FreeMarketOne.Tor.TorSocks5;
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
		private IBaseConfiguration _configuration { get; }

        public string TorOnionEndPoint { get; private set; }

        private ILogger _logger { get; set; }

        public static bool RequestFallbackAddressUsage { get; private set; } = false;

		private Process _torProcess { get; set; }

        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped
        /// </summary>
        private long running;

        public bool IsRunning => Interlocked.Read(ref running) == 1;

        private CancellationTokenSource _stop { get; set; }

        /// <param name="serverLogger">Base server logger.</param>
        /// <param name="_configuration">Base _configuration.</param>
        public TorProcessManager(IBaseConfiguration configuration)
        {
            _logger = Log.Logger.ForContext<TorProcessManager>();
            _logger.Information("Initializing Tor Process Manager");

            _configuration = configuration;
            running = 0;
			_stop = new CancellationTokenSource();
			_torProcess = null;
		}

        public bool Start()
        {
            if (_configuration.TorEndPoint is null)
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

                        if (IsTorRunningAsync(_configuration.TorEndPoint).GetAwaiter().GetResult())
                        {
                            _logger.Warning("Tor is already running.");
                            GetOnionEndPoint(toolsDir);
                            return true;
                        }

                        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            torPath = $@"{toolsDir}/Tor/tor";
                        }
                        else // If Windows
                        {
                            torPath = $@"{toolsDir}\Tor\tor.exe";
                        }

                    if (!File.Exists(torPath))
                        {
                            _logger.Error($"Tor instance NOT found at {torPath}. Attempting to acquire it...");
                            InstallTor(toolsDir);

                            //copy _configuration
                            CopyDefaultConfig(toolsDir);

                            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                var fulToolsDir = Path.Combine(_configuration.FullBaseDirectory, toolsDir);

                                // Make sure there's sufficient permission.
                                string chmodTorDirCmd = $"chmod -R 750 {fulToolsDir}";
                                var result = EnvironmentHelpers.ShellExec(chmodTorDirCmd);
                                if (result > 0)
                                {
                                    _logger.Error($"Command: {chmodTorDirCmd} exited with exit code: {result}, instead of 0.");
                                }
                                else
                                {
                                    _logger.Information($"Shell command executed: {chmodTorDirCmd}.");
                                }
                            }
                        }
                        else
                        {
                            _logger.Information($"Tor instance found at {torPath}.");
                        }

                        string torArguments = $" -f torrc";
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            _torProcess = Process.Start(new ProcessStartInfo
                            {
                                WorkingDirectory = $@"{toolsDir}/Tor/",
                                FileName = torPath,
                                Arguments = torArguments,
                                UseShellExecute = false,
                                CreateNoWindow = false,
                                RedirectStandardOutput = false
                            });
                            _logger.Information($"Starting Tor process with Process.Start.");
                        }
                        else // Linux and OSX
                        {
                            var fulToolsDir = Path.Combine(_configuration.FullBaseDirectory, toolsDir);

                            string runTorCmd = $"LD_LIBRARY_PATH=$LD_LIBRARY_PATH:={fulToolsDir}/Tor && export LD_LIBRARY_PATH && cd {fulToolsDir}/Tor && ./tor {torArguments}";
                            EnvironmentHelpers.ShellExec(runTorCmd, false);
                            _logger.Information($"Started Tor process with shell command: {runTorCmd}.");
                        }

                        //check if TOR is online
                        Task.Delay(3000).ConfigureAwait(false).GetAwaiter().GetResult(); // dotnet brainfart, ConfigureAwait(false) IS NEEDED HERE otherwise (only on) Manjuro Linux fails, WTF?!!
                        
                        if (!IsTorRunningAsync(_configuration.TorEndPoint).GetAwaiter().GetResult())
                        {
                            
                            throw new TorException("Attempted to start Tor, but it is not running.");
                        }
                        else
                        {
                            
                            GetOnionEndPoint(toolsDir);
                        }

                        _logger.Information("Tor is running.");
                    }
                    catch (Exception ex)
                    {
                        throw new TorException("Could not automatically start Tor. Try running Tor manually.", ex);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message + " " + ex.StackTrace);

                    return false;
                }
            //}).Start();

            return true;
        }

        private void CopyDefaultConfig(string torDir)
        {
            string torConfigDir = Path.Combine(_configuration.FullBaseDirectory, torDir);
            string sourceConfig = Path.Combine(torConfigDir, "torrc-default");
            string targetConfig = Path.Combine(torConfigDir, "Tor", "torrc");

            File.Copy(sourceConfig, targetConfig);
        }

        private void GetOnionEndPoint(string torDir)
        {
            string torHiddenServiceDir = Path.Combine(_configuration.FullBaseDirectory, torDir, "Tor", "hidden_service");

            if (Directory.Exists(torHiddenServiceDir))
            {
                _logger.Information($"Tor instance found at {torHiddenServiceDir}. Attempting to acquire it...");

                var hostNameFile = Path.Combine(torHiddenServiceDir, "hostname");

                try
                {
                    var onionEndPoint = File.ReadAllText(hostNameFile);
                    this.TorOnionEndPoint = onionEndPoint.Replace(Environment.NewLine, string.Empty);
                }
                catch (Exception)
                {
                    _logger.Error($"Tor onion endpoint cant be loaded from {hostNameFile}.");
                }
            }
            else
            {
                _logger.Error($"Tor instance not found at {torHiddenServiceDir}.");
            }
        }

        private void InstallTor(string torDir)
        {
            string torDaemonsDir = Path.Combine(_configuration.FullBaseDirectory, torDir);

            string dataZip = Path.Combine(torDaemonsDir, "data-folder.zip");
            IoHelpers.BetterExtractZipToDirectoryAsync(dataZip, torDaemonsDir).GetAwaiter().GetResult();
            _logger.Information($"Extracted {dataZip} to {torDaemonsDir}.");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string torWinZip = Path.Combine(torDaemonsDir, "tor-win32.zip");
                IoHelpers.BetterExtractZipToDirectoryAsync(torWinZip, torDaemonsDir).GetAwaiter().GetResult();
                _logger.Information($"Extracted {torWinZip} to {torDaemonsDir}.");
            }
            else // Linux or OSX
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    string torLinuxZip = Path.Combine(torDaemonsDir, "tor-linux64.zip");
                    IoHelpers.BetterExtractZipToDirectoryAsync(torLinuxZip, torDaemonsDir).GetAwaiter().GetResult();
                    _logger.Information($"Extracted {torLinuxZip} to {torDaemonsDir}.");
                }
                else // OSX
                {
                    string torOsxZip = Path.Combine(torDaemonsDir, "tor-osx64.zip");
                    IoHelpers.BetterExtractZipToDirectoryAsync(torOsxZip, torDaemonsDir).GetAwaiter().GetResult();
                    _logger.Information($"Extracted {torOsxZip} to {torDaemonsDir}.");
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
            if (_configuration.TorEndPoint is null)
            {
                return false;
            }

            using (var client = new TorSocks5Client(_configuration.TorEndPoint))
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

        public async Task<bool> IsOnionSeedRunningAsync(string url, int port)
        {
            if (_configuration.TorEndPoint is null)
            {
                return false;
            }

            using (var client = new TorSocks5Client(_configuration.TorEndPoint))
            {
                try
                {
                    await client.ConnectAsync().ConfigureAwait(false);
                    await client.HandshakeAsync().ConfigureAwait(false);
                    await client.ConnectToDestinationAsync(url, port).ConfigureAwait(false);
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

            if (_configuration.TorEndPoint is null)
            {
                Interlocked.Exchange(ref running, 3);
            }

            _stop?.Cancel();
            while (Interlocked.CompareExchange(ref running, 3, 0) == 2)
            {
                await Task.Delay(50).ConfigureAwait(false);
            }
            _stop?.Dispose();
            _stop = null;
            _torProcess?.Kill();
            _torProcess?.Dispose();
            _torProcess = null;

            _logger.Information("Tor stopped.");
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }
    }
}

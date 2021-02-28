using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FreeMarketOne.Tor.TorOverTcp.Models.Messages;
using FreeMarketOne.Extensions.Helpers;
using Serilog;

namespace FreeMarketOne.Tor
{
    public class TotServer
	{
		public TcpListener TcpListener { get; }
		private AsyncLock InitLock { get; }

		private Task AcceptTcpClientsTask { get; set; }

		private List<TotClient> Clients { get; }
		private AsyncLock ClientsLock { get; }

		public event EventHandler<TotRequest> RequestArrived;
		private void OnRequestArrived(TotClient client, TotRequest request) => RequestArrived?.Invoke(client, request);
		private ILogger _logger { get; set; }

		public TotServer(IPEndPoint bindToEndPoint)
		{
			_logger = Log.Logger.ForContext<TotServer>();
			Guard.NotNull(nameof(bindToEndPoint), bindToEndPoint);

			InitLock = new AsyncLock();

			using (InitLock.Lock())
			{
				TcpListener = new TcpListener(bindToEndPoint);

				ClientsLock = new AsyncLock();

				using (ClientsLock.Lock())
				{
					Clients = new List<TotClient>();
				}

				AcceptTcpClientsTask = null;
			}
		}

		public async Task StartAsync()
		{
			using (await InitLock.LockAsync().ConfigureAwait(false))
			{
				TcpListener.Start();

				AcceptTcpClientsTask = AcceptTcpClientsAsync();

				_logger.Information("Server started.");
			}
		}

		private async Task<TotClient> AcceptTcpClientsAsync()
		{
			while (true)
			{
				try
				{
					var tcpClient = await TcpListener.AcceptTcpClientAsync().ConfigureAwait(false); // TcpListener.Stop() will trigger ObjectDisposedException
					var totClient = new TotClient(tcpClient);

					await totClient.StartAsync().ConfigureAwait(false);
					totClient.RequestArrived += TotClient_RequestArrived;
					using (await ClientsLock.LockAsync().ConfigureAwait(false))
					{
						Clients.Add(totClient);
					}
				}
				catch (ObjectDisposedException ex)
				{
					// If TcpListener.Stop() is called, this exception will be triggered.
					_logger.Information("Server stopped accepting incoming connections.");
					_logger.Error(ex, "Error in AcceptTcpClientsAsync");
					return null;

				}
				catch (Exception ex)
				{
					_logger.Error(ex, "Error in AcceptTcpClientsAsync");
				}
			}
		}

		private void TotClient_RequestArrived(object sender, TotRequest request) => OnRequestArrived(sender as TotClient, request);

		public async Task StopAsync()
		{

			try
			{
				using (await InitLock.LockAsync().ConfigureAwait(false))
				{
					TcpListener.Stop();

					if (AcceptTcpClientsTask != null)
					{
						await AcceptTcpClientsTask.ConfigureAwait(false);
					}

					using (await ClientsLock.LockAsync().ConfigureAwait(false))
					{
						foreach (var client in Clients)
						{
							client.RequestArrived -= TotClient_RequestArrived;
							await client.StopAsync().ConfigureAwait(false);
						}
					}
				}
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Error in StopAsync");
			}
			finally
			{
				_logger.Information("Server stopped.");
			}
		}
	}
}

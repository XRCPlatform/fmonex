using FreeMarketOne.Tor;
using NetMQ.Sockets;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Libplanet.Net
{
    public class TorClientPool
    {
        private List<PooledClient> _pool;
        private TorSocks5Manager _socks5Manager;
        private static readonly object poolLock = new object();
        private ILogger _logger;

        public TorClientPool(ILogger logger, TorSocks5Manager socks5Manager)
        {
            _logger = logger;
            _pool = new List<PooledClient>();
            _socks5Manager = socks5Manager;
        }

        public void Remove(PooledClient PooledClient)
        {
            lock (poolLock)
            {
                _pool.Remove(PooledClient);
                _logger.Information($"Removed pooled socket for address {PooledClient.Address} total usages:{PooledClient.TotalRents}, active:{PooledClient.ActiveRents}.");
                //PooledClient.Dispose();
            }
        }

        public async Task<PooledClient> Get(BoundPeer peer)
        {
            PooledClient poolItem;
            lock (poolLock)
            {
                var candidates = _pool
                    .Where(sw =>
                        (sw.Host.Equals(peer.EndPoint.Host) && sw.Port.Equals(peer.EndPoint.Port))
                            && sw.Available
                         )
                    .OrderByDescending(sw => sw.TimeLastRented);
                _logger.Information($"Found available pooled socket candidates #{candidates?.Count()} out of total {_pool.Count}");
                poolItem = candidates.FirstOrDefault();               
            }

            if (poolItem == null)
            {
                var client = await CreateClient(peer);
                poolItem = new PooledClient(client, peer.EndPoint.Host, peer.EndPoint.Port);
                poolItem.Rent();
                _pool.Add(poolItem);
                _logger.Verbose($"Built new PooledClient for {peer}");

            }
            else
            {
                poolItem.Rent();
            }
            _logger.Verbose($"Rented pooled tor socket for {peer} with TotalRents #{poolItem.TotalRents} socket created at {poolItem.TimeCreated}");

            return poolItem;
        }


        private async Task<TotClient> CreateClient(BoundPeer peer)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            _logger.Verbose($"Connecting to remote server {peer}");
            var socks5client = await _socks5Manager.EstablishTcpConnectionAsync(peer.EndPoint.Host, peer.EndPoint.Port);           

            var tcpClient = socks5client.TcpClient;

            var client = new TotClient(tcpClient, peer.EndPoint.Host, peer.EndPoint.Port);
            client.Disconnected += ClientDisconected;
            await client.StartAsync();

            _logger.Verbose($"Created client for:{peer} IsConnected:{socks5client.IsConnected} time taken :{sw.ElapsedMilliseconds} ms");
            return client;
        }

        private void ClientDisconected(object sender, Exception e)
        {
            var cl = (TotClient)sender;
            try
            {
                _logger.Verbose($"Client {cl.Host}{cl.Port} disconected. Error:{e}");
                int count2 = _pool.RemoveAll(c => c.Client?.TcpClient?.Client.RemoteEndPoint == cl?.TcpClient?.Client.RemoteEndPoint);
                _logger.Verbose($"Removed {count2} specific clients from pool.");
                //cl.TcpClient.Close();
                cl.Disconnected -= ClientDisconected;
                int count = _pool.RemoveAll(c => (bool)!c.Client?.TcpClient?.Connected);
                _logger.Verbose($"Removed {count} dead clients from pool.");
            }
            catch (Exception)
            {
                //swallow as if we can't close then we can't
            }          
        }

        public void Recycle(BoundPeer peer)
        {
            lock (poolLock)
            {
                _pool.RemoveAll(sw => (sw.Host.Equals(peer.EndPoint.Host) && sw.Port.Equals(peer.EndPoint.Port)));
                _logger.Information($"Removed all pooled clients for peer:{peer}.");
            }
       }

        public void ShutDown()
        {
            foreach (var client in _pool)
            {
                client.Dispose();
            }
            _pool.Clear();
            _logger.Information($"Removed all pooled clients.");
        }

        public void KillIfUnhealthy(PooledClient client)
        {
            if (client.KillIfUnhealthy())
            {
                Remove(client);
            }
        }

        public void Return(PooledClient client)
        {
            client.Return();
            _logger.Information($"Returned {client.Host}{client.Port} socket to pool [totalRents:{client.TotalRents} Created:{client.TimeCreated}");
        }
    }
}

using NetMQ.Sockets;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Libplanet.Net
{
    public class NetmqConnectionPool
    {
        private List<PooledDealerSocket> pool;
        private string _socks5Proxy;
        private readonly object poolLock = new object();
        private ILogger _logger;

        public NetmqConnectionPool(ILogger logger, string socks5Proxy)
        {
            _logger = logger;
            pool = new List<PooledDealerSocket>();
            _socks5Proxy = socks5Proxy ?? null;
        }

        public void RemoveDealerSocket(PooledDealerSocket pooledDealerSocket, bool dispose = true)
        {
            lock (poolLock)
            {
                pool.Remove(pooledDealerSocket);
                if (dispose)
                {
                    pooledDealerSocket.Dispose();
                }
            }
        }

        public PooledDealerSocket RentDealerSocket(BoundPeer peer, bool exclusive)
        {
            PooledDealerSocket poolItem;
            lock (poolLock)
            {
                int acceptableRentsCount = 5;
                if (exclusive)
                {
                    acceptableRentsCount = 1;
                }
                //using list instead of dictionary so that we can hold more than 1 socket open for the address for concurency
                var candidates = pool
                    .Where(
                        sw => sw.Address.Equals(peer.Address)
                        && sw.ActiveRents < acceptableRentsCount
                        && sw.Available
                         )
                    .OrderBy(sw => sw.ActiveRents);//lowest utilisations first

                _logger.Information($"Found available pooled socket candidates #{candidates?.Count()} out of total {pool.Count}");

                poolItem = candidates.FirstOrDefault();

                if (poolItem == null)
                {
                    poolItem = new PooledDealerSocket(peer.Address, new DealerSocket(ToNetMQAddress(peer)));
                    poolItem.Rent(exclusive);
                    pool.Add(poolItem);
                    _logger.Verbose($"Built new PooledDealerSocket for {peer.Address}");

                }
                else
                {
                    poolItem.Rent(exclusive);
                }
                _logger.Verbose($"Rented pooled dealer socket for {peer.Address} with TotalRents #{poolItem.TotalRents} shared exclusively {poolItem.Exclusive} socket created at {poolItem.TimeCreated}");
            }

            return poolItem;
        }

        public void Recycle(BoundPeer peer)
        {
            lock (poolLock)
            {
                var candidates = pool.Where(sw => sw.Address.Equals(peer.Address));
                foreach (var item in candidates)
                {
                    pool.Remove(item);
                }
            }
        }

        private string ToNetMQAddress(BoundPeer peer)
        {
            return string.IsNullOrEmpty(_socks5Proxy) ?
                $"tcp://{peer.EndPoint.Host}:{peer.EndPoint.Port}" :
                $"socks5://{_socks5Proxy};{peer.EndPoint.Host}:{peer.EndPoint.Port}";
        }

        public void ShutDown()
        {
            foreach (var dealer in pool)
            {
                dealer.Dispose();
            }
            pool.Clear();
        }

        public void KillIfUnhealthy(PooledDealerSocket pooledDealerSocket)
        {
            if (pooledDealerSocket.KillIfUnhealthy())
            {
                RemoveDealerSocket(pooledDealerSocket, false);
            }
        }

        public void Return(PooledDealerSocket pooledDealerSocket)
        {
            pooledDealerSocket.Return();
        }
    }
}

using FreeMarketOne.Tor;
using System;
using System.Threading;

namespace Libplanet.Net
{
    public class PooledClient
    {
        private TotClient _client;
        private string _host;
        private int _port;

        public PooledClient(TotClient client, string host, int port)
        {
            Client = client;
            Host = host;
            Port = port;
            TimeCreated = DateTime.Now;
        }

        public TotClient Client { get => _client; set => _client = value; }
        public string Host { get => _host; set => _host = value; }
        public int Port { get => _port; set => _port = value; }

        private long activeRents;
        private long totalRents;
        private bool available = true;
        private long timeouts;

        public DateTime TimeCreated { get; }
        public DateTime TimeLastRented { get; private set; }
        public Address Address { get; }
        public bool Exclusive { get; private set; }
        public long ActiveRents { get => Interlocked.Read(ref activeRents); }
        public long TotalRents { get => Interlocked.Read(ref totalRents); }
        public bool Available { get => available; }

        private bool rented = false;

        private static readonly object rentAvaliabilityLock = new object();
        
        public long Timeouts { get => timeouts; }

        public TotClient Rent()
        {
            lock (rentAvaliabilityLock)
            {
                if (!available)
                {
                    throw new PooledSocketUnavailableException("Socket is already exclusively rented to another context.");
                }
                available = false;
                rented = true;
                TimeLastRented = DateTime.Now;

            }
            Interlocked.Increment(ref activeRents);
            Interlocked.Increment(ref totalRents);

            return _client;
        }

        public void Return()
        {
            lock (rentAvaliabilityLock)
            {
                if (rented)//only reset if was in rented state, if double called will
                {
                    available = true;
                    Interlocked.Decrement(ref activeRents);
                }
            }
        }

        public void Dispose()
        {
            try
            {
                _client.TcpClient.Close();
            }
            catch (Exception)
            {
                //swallow
            }
        }

        internal bool KillIfUnhealthy()
        {
            if (Interlocked.Read(ref timeouts) > 1)
            {
                lock (rentAvaliabilityLock)
                {
                    available = false;
                }
                return true;
            }
            Interlocked.Increment(ref timeouts);
            return false;
        }
    }
}

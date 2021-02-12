using NetMQ.Sockets;
using System;
using System.Threading;

namespace Libplanet.Net
{
    public class PooledDealerSocket : IDisposable
    {
        private long activeRents;
        private long totalRents;
        private bool available = true;
        private long timeouts;

        public PooledDealerSocket(Address address,  DealerSocket socket)
        {
            Address = address;
            Socket = socket;
            TimeCreated = DateTime.Now;
        }

        public DateTime TimeCreated { get; }
        public Address Address { get; }
        public bool Exclusive { get; private set; }        
        public long ActiveRents { get => Interlocked.Read( ref activeRents); }
        public long TotalRents { get => Interlocked.Read(ref totalRents); }  
        public bool Available { get => available; }

        private readonly object rentAvaliabilityLock = new object();
        internal DealerSocket Socket { get; }
        public long Timeouts { get => timeouts;}

        public DealerSocket Rent(bool exclusive)
        {
            lock (rentAvaliabilityLock)
            {
                if (!available)
                {
                    throw new PooledSocketUnavailableException("Socket is already exclusively rented to another context.");
                }
                if (exclusive)
                {
                    available = false;
                    Exclusive = exclusive;
                }               
            }
            Interlocked.Increment(ref activeRents);
            Interlocked.Increment(ref totalRents);

            return Socket;
        }

        public void Return()
        {
            lock (rentAvaliabilityLock)
            {
                available = true;
                Exclusive = false;
                Interlocked.Exchange(ref timeouts, 0L);
            }
            Interlocked.Decrement(ref activeRents);
        }

        public void Dispose()
        {
            try
            {
                Socket.Close();
                Socket.Dispose();
            }
            catch (Exception)
            {
                //swallow
            }
        }

        internal bool KillIfUnhealthy()
        {
            if (Interlocked.Read(ref activeRents) <2 && Interlocked.Read(ref timeouts) >3)
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
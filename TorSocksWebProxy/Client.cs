﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Org.Mentalis.Proxy.Http;
using Org.Mentalis.Network.ProxySocket;

namespace TorSocksWebProxy
{
    public class Client : IWebProxy, IDisposable
    {
        private static readonly object locker = new object();
        private static List<ProxyListener> listeners;
        private static bool allowBypass;

        private ProxyListener GetListener(ProxyConfig config, bool allowBypass = true)
        {
            lock (locker)
            {
                Client.allowBypass = allowBypass;
                if (listeners == null)
                    listeners = new List<ProxyListener>();

                var listener = listeners.Where(x => x.Port == config.HttpPort).FirstOrDefault();

                if (listener == null)
                {
                    listener = new ProxyListener(config);
                    listener.Start();
                    listeners.Add(listener);
                }

                if (listener.Version != config.Version)
                    throw new Exception("Socks Version Mismatch for Port " + config.HttpPort);

                return listener;
            }
        }

        private ProxyConfig Config { get; set; }

        /// <summary>
        /// Creates a new SocksWebProxy
        /// </summary>
        /// <param name="config">Proxy settings</param>
        /// <param name="allowBypass">Whether to allow bypassing the proxy server. 
        /// The current implementation to allow bypassing the proxy server requiers elevated privileges. 
        /// If you want to use the library in an environment with limited privileges (like Azure Websites or Azure Webjobs), set allowBypass = false</param>
        /// <returns></returns>
        public Client(ProxyConfig config = null, bool allowBypass = true)
        {
            Config = config;
            GetListener(config, allowBypass);
        }

        private ICredentials cred = null;
        public ICredentials Credentials
        {
            get
            {
                return cred;
            }
            set
            {
                cred = value;
            }
        }

        public Uri GetProxy(Uri uri)
        {
            return new Uri("http://" + Config.HttpAddress + ":" + Config.HttpPort);
        }
        public IPAddress GetProxyIPAddress()
        {
            return Config.HttpAddress;
        }
        public int GetProxyPort()
        {
            return Config.HttpPort;
        }
        public ProxyListener GetListener()
        {
            return GetListener(Config);
        }

        /// <summary>
        /// Indicates whether to use the proxy server for the specified host.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public bool IsBypassed(Uri host)
        {
            if (allowBypass)
            {
                return !IsActive();
            }
            return false;
        }

        public bool IsActive()
        {
            var isSocksPortListening = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(x => x.Port == Config.SocksPort);
            return isSocksPortListening;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        public void Dispose()
        {
            if (!disposedValue)
            {
                disposedValue = true;
                var itemsToDispose = listeners;
                listeners = null;
                itemsToDispose?.ForEach(x => x?.Dispose());
            }
            GC.SuppressFinalize(this);
        }

        ~Client()
        {
            Dispose();
        }
        #endregion
    }
}

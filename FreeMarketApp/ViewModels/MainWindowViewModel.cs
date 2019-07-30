using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FreeMarketApp.ViewModels;
using FreeMarketApp.Views;
using TorSocksWebProxy;
using System.Net.Http;

namespace FreeMarketApp.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string caption;

        public string Caption
        {
            get => caption;
            set => this.RaiseAndSetIfChanged(ref caption, value);
        }

        public string Greeting = "Hello World!x";

        public MainWindowViewModel()
        {
            Task.Run(() =>
            {

                //  RunParallel(2, "https://check.torproject.org/");
            });

            //Task.Run(() =>
            //{
            //    while (true)
            //    {
            //        Caption = DateTimeOffset.Now.ToString();
            //        Thread.Sleep(1000);
            //    }
            //});


        }

        private int GetNextFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            return port;
        }

        private void GetNewTorIdentity()
        {
            // Connect to tor, get a new identity and drop existing circuits
            Socket server = null;
            try
            {
                //Authenticate using control password
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9051);
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.Connect(endPoint);
                server.Send(
                    Encoding.ASCII.GetBytes("AUTHENTICATE \"password\"" + Environment.NewLine));
                byte[] data = new byte[1024];
                int receivedDataLength = server.Receive(data);
                string stringData = Encoding.ASCII.GetString(data, 0, receivedDataLength);

                //Request a new Identity
                server.Send(Encoding.ASCII.GetBytes("SIGNAL NEWNYM" + Environment.NewLine));
                data = new byte[1024];
                receivedDataLength = server.Receive(data);
                stringData = Encoding.ASCII.GetString(data, 0, receivedDataLength);
                if (!stringData.Contains("250"))
                {
                    Caption = Caption + "Unable to signal new user to server." + Environment.NewLine;
                    server.Shutdown(SocketShutdown.Both);
                    server.Close();
                }
                else
                {
                    Caption = Caption + "SIGNAL NEWNYM sent successfully" + Environment.NewLine;
                }

                //Enable circuit events to enable console output
                server.Send(Encoding.ASCII.GetBytes("setevents circ" + Environment.NewLine));
                data = new byte[1024];
                receivedDataLength = server.Receive(data);
                stringData = Encoding.ASCII.GetString(data, 0, receivedDataLength);

                //Get circuit information
                server.Send(Encoding.ASCII.GetBytes("getinfo circuit-status" + Environment.NewLine));
                data = new byte[16384];
                receivedDataLength = server.Receive(data);
                stringData = Encoding.ASCII.GetString(data, 0, receivedDataLength);
                stringData = stringData.Substring(stringData.IndexOf("250+circuit-status"),
                    stringData.IndexOf("250 OK") - stringData.IndexOf("250+circuit-status"));
                var stringArray = stringData.Split(new[] { "\r\n" }, StringSplitOptions.None);
                foreach (var s in stringArray)
                {
                    if (s.Contains("BUILT"))
                    {
                        //Close any existing circuit in order to get a new IP
                        var circuit = s.Substring(0, s.IndexOf("BUILT")).Trim();
                        server.Send(
                            Encoding.ASCII.GetBytes($"closecircuit {circuit}" + Environment.NewLine));
                        data = new byte[1024];
                        receivedDataLength = server.Receive(data);
                        stringData = Encoding.ASCII.GetString(data, 0, receivedDataLength);
                    }
                }
            }
            finally
            {
                server.Shutdown(SocketShutdown.Both);
                server.Close();
            }
        }

        private void RunParallel(int count, string url)
        {
            var locker = new object();

            for (int i = 0; i < count; i++)
            {
                if (i != 0)
                {
                    Thread.Sleep(2000);
                }

                var proxy = new TorSocksWebProxy.Client(new ProxyConfig(
                    //This is an internal http->socks proxy that runs in process
                    IPAddress.Parse("127.0.0.1"),
                    //This is the port your in process http->socks proxy will run on
                    GetNextFreePort(),
                    //This could be an address to a local socks proxy (ex: Tor / Tor Browser, If Tor is running it will be on 127.0.0.1)
                    IPAddress.Parse("127.0.0.1"),
                    //This is the port that the socks proxy lives on (ex: Tor / Tor Browser, Tor is 9150)
                    9050,
                    //This Can be Socks4 or Socks5
                    ProxyConfig.SocksVersion.Five
                    ));

                HttpClientHandler hch = new HttpClientHandler();
                hch.Proxy = proxy;
                hch.UseProxy = true;
                var client = new HttpClient(hch);
                var result = client.GetAsync(url).Result;
                var html = result.Content.ReadAsStringAsync().Result;

                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);
                var nodes = doc.DocumentNode.SelectNodes("//p/strong");

                foreach (var node in nodes)
                {
                    try
                    {
                        if (IPAddress.TryParse(node.InnerText, out IPAddress ip))
                        {
                            Caption = Caption + i + ":::::::::::::::::::::" + Environment.NewLine;
                            Caption = Caption + "" + Environment.NewLine;
                            if (html.Contains("Congratulations. This browser is configured to use Tor."))
                                Caption = Caption + "Connected through Tor with IP: " + ip.ToString() + Environment.NewLine;
                            else
                                Caption = Caption + "Not connected through Tor with IP: " + ip.ToString() + Environment.NewLine;

                            Caption = Caption + "" + Environment.NewLine;
                            Caption = Caption + i + ":::::::::::::::::::::" + Environment.NewLine;
                        }
                        else
                        {
                            Caption = Caption + i + ":::::::::::::::::::::" + Environment.NewLine;
                            Caption = Caption + "" + Environment.NewLine;
                            Caption = Caption + "IP not found" + Environment.NewLine;
                            Caption = Caption + "" + Environment.NewLine;
                            Caption = Caption + i + ":::::::::::::::::::::" + Environment.NewLine;
                        }

                        GetNewTorIdentity();
                    }
                    catch { }
                }
            }
        }

    }

}

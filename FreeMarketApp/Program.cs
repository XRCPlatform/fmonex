using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Logging.Serilog;
using FreeMarketApp.ViewModels;
using FreeMarketApp.Views;
using FreeMarketOne.ServerCore;
using Org.Mentalis.Network.ProxySocket;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using TorSocksWebProxy;

namespace FreeMarketApp
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args) => BuildAvaloniaApp().Start(AppMain, args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .SetExitMode(ExitMode.OnMainWindowClose)
                .LogToDebug()
                .UseReactiveUI();

        // Your application's entry point. Here you can initialize your MVVM framework, DI
        // container, etc.
        private static void AppMain(Application app, string[] args)
        {
            //var window = new MainWindow
            //{
            //    Content new MainWindowViewModel(),
            //    DataContext = new MainWindowViewModel(),
            //};

            //((MainWindowViewModel)window.DataContext).Greeting = "test";
            //    RunParallel(2, "https://check.torproject.org/");

            Console.WriteLine("loading Storage-Providers:");
            jaindb.jDB.loadPlugins(); //never forget this step !
            Console.WriteLine("");

            // jaindb.jDB.ClearExpired("fmdb", 637002592197784114);
            //jaindb.jDB.Reset();

            ////Genereate a test JSON
            string sJson = "{ \"prop1\": \"id1\", \"prop2\" : \"bla bla1\" }";

            //////Store JSON and set Object-Identifier to "OBJ1"
            // string sHash = jaindb.jDB.UploadFull(sJson, "fmdb2");

            ////Genereate a test JSON
            //sJson = "{ \"prop1\": \"id2\", \"prop2\" : \"bla bla2\" }";

            //////Store JSON and set Object-Identifier to "OBJ1"
            //sHash = jaindb.jDB.UploadFull(sJson, "fmdb");

            ////Genereate a test JSON
            //sJson = "{ \"prop1\": \"id3\", \"prop2\" : \"bla bla3\" }";

            //////Store JSON and set Object-Identifier to "OBJ1"
            //sHash = jaindb.jDB.UploadFull(sJson, "fmdb");

            ////Genereate a test JSON
            //sJson = "{ \"prop1\": \"id4\", \"prop2\" : \"bla bla4\" }";

            //////Store JSON and set Object-Identifier to "OBJ1"
            //sHash = jaindb.jDB.UploadFull(sJson, "fmdb");

            //////Get OBJ1 back from JainDB
            //var jObj1 = jaindb.jDB.GetFull("fmdb", 0);
            //var jObj2 = jaindb.jDB.GetFull("fmdb", 1);

            // jaindb.jDB.LookupID("#name", "Object1");

            ////Add an additional key Attribute
            //jObj1.Add("#name", "Object1");

            ////Upload JSON again
            //string sHash2 = jaindb.jDB.UploadFull(jObj1.ToString(), "OBJ1");

            ////Get OBJ1 back from JainDB by using the key #name
            //string sID = jaindb.jDB.LookupID("#name", "Object1");
            //var jObj2 = jaindb.jDB.GetFull(sID);
            //Console.WriteLine(jObj2.ToString(Newtonsoft.Json.Formatting.Indented));



            var window = new MainWindow();
            window.DataContext = new MainWindowViewModel();
            ((MainWindowViewModel)window.DataContext).Caption = "tesxt";
            ((MainWindowViewModel)window.DataContext).Greeting = "test";

            ////// wait until the user presses enter
            ////Console.WriteLine("");
            ////Console.WriteLine("Press enter to continue...");
            ////Console.ReadLine();

            Task.Run(() =>
            {

                TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 27272);
                listener.Start();
                var x = listener.LocalEndpoint.ToString();

                while (true)
                {
                    Console.WriteLine("Waiting for a client to connect...");
                    // Application blocks while waiting for an incoming connection.
                    // Type CNTL-C to terminate the server.
                    Socket client = listener.AcceptSocket();

                    if (client.Connected)
                    {
                        //To read from socket create NetworkStream object associated with socket
                        // var serverStream = new NetworkStream(client);

                        byte[] b = new byte[65535];
                        int k = client.Receive(b);
                        Console.WriteLine("Received:");
                        for (int i = 0; i < k; i++)
                            Console.Write(Convert.ToChar(b[i]));
                        ASCIIEncoding enc = new ASCIIEncoding();
                        client.Send(enc.GetBytes("Server responded"));
                        Console.WriteLine("\nSent Response");
                        client.Close();

                        //List<byte> inputStr = new List<byte>();

                        //int asw = 0;
                        //while (asw != -1)
                        //{
                        //    asw = serverStream.ReadByte();
                        //    inputStr.Add((Byte)asw);
                        //}

                        //var reply = Encoding.UTF8.GetString(inputStr.ToArray());
                        //serverStream.Close();

                        }

                    //NetworkStream networkStream = new NetworkStream(client);
                    ////System.IO.StreamWriter streamWriter =
                    ////new System.IO.StreamWriter(networkStream);
                    //System.IO.StreamReader streamReader =
                    //new System.IO.StreamReader(networkStream);
                    ////string theString = "Sending";
                    ////streamWriter.WriteLine(theString);
                    ////Console.WriteLine(theString);
                    ////streamWriter.Flush();
                    //var theString = streamReader.ReadLine();
                    ////Console.WriteLine(theString);
                    //streamReader.Close();
                    //networkStream.Close();
                    ////streamWriter.Close();

                    //var s = Encoding.ASCII.GetBytes("test");
                    //client.Send(s);
                   //var stream = client..GetStream();

                    //byte[] message = Encoding.UTF8.GetBytes("Hello from the server.<EOF>");
                    Console.WriteLine("Sending hello message.");
                    //stream.Write(message);
                }

                //new TcpClient { Client = socket };

                //sslStream = new SslStream(client.GetStream(), false);

                //var certificate = "fm.one.pfx";
                //SslTcpServer.RunServer(certificate, out Endpoint);
                //var listener = new TcpListener(IPAddress.Loopback, 0);
                //listener.Start();
                //var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                //listener.Stop();
                Socket server = null;

                var proxy = new TorSocksWebProxy.Client(new ProxyConfig(
                    //This is an internal http->socks proxy that runs in process
                    IPAddress.Parse("127.0.0.1"),
                    //This is the port your in process http->socks proxy will run on
                    27272,
                    //This could be an address to a local socks proxy (ex: Tor / Tor Browser, If Tor is running it will be on 127.0.0.1)
                    IPAddress.Parse("127.0.0.1"),
                    //This is the port that the socks proxy lives on (ex: Tor / Tor Browser, Tor is 9150)
                    9050,
                    //This Can be Socks4 or Socks5
                    ProxyConfig.SocksVersion.Five
                    ));

                //IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27272);
                //server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //server.Connect("uufjjzcy4v3hi5hkyvdh6lkxmafvedm3dsiyko3zefetq53prsinrhyd.onion", 80);
                //server.Send(
                //    Encoding.ASCII.GetBytes("AUTHENTICATE \"fmtest\"" + Environment.NewLine));
                //byte[] data = new byte[1024];
                //int receivedDataLength = server.Receive(data);
                //string stringData = Encoding.ASCII.GetString(data, 0, receivedDataLength);
                //server.Close();

                //proxy.GetListener().OnAccept

                //var endPoint = new IPEndPoint(proxy.GetProxyIPAddress(), proxy.GetProxyPort());

                //var tcp = new TcpClient(endPoint);

                //var result = tcp.GetAsync(url).Result;
                //var html = result.Content.ReadAsStringAsync().Result;

                //var doc = new HtmlAgilityPack.HtmlDocument();
                //doc.LoadHtml(html);
                //var nodes = doc.DocumentNode.SelectNodes("//p/strong");

                //foreach (var node in nodes)
                //{
                //    try
                //    {
                //        if (IPAddress.TryParse(node.InnerText, out IPAddress ip))
                //        {
                //            Caption = Caption + i + ":::::::::::::::::::::" + Environment.NewLine;
                //            Caption = Caption + "" + Environment.NewLine;
                //            if (html.Contains("Congratulations. This browser is configured to use Tor."))
                //                Caption = Caption + "Connected through Tor with IP: " + ip.ToString() + Environment.NewLine;
                //            else
                //                Caption = Caption + "Not connected through Tor with IP: " + ip.ToString() + Environment.NewLine;

                //            Caption = Caption + "" + Environment.NewLine;
                //            Caption = Caption + i + ":::::::::::::::::::::" + Environment.NewLine;
                //        }
                //        else
                //        {
                //            Caption = Caption + i + ":::::::::::::::::::::" + Environment.NewLine;
                //            Caption = Caption + "" + Environment.NewLine;
                //            Caption = Caption + "IP not found" + Environment.NewLine;
                //            Caption = Caption + "" + Environment.NewLine;
                //            Caption = Caption + i + ":::::::::::::::::::::" + Environment.NewLine;
                //        }

                //        GetNewTorIdentity();
                //    }
                //    catch { }
                //}

                //HttpClientHandler hch = new HttpClientHandler();
                //hch.Proxy = proxy;
                //hch.UseProxy = true;



            });


            FreeMarketOneServer.Current.Initialize();


            Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith((t) => {

                //sbyte y = new sbyte();

                //var socket = new ProxySocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, "fmtest");
                //socket.Connect("127.0.0.1", 9050);
                //socket.a
            //    ConnectViaSocks(9050, "uufjjzcy4v3hi5hkyvdh6lkxmafvedm3dsiyko3zefetq53prsinrhyd.onion", 80, ref y);
                //Console.WriteLine(Endpoint);
                //var certificate = "fm.one.cer";
                //SslTcpClient.RunClient(Endpoint, certificate);
            }).GetAwaiter();


            app.OnExit += OnExit;
            app.Run(window);

            //((MainWindowViewModel)window.DataContext).Caption = "tesxtXXX";
            //((MainWindowViewModel)window.DataContext).Greeting = "testCCCC";
        }

        public static Socket ConnectViaSocks(ushort Socksport, string RemoteAddress, ushort RemotePort, ref sbyte result)
        {
            Socket torsock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            torsock.Connect("127.0.0.1", Socksport);
            // Sending intial request : Authentication
            byte[] AuthReq = { 0x05, 0x01, 0x00 };
            torsock.Send(AuthReq);
            // Getting reply
            byte[] buffer = new byte[2];
            int received = torsock.Receive(buffer);
            if (buffer[1] != 0x00) { torsock.Close(); throw new Exception("SOCKS5 : Authentication error "); }
            // Sending Connect request to a domain (.onion domain)
            byte[] header = { 0x05, 0x01, 0x00, 0x03 };
            byte DomainLength = (byte)RemoteAddress.Length;
            byte[] DomainName = Encoding.ASCII.GetBytes(RemoteAddress);
            byte[] ConnRequest = new byte[4 + 1 + DomainLength + 2]; //Request format = {0x05 0x01 0x00 0x03 Domainlength(1 byte) DomainNmame(Variable Bytes) portNo(2 bytes) }
            System.Buffer.BlockCopy(header, 0, ConnRequest, 0, header.Length);
            ConnRequest[header.Length] = DomainLength;
            System.Buffer.BlockCopy(DomainName, 0, ConnRequest, header.Length + 1, DomainName.Length);
            ConnRequest[header.Length + 1 + DomainName.Length] = (byte)(RemotePort >> 8);
            ConnRequest[header.Length + 1 + DomainName.Length + 1] = (byte)(RemotePort & 255);

            torsock.Send(ConnRequest);
            byte[] buffer2 = new byte[10];
            received = torsock.Receive(buffer2);
            if (buffer2[1] != 0x00) { torsock.Close(); }
            result = (sbyte)buffer2[1];
            return torsock;
        }

        static void ProcessClient(TcpClient client)
        {
            var s = "true";
        }

        private static void OnExit(object sender, EventArgs e)
        {
            FreeMarketOneServer.Current.Stop();
        }
    }
}


using FreeMarketOne.DataStructure.Chat;
using FreeMarketOne.Extensions.Helpers;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace ChatTest
{
    class Program
    {
        public const int RequestTimeout = 5000;

        static void Main(string[] args)
        {
            StartMQListener();

            Console.WriteLine("Hello World!");
            Console.ReadKey();

            Timer timer = new Timer(TimeSpan.FromSeconds(10).TotalMilliseconds);
            timer.Elapsed += OnTick;
            timer.Start();


            Console.ReadKey();
        }

        private static void OnTick(object source, ElapsedEventArgs e)
        {
            SendNQMessage();
        }

        public static void StartMQListener()
        {
            Task.Run(() =>
            {
                var endPoint = EndPointHelper.ParseIPEndPoint("tcp://0.0.0.0:9110/").ToString();
                var connectionString = string.Format("tcp://{0}", endPoint);

                using (var response = new ResponseSocket())
                {
                    response.Options.Linger = TimeSpan.Zero;
                    Console.WriteLine("Chat listener binding {0}", connectionString);
                    response.Bind(connectionString);

                    while (true)
                    {
                        var clientMessage = response.ReceiveMultipartMessage(4);

                        Console.WriteLine("Receiving chat message from peer.");

                        var receivedChatItem = new ChatMessage(clientMessage);

                        Console.WriteLine(receivedChatItem.DateCreated);
                        Console.WriteLine(receivedChatItem.Message);
                        Console.WriteLine(receivedChatItem.ExtraMessage);                        

                        response.SendMultipartMessage(clientMessage);
                    }

                }
            });
        }

        private static bool SendNQMessage()
        {
            Console.WriteLine("Sending...");

            var connectionString = string.Format("tcp://localhost:9110"); //here to set target IP in case of test

            using (var client = new RequestSocket())
            {
                try
                {
                    var chatMessage = new ChatMessage();
                    chatMessage.Message = "test";
                    chatMessage.ExtraMessage = "extramessage";
                    chatMessage.DateCreated = DateTime.UtcNow;
                    chatMessage.Hash = "xxsxsxasxas";

                    client.Connect(connectionString);

                    client.SendMultipartMessage(chatMessage.ToNetMQMessage());
                    client.ReceiveReady += ClientOnReceiveReady;
                    bool pollResult = client.Poll(TimeSpan.FromMilliseconds(RequestTimeout));
                    client.ReceiveReady -= ClientOnReceiveReady;
                    client.Disconnect(connectionString);

                    return pollResult;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }
            }

            return false;
        }

        private static void ClientOnReceiveReady(object sender, NetMQSocketEventArgs args)
        {
            Console.WriteLine("Server replied ({0})", args.Socket.ReceiveFrameString());
        }
    }
}

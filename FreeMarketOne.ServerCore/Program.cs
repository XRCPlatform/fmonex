using System;
using System.Threading;
using CommandLine;
using FreeMarketOne.Users;

namespace FreeMarketOne.ServerCore
{
    class Program
    {
        private static FreeMarketOneServer server;
        private static CancellationTokenSource cancellationTokenSource;

        public class Options
        {
            [Option('c', "config", Required = true, HelpText="Path to config file")]
            public string ConfigFile { get; set; }

            [Option('p', "password", Required = false, HelpText="Password for user")]
            public string Password { get; set; }
        }

        static void Main(string[] args)
        {
            cancellationTokenSource = new CancellationTokenSource();
            Start(args);
        }

        public static void Start(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(HandleShutdown);
            var cancellationToken = cancellationTokenSource.Token;
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    Console.WriteLine("FreeMarket One Server");
                    Console.WriteLine("=====================");
                    Console.WriteLine();
                    var password = o.Password == null ? "" : o.Password;
                    var userManager = new UserManager(FreeMarketOneServer.Current.MakeConfiguration(o.ConfigFile));
                    while (password == "" || (userManager.Initialize(password, null) != Users.UserManager.PrivateKeyStates.Valid))
                    {
                        Console.Write("Password: ");
                        password = Console.ReadLine();
                    }

                    FreeMarketOneServer.Current.Initialize(password, null, o.ConfigFile);
                    FreeMarketOneServer.Current.LoadingEvent += new EventHandler<string>(OnLoadingEvent);
                    while (!cancellationToken.IsCancellationRequested)
                        Thread.Sleep(2000);
                });
        }

        static void OnLoadingEvent(object sender, string message)
        {
            Console.WriteLine(message);
        }

        static void HandleShutdown(object sender, ConsoleCancelEventArgs args)
        {
            Console.WriteLine("Shutting down...");
            args.Cancel = true;
            cancellationTokenSource.Cancel();
            FreeMarketOneServer.Current.Stop();

        }
    }
}

using System;
using System.IO;
using System.Threading;
using CommandLine;
using FreeMarketOne.Users;

namespace FreeMarketOne.ServerCore
{
    class Program
    {
        private static FreeMarketOneServer server;
        private static CancellationTokenSource cancellationTokenSource;
        
        static void Main(string[] args)
        {
            cancellationTokenSource = new CancellationTokenSource();
            Start(args);
        }

        public static void Start(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(HandleShutdown);
            var cancellationToken = cancellationTokenSource.Token;
            CommandLineOptions.Parse(args, o =>
                {
                    Console.WriteLine("FreeMarket One Server");
                    Console.WriteLine("=====================");
                    Console.WriteLine();
                    var password = o.Password ?? "";
                    // Hack to remove: if -c is set, set -d. -d is now preferred
                    if (o.ConfigFile != null)
                    {
                        o.DataDir = Path.GetDirectoryName(Path.GetFullPath(o.ConfigFile));
                        o.ConfigFile = Path.GetFileName(o.ConfigFile);
                    }

                    FreeMarketOneServer.Current.DataDir = o.DataDir != null ? 
                        new DataDir(o.DataDir, o.ConfigFile) : 
                        new DataDir();

                    var userManager = new UserManager(FreeMarketOneServer.Current.MakeConfiguration());
                    while (password == "" || (userManager.Initialize(password, null) != Users.UserManager.PrivateKeyStates.Valid))
                    {
                        Console.Write("Password: ");
                        password = Console.ReadLine();
                    }

                    FreeMarketOneServer.Current.Initialize(password, null);
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

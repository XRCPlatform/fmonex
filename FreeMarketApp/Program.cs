using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using FreeMarketOne.ServerCore;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketApp
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Free Market One - World precious metals marketplace;");
            Console.WriteLine("Copyright (c) 2020 www.freemarket.one;");
            Console.WriteLine("This program comes with ABSOLUTELY NO WARRANTY;");
            Console.WriteLine("This is free software, and you are welcome to redistribute it;");
            Console.WriteLine("------------------------------------");
            
            CommandLineOptions.Parse(args, o =>
            {
                if (o.ConfigFile != null)
                {
                    o.DataDir = Path.GetDirectoryName(Path.GetFullPath(o.ConfigFile));
                    o.ConfigFile = Path.GetFileName(o.ConfigFile);
                }
                FMONE.Current.DataDir = o.DataDir != null ? 
                    new DataDir(o.DataDir, o.ConfigFile) : 
                    new DataDir();
            });

            BuildAvaloniaApp()
               .StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
    }
}


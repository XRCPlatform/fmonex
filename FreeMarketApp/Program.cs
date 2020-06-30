using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;

namespace FreeMarketApp
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("FREEMARKET.ONE desktop application;");
            Console.WriteLine("Copyright (c) 2020 www.freemarket.one;");
            Console.WriteLine("This program comes with ABSOLUTELY NO WARRANTY;");
            Console.WriteLine("This is free software, and you are welcome to redistribute it;");
            Console.WriteLine("------------------------------------");

            BuildAvaloniaApp()
               .StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug()
                .UseReactiveUI();
    }
}


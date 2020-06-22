using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using FreeMarketApp.Views;
using FreeMarketOne.ServerCore;
using FreeMarketOne.ServerCore.ViewModels;

namespace FreeMarketApp
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("FREEMARKET.ONE desktop application;");
            Console.WriteLine("Copyright (c) 2020 www.freemarket.one;");
            Console.WriteLine("This program comes with ABSOLUTELY NO WARRANTY;");
            Console.WriteLine("This is free software, and you are welcome to redistribute it under GNU GPL license;");
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


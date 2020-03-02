using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging.Serilog;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Views;
using FreeMarketOne.ServerCore;
using System;

namespace FreeMarketApp
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
                desktop.Exit += OnExit;
                desktop.Startup += OnStart;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static void OnStart(object sender, EventArgs e)
        {
            FreeMarketOneServer.Current.Initialize();
        }

        private static void OnExit(object sender, EventArgs e)
        {
            FreeMarketOneServer.Current.Stop();
        }
    }
}

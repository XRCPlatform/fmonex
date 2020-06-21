using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging.Serilog;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Views;
using FreeMarketOne.ServerCore;
using FreeMarketOne.ServerCore.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;

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
                 desktop.Exit += OnExit;
                 desktop.Startup += OnStart;
            }

            base.OnFrameworkInitializationCompleted();
        }

        static async Task<MainWindow> GetAppLoadingAsync(SplashWindowViewModel splashViewModel)
        {
            FreeMarketOneServer.Current.Initialize(splashViewModel);

            return new MainWindow { DataContext = new MainWindowViewModel() };
        }

        private static void OnStart(object sender, EventArgs e)
        {
            if (sender is IClassicDesktopStyleApplicationLifetime desktop)
            {
                async void AppAsyncLoadingStart()
                {
                    await Task.Delay(10);
                    var splashViewModel = new SplashWindowViewModel();
                    splashViewModel.StartupProgressText = "Loading of applciation";
                    var splash = new SplashWindow { DataContext = splashViewModel };
                    splash.Show();

                    await Task.Delay(1000);
                    desktop.MainWindow = await GetAppLoadingAsync(splashViewModel);
                    desktop.MainWindow.Show();
                    desktop.MainWindow.Activate();

                    await Task.Delay(1000);
                    splash.Close();
                }
                AppAsyncLoadingStart();
            }
        }

        private static void OnExit(object sender, EventArgs e)
        {
            FreeMarketOneServer.Current.Stop();
        }
    }
}

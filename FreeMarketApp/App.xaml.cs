using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FreeMarketApp.ViewModels;
using FreeMarketApp.Views;
using FreeMarketOne.ServerCore;
using System;
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

        private static void OnStart(object sender, EventArgs e)
        {
            if (sender is IClassicDesktopStyleApplicationLifetime desktop)
            {
                async void AppAsyncLoadingStart()
                {
                    var splashViewModel = new SplashWindowViewModel();
                    splashViewModel.StartupProgressText = "Loading...";
                    var splashWindow = new SplashWindow { DataContext = splashViewModel };
                    splashWindow.Show();
                    await Task.Delay(10);

                    desktop.MainWindow = await GetAppLoadingAsync();
                    desktop.MainWindow.Show();
                    desktop.MainWindow.Activate();

                    if (splashWindow != null)
                    {
                        splashWindow.Close();
                    }
                }

                AppAsyncLoadingStart();
            }
        }

        private static async Task<MainWindow> GetAppLoadingAsync()
        {
            await Task.Run(() =>
            {
                FreeMarketOneServer.Current.FreeMarketOneServerLoadedEvent += ServerLoadedEvent;
                FreeMarketOneServer.Current.Initialize();
            }).ConfigureAwait(true);

            return new MainWindow { DataContext = new MainWindowViewModel() };
        }

        private static void ServerLoadedEvent(object sender, EventArgs e)
        {
            //do activities after load;
        }

        private static void OnExit(object sender, EventArgs e)
        {
            FreeMarketOneServer.Current.Stop();
        }
    }
}

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
        private static event EventHandler SplashMessageEventHandler;
        private static SplashWindow SplashWindow;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            SplashMessageEventHandler += new EventHandler(SplashMessageEvent);
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
                    SplashMessageEventHandler?.Invoke("Loading...", EventArgs.Empty);
                    await Task.Delay(100);

                    desktop.MainWindow = await GetAppLoadingAsync(null);
                    desktop.MainWindow.Show();
                    desktop.MainWindow.Activate();
                    await Task.Delay(1000);

                    if (SplashWindow != null)
                    {
                        await Task.Delay(100);
                        SplashWindow.Close();
                    }
                }

                AppAsyncLoadingStart();
            }
        }

        private static void SplashMessageEvent(object message, EventArgs e)
        {
            if (SplashWindow == null)
            {
                var splashViewModel = new SplashWindowViewModel();
                splashViewModel.StartupProgressText = (string)message;
                SplashWindow = new SplashWindow { DataContext = splashViewModel };
                SplashWindow.Show();
            }
            else
            {
                ((SplashWindowViewModel)SplashWindow.DataContext).StartupProgressText = (string)message;
            }
        }

        private static async Task<MainWindow> GetAppLoadingAsync(SplashWindowViewModel splashViewModel)
        {
            FreeMarketOneServer.Current.Initialize(SplashMessageEventHandler);

            return new MainWindow { DataContext = new MainWindowViewModel() };
        }

        private static void OnExit(object sender, EventArgs e)
        {
            FreeMarketOneServer.Current.Stop();
        }
    }
}

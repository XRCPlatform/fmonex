using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.ViewModels;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.ServerCore;
using System.Text;
using System.Threading.Tasks;

namespace FreeMarketApp.Views.Pages
{
    public class LoginPage : UserControl
    {
        private static LoginPage _instance;
        public static LoginPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new LoginPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public LoginPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public async void ButtonLogin_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            var tbPassword = this.FindControl<TextBox>("TBPassword");

            var errorCount = 0;
            var errorMessages = new StringBuilder();

            if (string.IsNullOrEmpty(tbPassword.Text) || tbPassword.Text.Length < 16)
            {
                errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_LoginPage_ShortPassword"));
                errorCount++;
            }
            else
            {
                if (!FreeMarketOneServer.Current.UserManager.IsTextValid(tbPassword.Text))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_LoginPage_InvalidCharsPassword"));
                    errorCount++;
                }
            }

            if (errorCount == 0)
            {
                //reloading server with splash window
                async void AppAsyncLoadingStart()
                {
                    var splashViewModel = new SplashWindowViewModel();
                    splashViewModel.StartupProgressText = "Unlocking...";
                    var splashWindow = new SplashWindow { DataContext = splashViewModel };
                    splashWindow.Show();
                    await Task.Delay(10);

                    FreeMarketOneServer.Current.Initialize(tbPassword.Text);
                    
                    if (FreeMarketOneServer.Current.UserManager.PrivateKeyState != UserManager.PrivateKeyStates.WrongPassword) {
                        PagesHelper.Switch(mainWindow, MainPage.Instance);
                        PagesHelper.UnlockTools(mainWindow, true);
                        PagesHelper.SetUserData(mainWindow);
                    } 
                    else
                    {
                        tbPassword.Text = string.Empty;
                        tbPassword.Watermark = SharedResources.ResourceManager.GetString("Dialog_LoginPage_WatermarkWrongPassword");
                    }
                    
                    if (splashWindow != null)
                    {
                        splashWindow.Close();
                    }
                }

                AppAsyncLoadingStart();
            }
            else
            {
                await MessageBox.Show(mainWindow,
                   errorMessages.ToString(),
                    SharedResources.ResourceManager.GetString("Dialog_Information_Title"),
                    MessageBox.MessageBoxButtons.Ok);
            }
        }
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.ViewModels;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Users;
using Serilog;
using System;
using System.Text;
using System.Threading.Tasks;
using static FreeMarketOne.Users.UserManager;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketApp.Views.Pages
{
    public class LoginPage : UserControl
    {
        private static LoginPage _instance;
        private static MainWindowViewModel mainViewModel;
        private ILogger _logger;

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
        public static LoginPage GetInstance()
        {
            return _instance;
        }

        public LoginPage()
        {
            if (FMONE.Current.Logger != null)
                _logger = FMONE.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(LoginPage).Namespace, typeof(LoginPage).Name));

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
            var tbError = this.FindControl<TextBlock>("TBError");

            var errorCount = 0;
            var errorMessages = new StringBuilder();
            var textHelper = new TextHelper();

            if (string.IsNullOrEmpty(tbPassword.Text) || tbPassword.Text.Length < 16)
            {
                errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_LoginPage_ShortPassword"));
                errorCount++;
            }
            else
            {
                if (!textHelper.IsTextNotDangerous(tbPassword.Text))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_LoginPage_InvalidCharsPassword"));
                    errorCount++;
                }
            }

            if (errorCount == 0)
            {
                //reloading server with splash window
                async Task<PrivateKeyStates> AppAsyncLoadingStart()
                {
                    PagesHelper.Switch(mainWindow, LoadingPage.Instance);
                    mainViewModel = new MainWindowViewModel();
                    mainViewModel.StartupProgressText = "Unlocking...";
                    mainWindow.DataContext = mainViewModel;
                    await Task.Delay(10);

                    var result = await GetAppLoadingAsync(tbPassword.Text);

                    if (result == UserManager.PrivateKeyStates.Valid)
                    {
           

                        // if (FMONE.Current.UserManager.PrivateKeyState == UserManager.PrivateKeyStates.Valid) {
                        PagesHelper.Switch(mainWindow, MainPage.Instance);
                        PagesHelper.UnlockTools(mainWindow, true);
                        PagesHelper.SetUserData(_logger, mainWindow);
                    } 
                    else
                    {
                        PagesHelper.Switch(mainWindow, LoginPage.Instance);
                        tbPassword.Text = string.Empty;
                        tbError.Text = SharedResources.ResourceManager.GetString("Dialog_LoginPage_WatermarkWrongPassword");
                    }
                    return result;
                }
                await AppAsyncLoadingStart();
            }
            else
            {
                await MessageBox.Show(mainWindow,
                   errorMessages.ToString(),
                    SharedResources.ResourceManager.GetString("Dialog_Information_Title"),
                    MessageBox.MessageBoxButtons.Ok);
            }
        }

        private static async Task<PrivateKeyStates> GetAppLoadingAsync(string password)
        {
            return await Task.Run(async () =>
            {
                FMONE.Current.LoadingEvent += new EventHandler<string>(LoadingEvent);
                return await FMONE.Current.InitializeAsync(password);
            });
        }

        private static void LoadingEvent(object sender, string message)
        {
            mainViewModel.StartupProgressText = message;
        }
    }
}

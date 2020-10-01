using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.ViewModels;
using FreeMarketApp.Views.Controls;
using Serilog;
using System.Text;
using System.Threading.Tasks;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketApp.Views.Pages
{
    public class FirstRunPage : UserControl
    {
        private static FirstRunPage _instance;
        private ILogger _logger;

        public static FirstRunPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new FirstRunPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
        public static FirstRunPage GetInstance()
        {
            return _instance;
        }

        public FirstRunPage()
        {
            if (FMONE.Current.Logger != null)
                _logger = FMONE.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(FirstRunPage).Namespace, typeof(FirstRunPage).Name));

            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public async void ButtonSave_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            ////check form
            var tbUserName = this.FindControl<TextBox>("TBUserName");
            var tbDescription = this.FindControl<TextBox>("TBDescription");
            var tbPassword = this.FindControl<TextBox>("TBPassword");
            var tbPasswordVerify = this.FindControl<TextBox>("TBPasswordVerify");
            var tbSeed = this.FindControl<TextBox>("TBSeed");

            var errorCount = 0;
            var errorMessages = new StringBuilder();
            var textHelper = new TextHelper();

            if (string.IsNullOrEmpty(tbUserName.Text) || (tbUserName.Text.Length < 10))
            {
                errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_ShortUserName"));
                errorCount++;
            }
            else
            {
                if (!textHelper.IsTextValid(tbUserName.Text))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_InvalidCharsUserName"));
                    errorCount++;
                }
            }

            if (string.IsNullOrEmpty(tbDescription.Text) || (tbDescription.Text.Length < 50))
            {
                errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_ShortDescription"));
                errorCount++;
            }
            else
            {
                if (!textHelper.IsTextValid(tbDescription.Text, true))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_InvalidCharsDescription"));
                    errorCount++;
                }
            }

            if (string.IsNullOrEmpty(tbPassword.Text) || string.IsNullOrEmpty(tbPasswordVerify.Text) || tbPassword.Text.Length < 16)
            {
                errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_ShortPassword"));
                errorCount++;
            }
            else
            {
                if (!textHelper.IsTextValid(tbPassword.Text, true))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_InvalidCharsPassword"));
                    errorCount++;
                }
            }
            if (tbPassword.Text != tbPasswordVerify.Text)
            {
                errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_PasswordNotIdentical"));
                errorCount++;
            }

            if (string.IsNullOrEmpty(tbSeed.Text) || tbSeed.Text.Length < 200)
            {
                errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_ShortSeed"));
                errorCount++;
            }
            else
            {
                if (!textHelper.IsTextValid(tbSeed.Text))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_InvalidCharsSeed"));
                    errorCount++;
                }
            }

            if (errorCount == 0)
            {
                FMONE.Current.Users.SaveNewPrivKey(
                    tbSeed.Text, 
                    tbPassword.Text,
                    FMONE.Current.Configuration.FullBaseDirectory,
                    FMONE.Current.Configuration.BlockChainSecretPath);
                
                var firstUserData = FMONE.Current.Users.SignUserData(tbUserName.Text, tbDescription.Text);

                FMONE.Current.Users.SaveUserData(
                    firstUserData,
                    FMONE.Current.Configuration.FullBaseDirectory,
                    FMONE.Current.Configuration.BlockChainUserPath);

                //reloading server with splash window
                async void AppAsyncLoadingStart()
                {
                    var splashViewModel = new SplashWindowViewModel();
                    splashViewModel.StartupProgressText = "Reloading...";
                    var splashWindow = new SplashWindow { DataContext = splashViewModel };
                    splashWindow.Show();
                    await Task.Delay(10);

                    FMONE.Current.Initialize(tbPassword.Text, firstUserData);
                    PagesHelper.Switch(mainWindow, MainPage.Instance);
                    PagesHelper.UnlockTools(mainWindow, true);

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

        public void ButtonRandomSeed_Click(object sender, RoutedEventArgs args)
        {
            var tbSeed = this.FindControl<TextBox>("TBSeed");
            tbSeed.Text = FMONE.Current.Users.CreateRandomSeed();
        }
    }
}

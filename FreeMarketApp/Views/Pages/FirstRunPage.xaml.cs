﻿using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.ViewModels;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using Serilog;
using System;
using System.Text;
using TextCopy;
using System.Threading.Tasks;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketApp.Views.Pages
{
    public class FirstRunPage : UserControl
    {
        private static FirstRunPage _instance;
        private static MainWindowViewModel mainViewModel;
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
                if (!textHelper.IsCleanTextValid(tbUserName.Text))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_InvalidCharsUserName"));
                    errorCount++;
                }
            }

            if (string.IsNullOrEmpty(tbDescription.Text) || (tbDescription.Text.Length < 30))
            {
                errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_ShortDescription"));
                errorCount++;
            }
            else
            {
                if (!textHelper.IsTextNotDangerous(tbDescription.Text))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_InvalidCharsDescription"));
                    errorCount++;
                }

                if (!textHelper.IsWithoutBannedWords(tbDescription.Text))
                {
                    errorMessages.AppendLine(
                        string.Format("{0}: {1}",
                        SharedResources.ResourceManager.GetString("Dialog_FirstRun_BannedWordsDescription"), 
                        TextHelper.WORDS_FILTER.Replace(" ,", ", ")));
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
                if (!textHelper.IsTextNotDangerous(tbPassword.Text))
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
                if (!textHelper.IsCleanTextValid(tbSeed.Text))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_InvalidCharsSeed"));
                    errorCount++;
                }
            }

            if (errorCount == 0)
            {
                FMONE.Current.UserManager.SaveNewPrivKey(
                    tbSeed.Text,
                    tbPassword.Text,
                    FMONE.Current.Configuration.FullBaseDirectory,
                    FMONE.Current.Configuration.BlockChainSecretPath);

                var firstUserData = FMONE.Current.UserManager.SignUserData(tbUserName.Text, tbDescription.Text);

                FMONE.Current.UserManager.SaveUserData(
                    firstUserData,
                    FMONE.Current.Configuration.FullBaseDirectory,
                    FMONE.Current.Configuration.BlockChainUserPath);

                //reloading server with splash window
                async void AppAsyncLoadingStart()
                {
                    PagesHelper.Switch(mainWindow, LoadingPage.Instance);
                    mainViewModel = new MainWindowViewModel();
                    mainViewModel.StartupProgressText = "Reloading...";
                    mainWindow.DataContext = mainViewModel;
                    await Task.Delay(10);
                    await GetAppLoadingAsync(tbPassword.Text, firstUserData);

                    PagesHelper.Switch(mainWindow, MainPage.Instance);
                    PagesHelper.UnlockTools(mainWindow, true);
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
            tbSeed.Text = FMONE.Current.UserManager.CreateRandomSeed();
        }

        private static async Task<bool> GetAppLoadingAsync(string password, UserDataV1 userData)
        {
            await Task.Run(async () =>
            {
                FMONE.Current.LoadingEvent += new EventHandler<string>(LoadingEvent);
                await FMONE.Current.InitializeAsync(password, userData);
            }).ConfigureAwait(true);

            return true;
        }

        public async void ButtonCopyToClipboard_Click(object sender, RoutedEventArgs args)
        {
            var tbSeed = this.FindControl<TextBox>("TBSeed");
            if ((tbSeed != null) && (!string.IsNullOrEmpty(tbSeed.Text)))
            {
                try
                {
                    await ClipboardService.SetTextAsync(tbSeed.Text);
                }
                catch (Exception e)
                {
                    PagesHelper.Log(Instance._logger,
                        string.Format("Isn't possible to use clipboard {0}", e.Message),
                        Serilog.Events.LogEventLevel.Error);
                }
            }
        }

        private static void LoadingEvent(object sender, string message)
        {
            mainViewModel.StartupProgressText = message;
        }
    }
}

﻿using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.ViewModels;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore;
using Serilog;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

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

        public FirstRunPage()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
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

            if (!PagesHelper.IsTextValid(tbUserName.Text))
            {
                errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_InvalidCharsUserName"));
                errorCount++;
            }

            if (!PagesHelper.IsTextValid(tbDescription.Text, true))
            {
                errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_InvalidCharsDescription"));
                errorCount++;
            }

            if (string.IsNullOrEmpty(tbPassword.Text) || string.IsNullOrEmpty(tbPasswordVerify.Text) || tbPassword.Text.Length < 16)
            {
                errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_ShortPassword"));
                errorCount++;
            }
            else
            {
                if (!PagesHelper.IsTextValid(tbPassword.Text, true))
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
                if (!PagesHelper.IsTextValid(tbSeed.Text))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_InvalidCharsSeed"));
                    errorCount++;
                }
            }

            if (errorCount == 0)
            {
                FreeMarketOneServer.Current.UserManager.SaveNewPrivKey(
                    tbSeed.Text, 
                    tbPassword.Text, 
                    FreeMarketOneServer.Current.Configuration.FullBaseDirectory,
                    FreeMarketOneServer.Current.Configuration.BlockChainSecretPath);
                
                var firstUserData = GenerateUserData(tbUserName.Text, tbDescription.Text);

                FreeMarketOneServer.Current.UserManager.SaveUserData(
                    firstUserData,
                    FreeMarketOneServer.Current.Configuration.FullBaseDirectory,
                    FreeMarketOneServer.Current.Configuration.BlockChainUserPath);

                //reloading server with splash window
                async void AppAsyncLoadingStart()
                {
                    var splashViewModel = new SplashWindowViewModel();
                    splashViewModel.StartupProgressText = "Reloading...";
                    var splashWindow = new SplashWindow { DataContext = splashViewModel };
                    splashWindow.Show();
                    await Task.Delay(10);

                    FreeMarketOneServer.Current.Initialize(tbPassword.Text, firstUserData);
                    PagesHelper.Switch(mainWindow, MainPage.Instance);
                    PagesHelper.UnlockTools(mainWindow, true);
                    PagesHelper.SetUserData(mainWindow);

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
            tbSeed.Text = FreeMarketOneServer.Current.UserManager.CreateRandomSeed();
        }

        /// <summary>
        /// Saving data to blockchain
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="description"></param>
        private UserDataV1 GenerateUserData(string userName, string description)
        {
            var newUser = new UserDataV1();
            newUser.UserName = userName;
            newUser.Description = description;
            var bytesToSign = newUser.ToByteArrayForSign();

            newUser.Signature = Convert.ToBase64String(FreeMarketOneServer.Current.UserManager.PrivateKey.Sign(bytesToSign));
            
            newUser.Hash = newUser.GenerateHash();

            return newUser;
        }
    }
}

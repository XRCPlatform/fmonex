using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore;
using Serilog;
using System;
using System.Text;

namespace FreeMarketApp.Views.Pages
{
    public class EditProfilePage : UserControl
    {
        private static EditProfilePage _instance;
        private ILogger _logger;

        public static EditProfilePage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new EditProfilePage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public EditProfilePage()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(EditProfilePage).Namespace, typeof(EditProfilePage).Name));

            this.InitializeComponent();

            var userData = FreeMarketOneServer.Current.UserManager.UserData;

            var tbUserName = this.FindControl<TextBox>("TBUserName");
            var tbDescription = this.FindControl<TextBox>("TBDescription");

            tbUserName.Text = userData.UserName;
            tbDescription.Text = userData.Description;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonBack_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyProfilePage.Instance);

            ClearForm();
        }

        public void ButtonMyReviews_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyReviewsPage.Instance);

            ClearForm();
        }

        public void ButtonMyProfile_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyProfilePage.Instance);

            ClearForm();
        }

        public void ButtonCancel_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyProfilePage.Instance);

            ClearForm();
        }

        public async void ButtonSave_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            ////check form
            var tbUserName = this.FindControl<TextBox>("TBUserName");
            var tbDescription = this.FindControl<TextBox>("TBDescription");
            var userData = FreeMarketOneServer.Current.UserManager.UserData;

            var errorCount = 0;
            var errorMessages = new StringBuilder();

            if (!FreeMarketOneServer.Current.UserManager.IsTextValid(tbUserName.Text))
            {
                errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_InvalidCharsUserName"));
                errorCount++;
            }

            if (!FreeMarketOneServer.Current.UserManager.IsTextValid(tbDescription.Text))
            {
                errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_InvalidCharsDescription"));
                errorCount++;
            }

            if (errorCount == 0)
            {
                var updatedUserData = GenerateUserData(tbUserName.Text, tbDescription.Text, userData);

                FreeMarketOneServer.Current.UserManager.SaveUserData(
                    updatedUserData,
                    FreeMarketOneServer.Current.Configuration.FullBaseDirectory,
                    FreeMarketOneServer.Current.Configuration.BlockChainUserPath);

                PagesHelper.SetUserData(mainWindow);

                FreeMarketOneServer.Current.BasePoolManager.AcceptActionItem(updatedUserData);
                FreeMarketOneServer.Current.BasePoolManager.PropagateAllActionItemLocal();

                ClearForm();
            }
            else
            {
                await MessageBox.Show(mainWindow,
                   errorMessages.ToString(),
                    SharedResources.ResourceManager.GetString("Dialog_Information_Title"),
                    MessageBox.MessageBoxButtons.Ok);
            }
        }

        private UserDataV1 GenerateUserData(string userName, string description, UserDataV1 userData)
        {
            userData.UserName = userName;
            userData.Description = description;
            var bytesToSign = userData.ToByteArrayForSign();

            userData.Signature = Convert.ToBase64String(FreeMarketOneServer.Current.UserManager.PrivateKey.Sign(bytesToSign));

            userData.Hash = userData.GenerateHash();

            return userData;
        }

        private void ClearForm()
        {
            _instance = null;
        }
    }
}

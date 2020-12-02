using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using FreeMarketApp.Helpers;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Skynet;
using Serilog;
using System;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketApp.Views.Pages
{
    public class MyProfilePage : UserControl
    {
        private static MyProfilePage _instance;
        private UserDataV1 _userData;
        private ILogger _logger;

        public static MyProfilePage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MyProfilePage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
        public static MyProfilePage GetInstance()
        {
            return _instance;
        }

        public MyProfilePage()
        {
            if (FMONE.Current.Logger != null)
                _logger = FMONE.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(MyProfilePage).Namespace, typeof(MyProfilePage).Name));

            this.InitializeComponent();

            if (FMONE.Current.Users != null)
            {
                PagesHelper.Log(_logger, string.Format("Loading user data of current user to profile page."));

                _userData = FMONE.Current.Users.UserData;

                var tbUserName = this.FindControl<TextBlock>("TBUserName");
                var tbDescription = this.FindControl<TextBlock>("TBDescription");
                var tbReviewStars = this.FindControl<TextBlock>("TBReviewStars");

                tbUserName.Text = _userData.UserName;
                tbDescription.Text = _userData.Description;

                if (!string.IsNullOrEmpty(_userData.Photo) && (_userData.Photo.Contains(SkynetWebPortal.SKYNET_PREFIX)))
                {
                    var iPhoto = this.FindControl<Image>("IPhoto");

                    var skynetHelper = new SkynetHelper();
                    var skynetStream = skynetHelper.DownloadFromSkynet(_userData.Photo, _logger);
                    if (skynetStream != null) iPhoto.Source = new Bitmap(skynetStream);
                }

                var userPubKey = FMONE.Current.Users.GetCurrentUserPublicKey();
                var reviews = FMONE.Current.SearchEngine.GetAllReviewsForPubKey(userPubKey);

                var reviewStars = FMONE.Current.Users.GetUserReviewStars(reviews);
                var reviewStartRounded = Math.Round(reviewStars, 1, MidpointRounding.AwayFromZero);
                tbReviewStars.Text = reviewStartRounded.ToString();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonBack_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MainPage.Instance);

            ClearForm();
        }

        public void ButtonMyReviews_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyReviewsPage.Instance);

            ClearForm();
        }

        public void ButtonEdit_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, EditProfilePage.Instance);

            ClearForm();
        }

        private void ClearForm()
        {
            _instance = null;
        }
    }
}

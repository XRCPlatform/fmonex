using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using DynamicData;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.ViewModels;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore;
using FreeMarketOne.Skynet;
using Libplanet.Extensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FreeMarketApp.Views.Pages
{
    public class PublicProfilePage : UserControl
    {
        private static PublicProfilePage _instance;
        private ILogger _logger;
        private UserDataV1 _userData;
        private UserControl _returnToInstanceOfPage;

        public static PublicProfilePage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PublicProfilePage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public static PublicProfilePage GetInstance()
        {
            return _instance;
        }

        public void SetReturnTo(UserControl page)
        {
            Instance._returnToInstanceOfPage = page;
        }

        public PublicProfilePage()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(PublicProfilePage).Namespace, typeof(PublicProfilePage).Name));

            this.InitializeComponent();

            DataContext = new PublicProfileReviewsViewModel(new List<ReviewUserDataV1>());
        }

        public void LoadUser(string signature, string hash)
        {
            var userData = FreeMarketOneServer.Current.UserManager.GetUserDataBySignatureAndHash(
                signature, hash, FreeMarketOneServer.Current.BasePoolManager, FreeMarketOneServer.Current.BaseBlockChainManager);

            if (userData != null)
            {
                _userData = userData;

                PagesHelper.Log(Instance._logger, string.Format("Loading public profile of user signature {0} hash {1}", userData.Signature, userData.Hash));

                var tbUserName = Instance.FindControl<TextBlock>("TBUserName");
                var tbDescription = Instance.FindControl<TextBlock>("TBDescription");
                var tbStars = Instance.FindControl<TextBlock>("TBStars");

                tbUserName.Text = userData.UserName;
                tbDescription.Text = userData.Description;

                var userBytes = userData.ToByteArrayForSign();
                var userPubKeys = UserPublicKey.Recover(userBytes, userData.Signature);

                var reviews = FreeMarketOneServer.Current.UserManager.GetAllReviewsForPubKey(
                    userPubKeys,
                    FreeMarketOneServer.Current.BasePoolManager,
                    FreeMarketOneServer.Current.BaseBlockChainManager);

                if (reviews.Any())
                {
                    var reviewStars = FreeMarketOneServer.Current.UserManager.GetUserReviewStars(reviews);
                    var reviewStartRounded = Math.Round(reviewStars, 1, MidpointRounding.AwayFromZero);

                    tbStars.Text = reviewStartRounded.ToString();

                    GetAllUserNames(reviews);

                    ((PublicProfileReviewsViewModel)DataContext).Items.AddRange(reviews);
                }

                if (!string.IsNullOrEmpty(_userData.Photo) && (_userData.Photo.Contains(SkynetWebPortal.SKYNET_PREFIX)))
                {
                    var iPhoto = this.FindControl<Image>("IPhoto");

                    var skynetHelper = new SkynetHelper();
                    var skynetStream = skynetHelper.DownloadFromSkynet(_userData.Photo, _logger);
                    iPhoto.Source = new Bitmap(skynetStream);
                }
            }
        }

        private void GetAllUserNames(List<ReviewUserDataV1> reviews)
        {
            if (reviews.Any())
            {
                for (int i = 0; i < reviews.Count; i++)
                {
                    var itemReview = reviews[i];

                    var itemReviewBytes = itemReview.ToByteArrayForSign();
                    var reviewUserPubKeys = UserPublicKey.Recover(itemReviewBytes, itemReview.Signature);

                    var reviewUserData = FreeMarketOneServer.Current.UserManager.GetUserDataByPublicKey(
                        reviewUserPubKeys,
                        FreeMarketOneServer.Current.BasePoolManager,
                        FreeMarketOneServer.Current.BaseBlockChainManager);

                    if (reviewUserData != null)
                    {
                        reviews[i].UserName = reviewUserData.UserName;
                        reviews[i].UserSignatureAndHash = string.Format("{0}|{1}", reviewUserData.Signature, reviewUserData.Hash);
                    }
                    else
                    {
                        reviews[i].UserName = SharedResources.ResourceManager.GetString("UnknownValue");
                    }
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonBack_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, Instance._returnToInstanceOfPage);

            ClearForm();
        }

        private void ClearForm()
        {
            _instance = null;
        }
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.ViewModels;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using Libplanet.Extensions;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketApp.Views.Pages
{
    public class MyReviewsPage : UserControl
    {
        private static MyReviewsPage _instance;
        private ILogger _logger;

        public static MyReviewsPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MyReviewsPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public static MyReviewsPage GetInstance()
        {
            return _instance;
        }

        public MyReviewsPage()
        {
            if (FMONE.Current.Logger != null)
                _logger = FMONE.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(MyReviewsPage).Namespace, typeof(MyReviewsPage).Name));

            this.InitializeComponent();

            if (FMONE.Current.Users != null)
            {
                SpinWait.SpinUntil(() => FMONE.Current.GetServerState() == FMONE.FreeMarketOneServerStates.Online);

                PagesHelper.Log(_logger, string.Format("Loading user reviews from chain."));

                var userPubKey = FMONE.Current.Users.GetCurrentUserPublicKey();
                var reviews = FMONE.Current.Users.GetAllReviewsForPubKey(
                    userPubKey,
                    FMONE.Current.BasePoolManager,
                    FMONE.Current.BaseBlockChainManager);

                GetAllUserNames(reviews);

                DataContext = new MyReviewsViewModel(reviews);
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

        public void ButtonMyProfile_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyProfilePage.Instance);

            ClearForm();
        }

        public void ButtonPublicProfile_Click(object sender, RoutedEventArgs args)
        {
            var signatureAndHash = ((Button)sender).Tag.ToString();

            if (!string.IsNullOrEmpty(signatureAndHash))
            {
                PagesHelper.Log(Instance._logger, string.Format("Loading public profile with array {0}", signatureAndHash));

                var mainWindow = PagesHelper.GetParentWindow(this);

                var publicProfilePage = PublicProfilePage.Instance;
                publicProfilePage.SetReturnTo(MyReviewsPage.Instance);

                var safeArrayHelper = new SafeArrayTransportHelper();
                var arrUserData = safeArrayHelper.GetArray(signatureAndHash);
                publicProfilePage.LoadUser(arrUserData[0], arrUserData[1]);

                PagesHelper.Switch(mainWindow, publicProfilePage);
            }
        }

        private void GetAllUserNames(List<ReviewUserDataV1> reviews)
        {
            if (reviews.Any())
            {
                var safeArrayHelper = new SafeArrayTransportHelper();

                for (int i = 0; i < reviews.Count; i++)
                {
                    var itemReview = reviews[i];

                    var itemReviewBytes = itemReview.ToByteArrayForSign();
                    var reviewUserPubKeys = UserPublicKey.Recover(itemReviewBytes, itemReview.Signature);

                    var reviewUserData = FMONE.Current.Users.GetUserDataByPublicKey(
                        reviewUserPubKeys,
                        FMONE.Current.BasePoolManager,
                        FMONE.Current.BaseBlockChainManager);

                    if (reviewUserData != null)
                    {
                        reviews[i].UserName = reviewUserData.UserName;
                        reviews[i].UserSignatureAndHash = safeArrayHelper.GetString(
                            new[] { reviewUserData.Signature , reviewUserData.Hash });
                    } 
                    else
                    {
                        reviews[i].UserName = SharedResources.ResourceManager.GetString("UnknownValue");
                    }
                }
            }
        }

        private void ClearForm()
        {
            _instance = null;
        }
    }
}

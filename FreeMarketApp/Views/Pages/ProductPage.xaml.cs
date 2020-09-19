using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketOne.ServerCore;
using Serilog;
using System;
using System.Linq;
using static FreeMarketOne.ServerCore.MarketManager;

namespace FreeMarketApp.Views.Pages
{
    public class ProductPage : UserControl
    {
        private static ProductPage _instance;
        private ILogger _logger;

        public static ProductPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ProductPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public static ProductPage GetInstance()
        {
            return _instance;
        }

        public ProductPage()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(ProductPage).Namespace, typeof(ProductPage).Name));

            this.InitializeComponent();
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

        public void ButtonSeller_Click(object sender, RoutedEventArgs args)
        {
            var signatureAndHash = ((Button)sender).Tag.ToString();

            if (!string.IsNullOrEmpty(signatureAndHash))
            {
                PagesHelper.Log(Instance._logger, string.Format("Loading public profile with array {0}", signatureAndHash));

                var mainWindow = PagesHelper.GetParentWindow(this);

                var publicProfilePage = PublicProfilePage.Instance;
                publicProfilePage.SetReturnTo(ProductPage.Instance);

                var signature = signatureAndHash.Split("|")[0];
                var hash = signatureAndHash.Split("|")[1];
                publicProfilePage.LoadUser(signature, hash);

                PagesHelper.Switch(mainWindow, publicProfilePage);

                ClearForm();
            }
        }

        public void LoadProduct(string signature)
        {
            var offer = FreeMarketOneServer.Current.MarketManager.GetOfferBySignature(signature);

            if (offer != null)
            {
                PagesHelper.Log(Instance._logger, string.Format("Loading detail of product signature {0}", offer.Signature));

                var tbTitle = Instance.FindControl<TextBlock>("TBTitle");
                var tbDescription = Instance.FindControl<TextBlock>("TBDescription");
                var tbShipping = Instance.FindControl<TextBlock>("TBShipping");
                var tbPrice = Instance.FindControl<TextBlock>("TBPrice");
                var tbPriceType = Instance.FindControl<TextBlock>("TBPriceType");
                var tbSeller = Instance.FindControl<TextBlock>("TBSeller");
                var tbSellerStars = Instance.FindControl<TextBlock>("TBSellerStars");
                var tbSellerReviewsCount = Instance.FindControl<TextBlock>("TBSellerReviewsCount");
                var btSeller = Instance.FindControl<Button>("BTSeller");

                tbTitle.Text = offer.Title;
                tbDescription.Text = offer.Description;
                tbShipping.Text = offer.Shipping;
                tbPrice.Text = offer.Price.ToString();
                tbPriceType.Text = ((ProductPriceTypeEnum)offer.PriceType).ToString();

                //seller userdata loading
                var userPubKey = FreeMarketOneServer.Current.MarketManager.GetSellerPubKeyFromMarketItem(offer);
                var userData = FreeMarketOneServer.Current.UserManager.GetUserDataByPublicKey(userPubKey);
                if (userData != null)
                {
                    tbSeller.Text = userData.UserName;
                    btSeller.Tag = string.Format("{0}|{1}", userData.Signature, userData.Hash);

                    var reviews = FreeMarketOneServer.Current.UserManager.GetAllReviewsForPubKey(userPubKey);
                    var reviewStars = FreeMarketOneServer.Current.UserManager.GetUserReviewStars(reviews);
                    var reviewStartRounded = Math.Round(reviewStars, 1, MidpointRounding.AwayFromZero);

                    tbSellerStars.Text = reviewStartRounded.ToString();
                    tbSellerReviewsCount.Text = reviews.Count().ToString();
                }

                //photos loading
                if ((offer.Photos != null) && (offer.Photos.Any()))
                {
                    SkynetHelper.PreloadPhotos(offer, Instance._logger);

                    for (int i = 0; i < offer.Photos.Count; i++)
                    {
                        var spPhoto = Instance.FindControl<StackPanel>("SPPhoto_" + i);
                        var iPhoto = Instance.FindControl<Image>("IPhoto_" + i);

                        spPhoto.IsVisible = true;
                        iPhoto.Source = offer.PrePhotos[i];
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

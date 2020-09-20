using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore;
using Libplanet.Extensions;
using Serilog;
using System;
using System.Linq;
using static FreeMarketApp.Views.Controls.MessageBox;
using static FreeMarketOne.ServerCore.MarketManager;

namespace FreeMarketApp.Views.Pages
{
    public class ProductPage : UserControl
    {
        private static ProductPage _instance;
        private ILogger _logger;
        private MarketItemV1 _offer;

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

        public async void ButtonBuy_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);
            var signature = ((Button)sender).Tag.ToString();

            var approxSpanToNewBlock = FreeMarketOneServer.Current.Configuration.BlockChainMarketPolicy.GetApproxTimeSpanToMineNextBlock();

            var result = await MessageBox.Show(mainWindow,
                string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_BuyProduct"), approxSpanToNewBlock.TotalSeconds),
                SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                MessageBox.MessageBoxButtons.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                //is it my offer?
                var itemReviewBytes = _offer.ToByteArrayForSign();
                var offerUserPubKeys = UserPublicKey.Recover(itemReviewBytes, _offer.Signature);
                var userPubKey = FreeMarketOneServer.Current.UserManager.GetCurrentUserPublicKey();

                foreach (var itemUserPubKey in offerUserPubKeys)
                {
                    if (userPubKey.SequenceEqual(itemUserPubKey))
                    {
                        await MessageBox.Show(mainWindow,
                            string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_YouCantBuyYourOffer")),
                            SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                            MessageBox.MessageBoxButtons.Ok);

                        break;
                    } 
                    else
                    {
                        //sign market data and generating chain connection
                        _offer = FreeMarketOneServer.Current.MarketManager.SignBuyerMarketData(_offer);

                        PagesHelper.Log(_logger, string.Format("Propagate bought information to chain."));

                        FreeMarketOneServer.Current.MarketPoolManager.AcceptActionItem(_offer);
                        FreeMarketOneServer.Current.MarketPoolManager.PropagateAllActionItemLocal();

                        await MessageBox.Show(mainWindow,
                            string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_Waiting")),
                            SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                            MessageBox.MessageBoxButtons.Ok);

                        var chatPage = ChatPage.Instance;
                        chatPage.LoadChatByProduct(_offer.Signature);

                        PagesHelper.Switch(mainWindow, chatPage);

                        ClearForm();

                        break;
                    }
                }
            }
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
                _offer = offer;

                PagesHelper.Log(Instance._logger, string.Format("Loading detail of product signature {0}", _offer.Signature));

                var tbTitle = Instance.FindControl<TextBlock>("TBTitle");
                var tbDescription = Instance.FindControl<TextBlock>("TBDescription");
                var tbShipping = Instance.FindControl<TextBlock>("TBShipping");
                var tbPrice = Instance.FindControl<TextBlock>("TBPrice");
                var tbPriceType = Instance.FindControl<TextBlock>("TBPriceType");
                var tbSeller = Instance.FindControl<TextBlock>("TBSeller");
                var tbSellerStars = Instance.FindControl<TextBlock>("TBSellerStars");
                var tbSellerReviewsCount = Instance.FindControl<TextBlock>("TBSellerReviewsCount");
                var btSeller = Instance.FindControl<Button>("BTSeller");
                var btBuyButton = Instance.FindControl<Button>("BTBuyButton");

                tbTitle.Text = _offer.Title;
                tbDescription.Text = _offer.Description;
                tbShipping.Text = _offer.Shipping;
                tbPrice.Text = _offer.Price.ToString();
                tbPriceType.Text = ((ProductPriceTypeEnum)_offer.PriceType).ToString();
                btBuyButton.Tag = _offer.Signature;

                //seller userdata loading
                var userPubKey = FreeMarketOneServer.Current.MarketManager.GetSellerPubKeyFromMarketItem(_offer);
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
                if ((_offer.Photos != null) && (_offer.Photos.Any()))
                {
                    SkynetHelper.PreloadPhotos(_offer, Instance._logger);

                    for (int i = 0; i < _offer.Photos.Count; i++)
                    {
                        var spPhoto = Instance.FindControl<StackPanel>("SPPhoto_" + i);
                        var iPhoto = Instance.FindControl<Image>("IPhoto_" + i);

                        spPhoto.IsVisible = true;
                        iPhoto.Source = _offer.PrePhotos[i];
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

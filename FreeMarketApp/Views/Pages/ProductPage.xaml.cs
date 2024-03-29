﻿using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Common;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Markets;
using FreeMarketOne.Pools;
using Libplanet.Extensions;
using Serilog;
using System;
using System.Linq;
using TextCopy;
using static FreeMarketOne.Markets.MarketManager;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketApp.Views.Pages
{
    public class ProductPage : UserControl
    {
        private static ProductPage _instance;
        private ILogger _logger;
        private MarketItemV1 _offer;
        private UserControl _backPage;
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
            if (FMONE.Current.Logger != null)
                _logger = FMONE.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
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
            if (_backPage != null)
            {
                PagesHelper.Switch(mainWindow, _backPage);
            }
            else
            {
                PagesHelper.Switch(mainWindow, MainPage.Instance);
            }


            ClearForm();
        }

        public void SetBackPage(MyBoughtProductsPage myBoughtProductsPage)
        {
            _backPage = (UserControl)myBoughtProductsPage;
        }

        public async void ButtonReview_Click(object sender, RoutedEventArgs args)
        {
            var textHelper = new TextHelper();
            var mainWindow = PagesHelper.GetParentWindow(this);
            var config = FMONE.Current.Configuration;
            var approxSpanToNewBlock = ((ExtendedConfiguration)config).BlockChainMarketPolicy.GetApproxTimeSpanToMineNextBlock();
            string reviewText = String.Empty;
            int stars = 0;

            if (_offer == null)
            {
                return;
            }

            var TBReviewText = Instance.FindControl<TextBox>("TBReviewText");
            var ReviewForm = Instance.FindControl<DockPanel>("ReviewForm");
            var StarReviewToggleButtonArray = Instance.FindControl<StackPanel>("StarReviewToggleButtonArray");
            foreach (var item in StarReviewToggleButtonArray.Children)
            {
                ToggleButton starButton = item as ToggleButton;
                if (starButton != null)
                {
                    if ((bool)starButton.IsChecked)
                    {
                        int observedStars = int.Parse(starButton.Tag.ToString());
                        if (observedStars > stars)
                        {
                            stars = observedStars;
                        }
                    }
                }
            }

            reviewText = TBReviewText.Text;


            var result = await MessageBox.Show(mainWindow,
              string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_ReviewProduct"), approxSpanToNewBlock.TotalSeconds),
              SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
              MessageBox.MessageBoxButtons.YesNo);

            if (result == MessageBox.MessageBoxResult.Yes)
            {

                //is it my offer?
                var itemReviewBytes = _offer.ToByteArrayForSign();
                var sellerPublicKeys = UserPublicKey.Recover(itemReviewBytes, _offer.Signature);
                var userPubKey = FMONE.Current.UserManager.GetCurrentUserPublicKey();
                var sellerUserData = FMONE.Current.UserManager.GetUserDataByPublicKey(sellerPublicKeys, FMONE.Current.BasePoolManager, FMONE.Current.BaseBlockChainManager);

                var isMine = false;

                foreach (var itemUserPubKey in sellerPublicKeys)
                {
                    if (userPubKey.SequenceEqual(itemUserPubKey))
                    {
                        isMine = true;
                        break;
                    }
                }
                if (!textHelper.IsWithoutBannedWords(reviewText))
                {
                    await MessageBox.Show(mainWindow,
                               string.Format(SharedResources.ResourceManager.GetString("Dialog_Review_BannedWordsDescription")),
                               SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                               MessageBox.MessageBoxButtons.Ok);
                    return;
                }

                if (isMine)
                {
                    await MessageBox.Show(mainWindow,
                                string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_YouCantBuyYourOffer")),
                                SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                                MessageBox.MessageBoxButtons.Ok);
                    return;
                }
                else
                {
                    if (String.IsNullOrEmpty(reviewText))
                    {
                        await MessageBox.Show(mainWindow,
                              string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_PleaseProvideReviewText")),
                              SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                              MessageBox.MessageBoxButtons.Ok);
                        return;
                    }

                    var review = new ReviewUserDataV1();
                    review.ReviewDateTime = DateTime.UtcNow;
                    review.Message = reviewText;
                    review.Stars = stars;
                    review.UserName = FMONE.Current.UserManager.UserData.UserName;
                    review.MarketItemHash = _offer.Hash;
                    review.RevieweePublicKey = sellerUserData.PublicKey;
                    review.Hash = review.GenerateHash();

                    ReviewUserDataV1 signedReview = FMONE.Current.UserManager.SignReviewData(review, FMONE.Current.UserManager.PrivateKey);

                    PagesHelper.Log(_logger, string.Format("Propagate review information to chain."));

                    var resultPool = FMONE.Current.BasePoolManager.AcceptActionItem(signedReview);
                    if (resultPool != null)
                    {
                        await MessageBox.Show(mainWindow,
                        string.Format(SharedResources.ResourceManager.GetString("Dialog_Error_" + resultPool.Value.ToString())),
                        SharedResources.ResourceManager.GetString("Dialog_Error_Title"),
                        MessageBox.MessageBoxButtons.Ok);

                        //not allow change in case of another state is in process
                        if (resultPool == PoolManagerStates.Errors.StateOfItemIsInProgress)
                        {
                            //TODO: not sure what todo here yet 
                        }
                    }
                }
            }
            ReviewForm.IsVisible = false;
        }

        public void ButtonStar_Click(object sender, RoutedEventArgs args)
        {

            int observedStars = int.Parse(((Button)sender).Tag.ToString());
            var StarReviewToggleButtonArray = Instance.FindControl<StackPanel>("StarReviewToggleButtonArray");
            foreach (var item in StarReviewToggleButtonArray.Children)
            {
                ToggleButton starButton = item as ToggleButton;
                if (starButton != null)
                {
                    int iterableStars = int.Parse(starButton.Tag.ToString());
                    if (iterableStars <= observedStars)
                    {
                        starButton.IsChecked = true;
                    }
                    else
                    {
                        starButton.IsChecked = false;
                    }
                }
            }
        }

        public void ButtonReviewReset_Click(object sender, RoutedEventArgs args)
        {
            var StarReviewToggleButtonArray = Instance.FindControl<StackPanel>("StarReviewToggleButtonArray");
            foreach (var item in StarReviewToggleButtonArray.Children)
            {
                ToggleButton starButton = item as ToggleButton;
                if (starButton != null)
                {
                    starButton.IsChecked = false;
                }
            }
            var TBReviewText = Instance.FindControl<TextBox>("TBReviewText");
            TBReviewText.Text = String.Empty;
        }

        public void ShowReview()
        {
            var ReviewForm = Instance.FindControl<DockPanel>("ReviewForm");
            ReviewForm.IsVisible = true;
        }

        public async void ButtonBuy_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);
            var config = (ExtendedConfiguration)FMONE.Current.Configuration;
            var approxSpanToNewBlock = config.BlockChainMarketPolicy.GetApproxTimeSpanToMineNextBlock();

            var tbXRCReceivingTransaction = Instance.FindControl<TextBox>("TBXRCReceivingTransaction");

            var result = await MessageBox.Show(mainWindow,
                string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_BuyProduct"), approxSpanToNewBlock.TotalSeconds),
                SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                MessageBox.MessageBoxButtons.YesNo);

            if (result == MessageBox.MessageBoxResult.Yes)
            {
                //is it my offer?
                var itemReviewBytes = _offer.ToByteArrayForSign();
                var offerUserPubKeys = UserPublicKey.Recover(itemReviewBytes, _offer.Signature);
                var userPubKey = FMONE.Current.UserManager.GetCurrentUserPublicKey();
                var isMine = false;

                foreach (var itemUserPubKey in offerUserPubKeys)
                {
                    if (userPubKey.SequenceEqual(itemUserPubKey))
                    {
                        isMine = true;
                        break;
                    }
                }

                if (isMine)
                {
                    await MessageBox.Show(mainWindow,
                                string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_YouCantBuyYourOffer")),
                                SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                                MessageBox.MessageBoxButtons.Ok);
                }
                else
                {
                    if (string.IsNullOrEmpty(tbXRCReceivingTransaction.Text) || (tbXRCReceivingTransaction.Text.Length != 64))
                    {
                        await MessageBox.Show(mainWindow,
                              string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_PleaseProvideXRCTransactionHash")),
                              SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                              MessageBox.MessageBoxButtons.Ok);
                        return;
                    }

                    _offer.XRCTransactionHash = tbXRCReceivingTransaction.Text;
                    //sign market data and generating chain connection
                    var signedOffer = FMONE.Current.MarketManager.SignBuyerMarketData(
                        _offer,
                        FMONE.Current.TorProcessManager.TorOnionEndPoint,
                        FMONE.Current.UserManager.PrivateKey);

                    _offer = signedOffer.Clone();

                    PagesHelper.Log(_logger, string.Format("Propagate bought information to chain."));

                    var resultPool = FMONE.Current.MarketPoolManager.AcceptActionItem(signedOffer);
                    if (resultPool == null)
                    {
                        //create a new chat
                        var newChat = FMONE.Current.ChatManager.CreateNewChat(signedOffer);
                        FMONE.Current.ChatManager.SaveChat(newChat);

                        await MessageBox.Show(mainWindow,
                            string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_Waiting")),
                            SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                            MessageBox.MessageBoxButtons.Ok);

                        var chatPage = ChatPage.Instance;
                        chatPage.LoadChatByProduct(signedOffer.Hash);

                        PagesHelper.Switch(mainWindow, chatPage);

                        ClearForm();
                    }
                    else
                    {
                        await MessageBox.Show(mainWindow,
                            string.Format(SharedResources.ResourceManager.GetString("Dialog_Error_" + resultPool.Value.ToString())),
                            SharedResources.ResourceManager.GetString("Dialog_Error_Title"),
                            MessageBox.MessageBoxButtons.Ok);

                        //not allow change in case of another state is in process
                        if (resultPool == PoolManagerStates.Errors.StateOfItemIsInProgress)
                        {
                            PagesHelper.Switch(mainWindow, MainPage.Instance);

                            ClearForm();
                        }
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

                var safeArrayHelper = new SafeArrayTransportHelper();
                var arrUserData = safeArrayHelper.GetArray(signatureAndHash);
                publicProfilePage.LoadUser(arrUserData[0], arrUserData[1]);

                PagesHelper.Switch(mainWindow, publicProfilePage);
            }
        }

        public async void ButtonCopyToClipboard_Click(object sender, RoutedEventArgs args)
        {
            if ((_offer != null) && (!string.IsNullOrEmpty(_offer.XRCReceivingAddress)))
            {
                try
                {
                    await ClipboardService.SetTextAsync(_offer.XRCReceivingAddress);
                }
                catch (Exception e)
                {
                    PagesHelper.Log(Instance._logger,
                        string.Format("Isn't possible to use clipboard {0}", e.Message),
                        Serilog.Events.LogEventLevel.Error);
                }
            }
        }

        public void LoadProduct(string signature)
        {
            var offer = FMONE.Current.MarketManager.GetOfferBySignature(
                signature,
                FMONE.Current.MarketPoolManager,
                FMONE.Current.MarketBlockChainManager);

            if (offer != null)
            {
                _offer = offer;

                PagesHelper.Log(Instance._logger, string.Format("Loading detail of product signature {0}", _offer.Signature));

                var tbTitle = Instance.FindControl<TextBlock>("TBTitle");
                var tbDescription = Instance.FindControl<TextBlock>("TBDescription");
                var tbShipping = Instance.FindControl<TextBlock>("TBShipping");
                var tbPrice = Instance.FindControl<TextBlock>("TBPrice");
                var tbSeller = Instance.FindControl<TextBlock>("TBSeller");
                var tbSellerStars = Instance.FindControl<TextBlock>("TBSellerStars");
                var tbSellerReviewsCount = Instance.FindControl<TextBlock>("TBSellerReviewsCount");
                var btSeller = Instance.FindControl<Button>("BTSeller");
                var btBuyButton = Instance.FindControl<Button>("BTBuyButton");

                var tbXRCReceivingAddress = Instance.FindControl<TextBox>("TBXRCReceivingAddress");

                tbTitle.Text = _offer.Title;
                tbDescription.Text = _offer.Description;
                tbShipping.Text = _offer.Shipping;
                tbPrice.Text = _offer.Price.ToString();
                btBuyButton.Tag = _offer.Signature;
                tbXRCReceivingAddress.Text = _offer.XRCReceivingAddress;

                //we need to hide these value in case of some active offers
                PagesHelper.HideOrSetValueToTextBlock(Instance, "TBManufacturer", "TBManufacturerLabel", offer.Manufacturer);
                PagesHelper.HideOrSetValueToTextBlock(Instance, "TBFineness", "TBFinenessLabel", offer.Fineness);
                PagesHelper.HideOrSetValueToTextBlock(Instance, "TBSize", "TBSizeLabel", offer.Size);
                PagesHelper.HideOrSetValueToTextBlock(Instance, "TBWeightInGrams", "TBWeightInGramsLabel", offer.WeightInGrams.ToString());

                btBuyButton.IsEnabled = true;
                if (!String.IsNullOrEmpty(_offer.BuyerSignature) || _offer.State == (int)ProductStateEnum.Sold)
                {
                    btBuyButton.IsEnabled = false;
                }

                //seller userdata loading
                var userPubKey = FMONE.Current.MarketManager.GetSellerPubKeyFromMarketItem(_offer);
                var userData = FMONE.Current.SearchEngine.GetUser(userPubKey);

                if (userData != null)
                {
                    var safeArrayHelper = new SafeArrayTransportHelper();

                    tbSeller.Text = userData.UserName;
                    btSeller.Tag = safeArrayHelper.GetString(
                            new[] { userData.Signature, userData.Hash });


                    var reviews = FMONE.Current.SearchEngine.GetAllReviewsForPubKey(userPubKey);
                    var reviewStars = FMONE.Current.UserManager.GetUserReviewStars(reviews);
                    var reviewStartRounded = Math.Round(reviewStars, 1, MidpointRounding.AwayFromZero);

                    tbSellerStars.Text = reviewStartRounded.ToString();
                    tbSellerReviewsCount.Text = reviews.Count().ToString();
                }
                else
                {
                    //if seller info could not be located, it's due to corrupted keys
                    btBuyButton.IsEnabled = false;
                }

                //photos loading
                if ((_offer.Photos != null) && (_offer.Photos.Any()))
                {
                    var skynetHelper = new SkynetHelper();
                    skynetHelper.PreloadPhotos(_offer, Instance._logger);

                    for (int i = 0; i < _offer.Photos.Count; i++)
                    {
                        if ((_offer.PrePhotos != null) && (_offer.PrePhotos.Count > i))
                        {
                            var spPhoto = Instance.FindControl<StackPanel>("SPPhoto_" + i);
                            var iPhoto = Instance.FindControl<Image>("IPhoto_" + i);
                            var zoomedPhoto = Instance.FindControl<Image>("ZoomedPhoto_" + i);

                            spPhoto.IsVisible = true;
                            iPhoto.Source = _offer.PrePhotos[i];
                            zoomedPhoto.Source = _offer.PrePhotos[i];
                        }
                    }
                }
            }
        }
        private void ClearForm()
        {
            _instance = null;
        }

        //photo zoom (maybe you could optimize the code below)
        public void IPhoto_0_Click(object sender, RoutedEventArgs e)
        {
            Popup popup_0 = this.FindControl<Popup>("Popup_0");
            popup_0.IsOpen = true;
        }
        public void Close0_Click(object sender, RoutedEventArgs e)
        {
            Popup popup_0 = this.FindControl<Popup>("Popup_0");
            popup_0.IsOpen = false;
        }

        public void IPhoto_1_Click(object sender, RoutedEventArgs e)
        {
            Popup popup_1 = this.FindControl<Popup>("Popup_1");
            popup_1.IsOpen = true;
        }
        public void Close1_Click(object sender, RoutedEventArgs e)
        {
            Popup popup_1 = this.FindControl<Popup>("Popup_1");
            popup_1.IsOpen = false;
        }

        public void IPhoto_2_Click(object sender, RoutedEventArgs e)
        {
            Popup popup_2 = this.FindControl<Popup>("Popup_2");
            popup_2.IsOpen = true;
        }
        public void Close2_Click(object sender, RoutedEventArgs e)
        {
            Popup popup_2 = this.FindControl<Popup>("Popup_2");
            popup_2.IsOpen = false;
        }

        public void IPhoto_3_Click(object sender, RoutedEventArgs e)
        {
            Popup popup_3 = this.FindControl<Popup>("Popup_3");
            popup_3.IsOpen = true;
        }
        public void Close3_Click(object sender, RoutedEventArgs e)
        {
            Popup popup_3 = this.FindControl<Popup>("Popup_3");
            popup_3.IsOpen = false;
        }

        public void IPhoto_4_Click(object sender, RoutedEventArgs e)
        {
            Popup popup_4 = this.FindControl<Popup>("Popup_4");
            popup_4.IsOpen = true;
        }
        public void Close4_Click(object sender, RoutedEventArgs e)
        {
            Popup popup_4 = this.FindControl<Popup>("Popup_4");
            popup_4.IsOpen = false;
        }

        public void IPhoto_5_Click(object sender, RoutedEventArgs e)
        {
            Popup popup_5 = this.FindControl<Popup>("Popup_5");
            popup_5.IsOpen = true;
        }
        public void Close5_Click(object sender, RoutedEventArgs e)
        {
            Popup popup_5 = this.FindControl<Popup>("Popup_5");
            popup_5.IsOpen = false;
        }

        public void IPhoto_6_Click(object sender, RoutedEventArgs e)
        {
            Popup popup_6 = this.FindControl<Popup>("Popup_6");
            popup_6.IsOpen = true;
        }
        public void Close6_Click(object sender, RoutedEventArgs e)
        {
            Popup popup_6 = this.FindControl<Popup>("Popup_6");
            popup_6.IsOpen = false;
        }

        public void IPhoto_7_Click(object sender, RoutedEventArgs e)
        {
            Popup popup_7 = this.FindControl<Popup>("Popup_7");
            popup_7.IsOpen = true;
        }
        public void Close7_Click(object sender, RoutedEventArgs e)
        {
            Popup popup_7 = this.FindControl<Popup>("Popup_7");
            popup_7.IsOpen = false;
        }
    }
}

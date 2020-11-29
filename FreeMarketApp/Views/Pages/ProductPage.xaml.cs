using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.DataStructure.Objects.BaseItems;
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
            var mainWindow = PagesHelper.GetParentWindow(this);
            var approxSpanToNewBlock = FMONE.Current.Configuration.BlockChainMarketPolicy.GetApproxTimeSpanToMineNextBlock();
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
                var userPubKey = FMONE.Current.Users.GetCurrentUserPublicKey();
                var sellerUserData = FMONE.Current.Users.GetUserDataByPublicKey(sellerPublicKeys, FMONE.Current.BasePoolManager, FMONE.Current.BaseBlockChainManager);

                var isMine = false;

                foreach (var itemUserPubKey in sellerPublicKeys)
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
                    review.UserName = FMONE.Current.Users.UserData.UserName;
                    review.MarketItemHash = _offer.Hash;
                    review.RevieweePublicKey = sellerUserData.PublicKey;
                    review.Hash = review.GenerateHash();

                    ReviewUserDataV1 signedReview = FMONE.Current.Users.SignReviewData(review, FMONE.Current.Users.PrivateKey);

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

        public void ButtonStar_Click(object sender, RoutedEventArgs args) {

            int observedStars = int.Parse(((Button)sender).Tag.ToString());
            var StarReviewToggleButtonArray = Instance.FindControl<StackPanel>("StarReviewToggleButtonArray");
            foreach (var item in StarReviewToggleButtonArray.Children)
            {
                ToggleButton starButton = item as ToggleButton;
                if (starButton != null)
                {
                    int iterableStars = int.Parse(starButton.Tag.ToString());
                    if (iterableStars<= observedStars)
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
            var approxSpanToNewBlock = FMONE.Current.Configuration.BlockChainMarketPolicy.GetApproxTimeSpanToMineNextBlock();
            var TBXRCReceivingTransaction = Instance.FindControl<TextBox>("TBXRCReceivingTransaction");

            var result = await MessageBox.Show(mainWindow,
                string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_BuyProduct"), approxSpanToNewBlock.TotalSeconds),
                SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                MessageBox.MessageBoxButtons.YesNo);

            if (result == MessageBox.MessageBoxResult.Yes)
            {
                //is it my offer?
                var itemReviewBytes = _offer.ToByteArrayForSign();
                var offerUserPubKeys = UserPublicKey.Recover(itemReviewBytes, _offer.Signature);
                var userPubKey = FMONE.Current.Users.GetCurrentUserPublicKey();
                var isMine = false;

                foreach (var itemUserPubKey in offerUserPubKeys)
                {
                    if (userPubKey.SequenceEqual(itemUserPubKey))
                    {
                        isMine = true;
                        break;
                    }
                }

                if (isMine) {
                    await MessageBox.Show(mainWindow,
                                string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_YouCantBuyYourOffer")),
                                SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                                MessageBox.MessageBoxButtons.Ok);
                }
                else
                {
                    if (String.IsNullOrEmpty(TBXRCReceivingTransaction.Text))
                    {
                        await MessageBox.Show(mainWindow,
                              string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_PleaseProvideXRCTransactionHash")),
                              SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                              MessageBox.MessageBoxButtons.Ok);
                        return;
                    }

                    _offer.XRCTransactionHash = TBXRCReceivingTransaction.Text;
                    //sign market data and generating chain connection
                    _offer = FMONE.Current.Markets.SignBuyerMarketData(
                        _offer,
                        FMONE.Current.ServerPublicAddress.PublicIP,
                        FMONE.Current.Users.PrivateKey);

                    PagesHelper.Log(_logger, string.Format("Propagate bought information to chain."));

                    var resultPool = FMONE.Current.MarketPoolManager.AcceptActionItem(_offer);
                    if (resultPool == null)
                    {
                        //create a new chat
                        var newChat = FMONE.Current.Chats.CreateNewChat(_offer);
                        FMONE.Current.Chats.SaveChat(newChat);

                        await MessageBox.Show(mainWindow,
                            string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_Waiting")),
                            SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                            MessageBox.MessageBoxButtons.Ok);

                        var chatPage = ChatPage.Instance;
                        chatPage.LoadChatByProduct(_offer.Signature);

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

                ClearForm();
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
            var offer = FMONE.Current.Markets.GetOfferBySignature(
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
                var tbPriceType = Instance.FindControl<TextBlock>("TBPriceType");
                var tbSeller = Instance.FindControl<TextBlock>("TBSeller");
                var tbSellerStars = Instance.FindControl<TextBlock>("TBSellerStars");
                var tbSellerReviewsCount = Instance.FindControl<TextBlock>("TBSellerReviewsCount");
                var btSeller = Instance.FindControl<Button>("BTSeller");
                var btBuyButton = Instance.FindControl<Button>("BTBuyButton");
                var tbManufacturer = Instance.FindControl<TextBlock>("TBManufacturer");
                var tbFineness = Instance.FindControl<TextBlock>("TBFineness");
                var tbWeightInGrams = Instance.FindControl<TextBlock>("TBWeightInGrams");
                var tbSize = Instance.FindControl<TextBlock>("TBSize");
                var tbXRCReceivingAddress = Instance.FindControl<TextBox>("TBXRCReceivingAddress");
                

                tbTitle.Text = _offer.Title;
                tbDescription.Text = _offer.Description;
                tbShipping.Text = _offer.Shipping;
                tbPrice.Text = _offer.Price.ToString();
                tbPriceType.Text = ((MarketManager.ProductPriceTypeEnum)_offer.PriceType).ToString();
                btBuyButton.Tag = _offer.Signature;            
                tbManufacturer.Text = _offer.Manufacturer;
                tbFineness.Text = _offer.Fineness;
                tbWeightInGrams.Text = _offer.WeightInGrams.ToString();
                tbSize.Text = _offer.Size;
                tbXRCReceivingAddress.Text = _offer.XRCReceivingAddress;

                btBuyButton.IsEnabled = true;
                if (!String.IsNullOrEmpty(_offer.BuyerSignature) || _offer.State == (int)ProductStateEnum.Sold)
                {
                    btBuyButton.IsEnabled = false;
                }

                //seller userdata loading
                var userPubKey = FMONE.Current.Markets.GetSellerPubKeyFromMarketItem(_offer);
                var userData = FMONE.Current.SearchEngine.GetUser(userPubKey);

                if (userData != null)
                {
                    var safeArrayHelper = new SafeArrayTransportHelper();

                    tbSeller.Text = userData.UserName;
                    btSeller.Tag = safeArrayHelper.GetString(
                            new[] { userData.Signature, userData.Hash }); 

                    
                    var reviews = FMONE.Current.SearchEngine.GetAllReviewsForPubKey(userPubKey);
                    var reviewStars = FMONE.Current.Users.GetUserReviewStars(reviews);
                    var reviewStartRounded = Math.Round(reviewStars, 1, MidpointRounding.AwayFromZero);

                    tbSellerStars.Text = reviewStartRounded.ToString();
                    tbSellerReviewsCount.Text = reviews.Count().ToString();
                }

                //photos loading
                if ((_offer.Photos != null) && (_offer.Photos.Any()))
                {
                    var skynetHelper = new SkynetHelper();
                    skynetHelper.PreloadPhotos(_offer, Instance._logger);

                    for (int i = 0; i < _offer.Photos.Count; i++)
                    {
                        if (( _offer.PrePhotos != null) && (_offer.PrePhotos.Count > i))
                        {
                            var spPhoto = Instance.FindControl<StackPanel>("SPPhoto_" + i);
                            var iPhoto = Instance.FindControl<Image>("IPhoto_" + i);

                            spPhoto.IsVisible = true;
                            iPhoto.Source = _offer.PrePhotos[i];
                        }
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

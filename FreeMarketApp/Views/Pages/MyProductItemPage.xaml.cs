using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.Markets;
using FreeMarketOne.Pools;
using Serilog;
using System.Linq;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketApp.Views.Pages
{
    public class MyProductItemPage : UserControl
    {
        private static MyProductItemPage _instance;
        private ILogger _logger;

        public static MyProductItemPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MyProductItemPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
        public static MyProductItemPage GetInstance()
        {
            return _instance;
        }

        public MyProductItemPage()
        {
            if (FMONE.Current.Logger != null)
                _logger = FMONE.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(MyProductItemPage).Namespace, typeof(MyProductItemPage).Name));

            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonBack_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyProductsPage.Instance);

            ClearForm();
        }

        public async void ButtonRemove_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);
            var approxSpanToNewBlock = FMONE.Current.Configuration.BlockChainMarketPolicy.GetApproxTimeSpanToMineNextBlock();
            var result = await MessageBox.Show(mainWindow,
                 string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_RemoveProduct"), approxSpanToNewBlock.TotalSeconds),
                 SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                 MessageBox.MessageBoxButtons.YesNo);

            var signature = ((Button)sender).Tag.ToString();
            if (result == MessageBox.MessageBoxResult.Yes)
            {
                var offer = FMONE.Current.Markets.GetOfferBySignature(
                    signature,
                    FMONE.Current.MarketPoolManager,
                    FMONE.Current.MarketBlockChainManager);

                if (offer != null)
                {
                    offer.State = (int)MarketManager.ProductStateEnum.Removed;

                    //sign market data and generating chain connection
                    offer = FMONE.Current.Markets.SignMarketData(offer, FMONE.Current.Users.PrivateKey);

                    PagesHelper.Log(_logger, string.Format("Saving remove of product to chain {0}.", signature));

                    await MessageBox.Show(mainWindow,
                        string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_Waiting")),
                        SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                        MessageBox.MessageBoxButtons.Ok);

                    var resultPool = FMONE.Current.MarketPoolManager.AcceptActionItem(offer);
                    if (resultPool == null)
                    {
                        FMONE.Current.MarketPoolManager.PropagateAllActionItemLocal();

                        MyProductsPage.Instance = null;
                        PagesHelper.Switch(mainWindow, MyProductsPage.Instance);
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
                            MyProductsPage.Instance = null;
                            PagesHelper.Switch(mainWindow, MyProductsPage.Instance);
                            ClearForm();
                        }
                    }
                }
            }
        }

        public void ButtonEdit_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            var signature = ((Button)sender).Tag.ToString();
            var addEditProductPage = AddEditProductPage.Instance;
            addEditProductPage.LoadProduct(signature);

            PagesHelper.Switch(mainWindow, addEditProductPage);

            ClearForm();
        }

        public void LoadProduct(string signature)
        {
            var offer = FMONE.Current.Markets.GetOfferBySignature(
                signature,
                FMONE.Current.MarketPoolManager,
                FMONE.Current.MarketBlockChainManager);

            if (offer != null)
            {
                PagesHelper.Log(Instance._logger, string.Format("Loading detail of my product signature {0}", offer.Signature));

                var tbTitle = Instance.FindControl<TextBlock>("TBTitle");
                var tbDescription = Instance.FindControl<TextBlock>("TBDescription");
                var tbShipping = Instance.FindControl<TextBlock>("TBShipping");
                var tbPrice = Instance.FindControl<TextBlock>("TBPrice");
                var tbPriceType = Instance.FindControl<TextBlock>("TBPriceType");
                var btEdit = Instance.FindControl<Button>("BTEdit");
                var btRemove = Instance.FindControl<Button>("BTRemove");

                var tbManufacturer = this.FindControl<TextBlock>("TBManufacturer");
                var tbFineness = this.FindControl<TextBlock>("TBFineness");
                var tbSize = this.FindControl<TextBlock>("TBSize");
                var tbWeightInGrams = this.FindControl<TextBlock>("TBWeightInGrams");
                var tbCategory = this.FindControl<TextBlock>("TBCategory");
                var tbDealType = this.FindControl<TextBlock>("TBDealType");

                tbTitle.Text = offer.Title;
                tbDescription.Text = offer.Description;
                tbShipping.Text = offer.Shipping;
                tbManufacturer.Text = offer.Manufacturer;
                tbFineness.Text = offer.Fineness;
                tbSize.Text = offer.Size;
                tbWeightInGrams.Text = offer.WeightInGrams.ToString();
                tbCategory.Text = SharedResources.ResourceManager.GetString("MarketCategory_Label_" + offer.Category.ToString());
                tbDealType.Text = SharedResources.ResourceManager.GetString("MarketDealType_Label_" + offer.DealType.ToString());

                tbPrice.Text = offer.Price.ToString();
                tbPriceType.Text = ((MarketManager.ProductPriceTypeEnum)offer.PriceType).ToString();
                btEdit.Tag = signature;
                btRemove.Tag = signature;

                //photos loading
                if ((offer.Photos != null) && (offer.Photos.Any()))
                {
                    var skynetHelper = new SkynetHelper();
                    skynetHelper.PreloadPhotos(offer, Instance._logger);

                    for (int i = 0; i < offer.Photos.Count; i++)
                    {
                        var spPhoto = Instance.FindControl<StackPanel>("SPPhoto_" + i);
                        var iPhoto = Instance.FindControl<Image>("IPhoto_" + i);

                        spPhoto.IsVisible = true;
                        iPhoto.Source = offer.PrePhotos[i];
                    }
                }

                //if item is sold or removed lock remove and edit activity
                if (offer.State != (int)MarketManager.ProductStateEnum.Default)
                {
                    btRemove.IsVisible = false;
                    btEdit.IsVisible = false;
                } 
            }
        }

        private void ClearForm()
        {
            _instance = new MyProductItemPage();
        }
    }
}

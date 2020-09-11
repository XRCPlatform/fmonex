using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.ServerCore;
using Serilog;
using System.Linq;
using static FreeMarketOne.ServerCore.MarketManager;

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
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
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

        public void ButtonRemove_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            MessageBox.Show(mainWindow, "Test", "Test title", MessageBox.MessageBoxButtons.YesNoCancel);
            //TODO: Write to chain -> state = deleted


            ClearForm();
        }

        public void ButtonEdit_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            var signature = ((Button)sender).Tag.ToString();
            var addEditProductPage = AddEditProductPage.Instance;
            addEditProductPage.LoadProduct(signature);

            PagesHelper.Switch(mainWindow, addEditProductPage);
        }

        public void LoadProduct(string signature)
        {
            var offer = FreeMarketOneServer.Current.MarketManager.GetOfferBySignature(signature);

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

                tbTitle.Text = offer.Title;
                tbDescription.Text = offer.Description;
                tbShipping.Text = offer.Shipping;
                tbPrice.Text = offer.Price.ToString();
                tbPriceType.Text = ((ProductPriceTypeEnum)offer.PriceType).ToString();
                btEdit.Tag = signature;
                btRemove.Tag = signature;

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

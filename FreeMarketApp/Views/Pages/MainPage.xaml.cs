using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DynamicData;
using FreeMarketApp.Helpers;
using FreeMarketApp.ViewModels;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FreeMarketApp.Views.Pages
{
    public class MainPage : UserControl
    {
        private static MainPage _instance;
        private ILogger _logger;

        public ObservableCollection<MarketItemV1> Items { get; }

        public static MainPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MainPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public MainPage()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(MainPage).Namespace, typeof(MainPage).Name));

            if (FreeMarketOneServer.Current.MarketManager != null)
            {
                PagesHelper.Log(_logger, string.Format("Loading market offers from chain."));

                var offers = FreeMarketOneServer.Current.MarketManager.GetAllActiveOffers();
                SkynetHelper.PreloadTitlePhotos(offers, _logger);
            }
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonProduct_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);
            var signature = ((Button) sender).Tag.ToString();

            PagesHelper.Switch(mainWindow, ProductPage.Instance);
            ProductPage.LoadProduct(signature);
        }

        public void ButtonCategory_Click(object sender, RoutedEventArgs args)
        {
            var category = Enum.Parse<MarketManager.MarketCategoryEnum>(((Button)sender).Tag.ToString());
            
            var offers = FreeMarketOneServer.Current.MarketManager.GetAllActiveOffers(category);

            ((MainPageViewModel)DataContext).Items.Clear();

            if (offers.Any())
            {
                SkynetHelper.PreloadTitlePhotos(offers, _logger);
                ((MainPageViewModel)DataContext).Items.AddRange(offers);
            }
        }
    }
}

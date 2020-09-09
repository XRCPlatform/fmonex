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
using System.Collections.Generic;
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

            //if (FreeMarketOneServer.Current.MarketManager != null)
            //{
            //    PagesHelper.Log(_logger, string.Format("Loading market offers from chain."));

            //    //var offers = FreeMarketOneServer.Current.MarketManager.GetAllActiveOffers();
            //}

           // var offers = new List<MarketItemV1>();

           // var item = new MarketItemV1();
           // item.Title = "Gold";
           // item.Shipping = "World";
           // item.Price = 102f;

           // var item2 = new MarketItemV1();
           // item2.Title = "Silver";
           // item2.Shipping = "Eu";
           // item2.Price = 1.2f;
           // item2.PriceType = 1;

           // offers.Add(item);
           // offers.Add(item2);

           //// Items = new ObservableCollection<MarketItemV1>(offers);

           // DataContext = new MainPageViewModel(offers);

            this.InitializeComponent();



            if (FreeMarketOneServer.Current.MarketManager != null)
            {
                PagesHelper.Log(_logger, string.Format("Loading market offers from chain."));

                var offers = FreeMarketOneServer.Current.MarketManager.GetAllActiveOffers();

                DataContext = new MainPageViewModel(offers);
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

            PagesHelper.Switch(mainWindow, ProductPage.Instance);
        }

        public void ButtonCategory_Click(object sender, RoutedEventArgs args)
        {
            var category = Enum.Parse<MarketManager.MarketCategoryEnum>(((Button)sender).Tag.ToString());
            
            var offers = FreeMarketOneServer.Current.MarketManager.GetAllActiveOffers(category);

            ((MainPageViewModel)DataContext).Items.Clear();

            if (offers.Any())
            {
                ((MainPageViewModel)DataContext).Items.AddRange(offers);
            }
        }
    }
}

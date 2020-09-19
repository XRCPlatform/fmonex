using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.ViewModels;
using FreeMarketOne.ServerCore;
using Serilog;
using System;
using System.Linq;
using System.Threading;

namespace FreeMarketApp.Views.Pages
{
    public class SearchResultsPage : UserControl
    {
        private static SearchResultsPage _instance;
        private ILogger _logger;
        private static string _searchPhrase;

        public static SearchResultsPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SearchResultsPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public static void SetSearchPhrase(string searchPhrase = null)
        {
            _searchPhrase = searchPhrase;
        }

        public static SearchResultsPage GetInstance()
        {
            return _instance;
        }

        public static void ResetInstance()
        {
            _instance = null;
        }

        public SearchResultsPage()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(SearchResultsPage).Namespace, typeof(SearchResultsPage).Name));

            if ((FreeMarketOneServer.Current.MarketManager != null) && (FreeMarketOneServer.Current.UserManager != null))
            {
                SpinWait.SpinUntil(() => FreeMarketOneServer.Current.GetServerState() == FreeMarketOneServer.FreeMarketOneServerStates.Online);

                PagesHelper.Log(_logger, string.Format("Loading search results from lucene."));

                var engine = FreeMarketOneServer.Current.SearchEngine;
                var result = engine.Search(_searchPhrase);

                SkynetHelper.PreloadTitlePhotos(result.Results, _logger);                

                DataContext = new SearchResultsPageViewModel(result);
            }

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
        }

        public void ButtonProduct_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, ProductPage.Instance);
        }

       
    }
}

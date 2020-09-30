using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media.Imaging;
using FreeMarketApp.Resources;
using FreeMarketApp.Views.Pages;
using FreeMarketOne.ServerCore;
using FreeMarketOne.Skynet;
using Serilog;
using Serilog.Events;
using System;
using System.Linq;

namespace FreeMarketApp.Helpers
{
    internal static class PagesHelper
    {
        internal static Window GetParentWindow(UserControl userControl)
        {
            IControl parent = userControl.Parent;
            var isWindow = false;

            do
            {
                if (parent is Window)
                {
                    isWindow = true;
                }
                else
                {
                    parent = parent.Parent;
                }

            } while (!isWindow);

            return (Window)parent;
        }

        internal static void Switch(Window mainWindow, UserControl pageAddInstance)
        {
            Panel panel = mainWindow.FindControl<Panel>("PCMainContent");
            if (!panel.Children.Contains(pageAddInstance)) panel.Children.Add(pageAddInstance);

            if ((pageAddInstance.GetType() != typeof(MainPage)) && (MainPage.GetInstance() != null) && panel.Children.Contains(MainPage.Instance)) panel.Children.Remove(MainPage.Instance);
            if ((pageAddInstance.GetType() != typeof(MyProfilePage)) && (MyProfilePage.GetInstance() != null) && panel.Children.Contains(MyProfilePage.Instance)) panel.Children.Remove(MyProfilePage.Instance);
            if ((pageAddInstance.GetType() != typeof(MyProductsPage)) && (MyProductsPage.GetInstance() != null) && panel.Children.Contains(MyProductsPage.Instance)) panel.Children.Remove(MyProductsPage.Instance);
            if ((pageAddInstance.GetType() != typeof(ChatPage)) && (ChatPage.GetInstance() != null) && panel.Children.Contains(ChatPage.Instance)) panel.Children.Remove(ChatPage.Instance);
            if ((pageAddInstance.GetType() != typeof(AddEditProductPage)) && (AddEditProductPage.GetInstance() != null) && panel.Children.Contains(AddEditProductPage.Instance)) panel.Children.Remove(AddEditProductPage.Instance);
            if ((pageAddInstance.GetType() != typeof(EditProfilePage)) && (EditProfilePage.GetInstance() != null) && panel.Children.Contains(EditProfilePage.Instance)) panel.Children.Remove(EditProfilePage.Instance);
            if ((pageAddInstance.GetType() != typeof(MyProductItemPage)) && (MyProductItemPage.GetInstance() != null) && panel.Children.Contains(MyProductItemPage.Instance)) panel.Children.Remove(MyProductItemPage.Instance);
            if ((pageAddInstance.GetType() != typeof(MyReviewsPage)) && (MyReviewsPage.GetInstance() != null) && panel.Children.Contains(MyReviewsPage.Instance)) panel.Children.Remove(MyReviewsPage.Instance);
            if ((pageAddInstance.GetType() != typeof(PublicProfilePage)) && (PublicProfilePage.GetInstance() != null) && panel.Children.Contains(PublicProfilePage.Instance)) panel.Children.Remove(PublicProfilePage.Instance);
            if ((pageAddInstance.GetType() != typeof(SearchResultsPage)) && (SearchResultsPage.GetInstance() != null) && panel.Children.Contains(SearchResultsPage.Instance)) panel.Children.Remove(SearchResultsPage.Instance);
            if ((pageAddInstance.GetType() != typeof(ProductPage)) && (ProductPage.GetInstance() != null) && panel.Children.Contains(ProductPage.Instance)) panel.Children.Remove(ProductPage.Instance);
            if ((pageAddInstance.GetType() != typeof(FirstRunPage)) && (FirstRunPage.GetInstance() != null) && panel.Children.Contains(FirstRunPage.Instance)) panel.Children.Remove(FirstRunPage.Instance);
            if ((pageAddInstance.GetType() != typeof(LoginPage)) && (LoginPage.GetInstance() != null) && panel.Children.Contains(LoginPage.Instance)) panel.Children.Remove(LoginPage.Instance);
            if ((pageAddInstance.GetType() != typeof(SettingsPage)) && (SettingsPage.GetInstance() != null) && panel.Children.Contains(SettingsPage.Instance)) panel.Children.Remove(SettingsPage.Instance);
            if ((pageAddInstance.GetType() != typeof(MyBoughtProductsPage)) && (MyBoughtProductsPage.GetInstance() != null) && panel.Children.Contains(MyBoughtProductsPage.Instance)) panel.Children.Remove(MyBoughtProductsPage.Instance);
        }

        internal static void UnlockTools(Window mainWindow, bool isUnlocked)
        {
            var btMyProfile = mainWindow.FindControl<Button>("BTMyProfile");
            var btSearch = mainWindow.FindControl<Button>("BTSearch");
            var btMyProducts = mainWindow.FindControl<Button>("BTMyProducts");
            var btPrivateChat = mainWindow.FindControl<Button>("BTPrivateChat");

            btMyProfile.IsEnabled = isUnlocked;
            btSearch.IsEnabled = isUnlocked;
            btMyProducts.IsEnabled = isUnlocked;
            btPrivateChat.IsEnabled = isUnlocked;
        }

        internal static void SetUserData(ILogger _logger, Window mainWindow)
        {
            var userManager = FreeMarketOneServer.Current.Users;
            if ((userManager != null) && (userManager.UserData != null))
            {
                var tbUserName = mainWindow.FindControl<TextBlock>("TBUserName");
                tbUserName.Text = userManager.UserData.UserName;

                var reviews = FreeMarketOneServer.Current.Users.GetAllReviewsForPubKey(
                    userManager.GetCurrentUserPublicKey(),
                    FreeMarketOneServer.Current.BasePoolManager,
                    FreeMarketOneServer.Current.BaseBlockChainManager);
                
                if (reviews.Any())
                {
                    var reviewStars = userManager.GetUserReviewStars(reviews);
                    var reviewStartRounded = Math.Round(reviewStars, 1, MidpointRounding.AwayFromZero);

                    var tbStar1 = mainWindow.FindControl<Path>("TBStar1");
                    var tbStar2 = mainWindow.FindControl<Path>("TBStar2");
                    var tbStar3 = mainWindow.FindControl<Path>("TBStar3");
                    var tbStar4 = mainWindow.FindControl<Path>("TBStar4");
                    var tbStar5 = mainWindow.FindControl<Path>("TBStar5");

                    if (reviewStartRounded >= 1) tbStar1.IsVisible = true;
                    if (reviewStartRounded >= 2) tbStar2.IsVisible = true;
                    if (reviewStartRounded >= 3) tbStar3.IsVisible = true;
                    if (reviewStartRounded >= 4) tbStar4.IsVisible = true;
                    if (reviewStartRounded >= 5) tbStar5.IsVisible = true;
                }

                if (!string.IsNullOrEmpty(userManager.UserData.Photo) && (userManager.UserData.Photo.Contains(SkynetWebPortal.SKYNET_PREFIX)))
                {
                    var iPhoto = mainWindow.FindControl<Image>("IPhoto");

                    var skynetHelper = new SkynetHelper();
                    var skynetStream = skynetHelper.DownloadFromSkynet(userManager.UserData.Photo, _logger);
                    iPhoto.Source = new Bitmap(skynetStream);
                }
            }
        }

        internal static void SetServerData(ILogger _logger, Window mainWindow)
        {
            var torProcessManager = FreeMarketOneServer.Current.TorProcessManager;
            var swarmServer = FreeMarketOneServer.Current.BaseBlockChainManager.SwarmServer;

            if (swarmServer != null)
            {
                var tbPeers = mainWindow.FindControl<TextBlock>("TBPeers");
                tbPeers.Text = swarmServer.Peers.Count().ToString();
            }

            if (torProcessManager != null)
            {
                var tbTorStatus = mainWindow.FindControl<TextBlock>("TBTorStatus");
                tbTorStatus.Text = torProcessManager.IsRunning ?
                    SharedResources.ResourceManager.GetString("State_Running") :
                    SharedResources.ResourceManager.GetString("State_Down");
            }
        }

        internal static void Log(ILogger _logger, string message, LogEventLevel level = LogEventLevel.Information)
        {
            if (_logger != null)
            {
                switch (level)
                {
                    case LogEventLevel.Debug:
                        _logger.Debug(message);
                        break;
                    case LogEventLevel.Error:
                        _logger.Error(message);
                        break;
                    case LogEventLevel.Fatal:
                        _logger.Fatal(message);
                        break;
                    case LogEventLevel.Verbose:
                        _logger.Verbose(message);
                        break;
                    case LogEventLevel.Warning:
                        _logger.Warning(message);
                        break;
                    default:
                        _logger.Information(message);
                        break;
                }
            }
        }
    }
}

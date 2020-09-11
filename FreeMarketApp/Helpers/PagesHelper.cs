using Avalonia.Controls;
using FreeMarketApp.Views.Pages;
using FreeMarketOne.ServerCore;
using Serilog;
using Serilog.Events;

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
            if ((pageAddInstance.GetType() != typeof(MyItemPage)) && (MyItemPage.GetInstance() != null) && panel.Children.Contains(MyItemPage.Instance)) panel.Children.Remove(MyItemPage.Instance);
            if ((pageAddInstance.GetType() != typeof(MyReviewsPage)) && (MyReviewsPage.GetInstance() != null) && panel.Children.Contains(MyReviewsPage.Instance)) panel.Children.Remove(MyReviewsPage.Instance);
            if ((pageAddInstance.GetType() != typeof(PublicProfilePage)) && (PublicProfilePage.GetInstance() != null) && panel.Children.Contains(PublicProfilePage.Instance)) panel.Children.Remove(PublicProfilePage.Instance);
            if ((pageAddInstance.GetType() != typeof(SearchResultsPage)) && (SearchResultsPage.GetInstance() != null) && panel.Children.Contains(SearchResultsPage.Instance)) panel.Children.Remove(SearchResultsPage.Instance);
            if ((pageAddInstance.GetType() != typeof(ProductPage)) && (ProductPage.GetInstance() != null) && panel.Children.Contains(ProductPage.Instance)) panel.Children.Remove(ProductPage.Instance);
            if ((pageAddInstance.GetType() != typeof(FirstRunPage)) && (FirstRunPage.GetInstance() != null) && panel.Children.Contains(FirstRunPage.Instance)) panel.Children.Remove(FirstRunPage.Instance);
            if ((pageAddInstance.GetType() != typeof(LoginPage)) && (LoginPage.GetInstance() != null) && panel.Children.Contains(LoginPage.Instance)) panel.Children.Remove(LoginPage.Instance);
            if ((pageAddInstance.GetType() != typeof(SettingsPage)) && (SettingsPage.GetInstance() != null) && panel.Children.Contains(SettingsPage.Instance)) panel.Children.Remove(SettingsPage.Instance);
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

        internal static void SetUserData(Window mainWindow)
        {
            var userManager = FreeMarketOneServer.Current.UserManager;
            if ((userManager != null) && (userManager.UserData != null))
            {
                var tbUserName = mainWindow.FindControl<TextBlock>("TBUserName");
                tbUserName.Text = userManager.UserData.UserName;
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

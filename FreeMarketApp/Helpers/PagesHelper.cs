using Avalonia.Controls;
using FreeMarketApp.Views.Pages;
using FreeMarketOne.ServerCore;
using FreeMarketOne.Skynet;
using Microsoft.Extensions.FileProviders;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace FreeMarketApp.Helpers
{
    internal static class PagesHelper
    {
        private const string VALIDCHARS = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";

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

            if ((pageAddInstance.GetType() != typeof(MainPage)) && panel.Children.Contains(MainPage.Instance)) panel.Children.Remove(MainPage.Instance);
            if ((pageAddInstance.GetType() != typeof(MyProfilePage)) && panel.Children.Contains(MyProfilePage.Instance)) panel.Children.Remove(MyProfilePage.Instance);
            if ((pageAddInstance.GetType() != typeof(MyProductsPage)) && panel.Children.Contains(MyProductsPage.Instance)) panel.Children.Remove(MyProductsPage.Instance);
            if ((pageAddInstance.GetType() != typeof(ChatPage)) && panel.Children.Contains(ChatPage.Instance)) panel.Children.Remove(ChatPage.Instance);
            if ((pageAddInstance.GetType() != typeof(AddEditProductPage)) && panel.Children.Contains(AddEditProductPage.Instance)) panel.Children.Remove(AddEditProductPage.Instance);
            if ((pageAddInstance.GetType() != typeof(EditProfilePage)) && panel.Children.Contains(EditProfilePage.Instance)) panel.Children.Remove(EditProfilePage.Instance);
            if ((pageAddInstance.GetType() != typeof(MyItemPage)) && panel.Children.Contains(MyItemPage.Instance)) panel.Children.Remove(MyItemPage.Instance);
            if ((pageAddInstance.GetType() != typeof(MyReviewsPage)) && panel.Children.Contains(MyReviewsPage.Instance)) panel.Children.Remove(MyReviewsPage.Instance);
            if ((pageAddInstance.GetType() != typeof(PublicProfilePage)) && panel.Children.Contains(PublicProfilePage.Instance)) panel.Children.Remove(PublicProfilePage.Instance);
            if ((pageAddInstance.GetType() != typeof(SearchResultsPage)) && panel.Children.Contains(SearchResultsPage.Instance)) panel.Children.Remove(SearchResultsPage.Instance);
            if ((pageAddInstance.GetType() != typeof(ProductPage)) && panel.Children.Contains(ProductPage.Instance)) panel.Children.Remove(ProductPage.Instance);
            if ((pageAddInstance.GetType() != typeof(FirstRunPage)) && panel.Children.Contains(FirstRunPage.Instance)) panel.Children.Remove(FirstRunPage.Instance);
            if ((pageAddInstance.GetType() != typeof(LoginPage)) && panel.Children.Contains(LoginPage.Instance)) panel.Children.Remove(LoginPage.Instance);
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

        internal static string UploadToSkynet(string localPath, ILogger logger)
        {
            string skylinkUrl = null;

            try
            {
                PagesHelper.Log(logger, string.Format("Skynet Upload File: {0}", localPath));

                var applicationRoot = Path.GetDirectoryName(localPath);
                var fileName = Path.GetFileName(localPath);
                IFileProvider provider = new PhysicalFileProvider(applicationRoot);

                PagesHelper.Log(logger, string.Format("Skynet Gateway: {0}", SkynetWebPortal.SKYNET_GATEURL));

                var httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(SkynetWebPortal.SKYNET_GATEURL)
                };

                var skynetWebPortal = new SkynetWebPortal(httpClient);
                var fileInfo = provider.GetFileInfo(fileName);

                var uniqueIndex = Guid.NewGuid();
                PagesHelper.Log(logger, string.Format("Procesing upload with GUID: {0}", uniqueIndex));

                var uploadInfo = skynetWebPortal.UploadFiles(uniqueIndex.ToString(), new UploadItem[] { new UploadItem(fileInfo) }).Result;

                skylinkUrl = string.Format("{0}{1}", SkynetWebPortal.SKYNET_PREFIX, uploadInfo.Skylink);

                PagesHelper.Log(logger, string.Format("Skynet Link: {0}", skylinkUrl));
            }
            catch (Exception e)
            {
                PagesHelper.Log(logger, string.Format("Skynet Link: {0} - {1}", e.Message, e.StackTrace), Serilog.Events.LogEventLevel.Error);
            }

            return skylinkUrl;
        }

        internal static bool IsNumberValid(string text)
        {
            float number;

            if (float.TryParse(text, out number))
            {
                return true;
            } 
            else
            {
                return false;
            }
        }

        internal static Stream DownloadFromSkynet(string skylink, ILogger logger)
        {
            PagesHelper.Log(logger, string.Format("Skynet Download File: {0}", skylink));

            var httpClient = new HttpClient()
            {
                BaseAddress = new Uri(SkynetWebPortal.SKYNET_GATEURL)
            };

            var skynetWebPortal = new SkynetWebPortal(httpClient);

            var content = skynetWebPortal.DownloadFile(skylink).Result;

            return content.ReadAsStreamAsync().Result;
        }

        public static bool IsTextValid(string text, bool allowSpace = false)
        {
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }
            else
            {
                for (int i = 0; i < text.Length; i++)
                {
                    var charTest = text.Substring(i, 1);
                    if (!VALIDCHARS.Contains(charTest))
                    {
                        if ((charTest == " ") && (allowSpace == true))
                        {
                            //silence
                        } 
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}

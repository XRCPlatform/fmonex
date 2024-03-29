﻿using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FreeMarketApp.Resources;
using FreeMarketApp.Views.Pages;
using FreeMarketOne.Skynet;
using Serilog;
using Serilog.Events;
using System;
using System.Linq;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

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
            if ((pageAddInstance.GetType() != typeof(MySoldProductsPage)) && (MySoldProductsPage.GetInstance() != null) && panel.Children.Contains(MySoldProductsPage.Instance)) panel.Children.Remove(MySoldProductsPage.Instance);
        }

        internal static void UnlockTools(Window mainWindow, bool isUnlocked)
        {
            var btMyProfile = mainWindow.FindControl<Button>("BTMyProfile");
            var btSearch = mainWindow.FindControl<Button>("BTSearch");
            var btMyProducts = mainWindow.FindControl<Button>("BTMyProducts");
            var btPrivateChat = mainWindow.FindControl<Button>("BTPrivateChat");
            var tbSearchField = mainWindow.FindControl<TextBox>("TBSearchField");

            btMyProfile.IsEnabled = isUnlocked;
            btSearch.IsEnabled = isUnlocked;
            btMyProducts.IsEnabled = isUnlocked;
            btPrivateChat.IsEnabled = isUnlocked;
            tbSearchField.IsEnabled = isUnlocked;
        }

        internal static void SetUserData(ILogger _logger, Window mainWindow)
        {
            var userManager = FMONE.Current.UserManager;
            if ((userManager != null) && (userManager.UserData != null))
            {
                var tbUserName = mainWindow.FindControl<TextBlock>("TBUserName");
                tbUserName.Text = userManager.UserData.UserName;

                var reviews = FMONE.Current.UserManager.GetAllReviewsForPubKey(
                    userManager.GetCurrentUserPublicKey(),
                    FMONE.Current.BasePoolManager,
                    FMONE.Current.BaseBlockChainManager);
                
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
                    if (skynetStream != null) iPhoto.Source = new Bitmap(skynetStream);
                }
            }
        }

        internal static void SetServerData(ILogger _logger, Window mainWindow)
        {
            var torProcessManager = FMONE.Current.TorProcessManager;
            var swarmServer = FMONE.Current.BaseBlockChainManager.SwarmServer;

            if (swarmServer != null)
            {
                var tbPeers = mainWindow.FindControl<TextBlock>("TBPeers");
                var bPeersStatus = mainWindow.FindControl<Border>("BPeersStatus");
                tbPeers.Text = swarmServer.Peers.Count().ToString();
                
                if (swarmServer.Peers.Count() > 0)
                {
                    bPeersStatus.Classes = new Classes(new[] { "StatusBarIconGreen" });
                } 
                else
                {
                    bPeersStatus.Classes = new Classes(new[] { "StatusBarIconRed" });
                }
            }

            if (torProcessManager != null)
            {
                var tbTorStatus = mainWindow.FindControl<TextBlock>("TBTorStatus");
                tbTorStatus.Text = torProcessManager.IsRunning ?
                    SharedResources.ResourceManager.GetString("State_Running") :
                    SharedResources.ResourceManager.GetString("State_Down");
            }
        }

        internal static void SetServerData(Window mainWindow, 
            bool peersUp, 
            bool torUp, 
            int peersCount,
            long baseHeight,
            int poolBaseLocalItemsCount, 
            int poolBaseStagedItemsCount,
            long marketHeight,
            int poolMarketLocalItemsCount, 
            int poolMarketStagedItemsCount)
        {
            Dispatcher.UIThread.Post(() => {
                try
                {
                    if (mainWindow != null)
                    {
                        //publishing peer count info
                        var tbPeers = mainWindow.FindControl<TextBlock>("TBPeers");
                        tbPeers.Text = peersCount.ToString();

                        var bPeersStatus = mainWindow.FindControl<Border>("BPeersStatus");

                        if (peersUp)
                        {
                            if (peersCount > 0)
                            {
                                bPeersStatus.Classes = new Classes(new[] { "StatusBarIconGreen" });
                            }
                            else
                            {
                                bPeersStatus.Classes = new Classes(new[] { "StatusBarIconRed" });
                            }
                        }
                        else
                        {
                            bPeersStatus.Classes = new Classes(new[] { "StatusBarIconOrange" });
                        }

                        //publishing TOR info 
                        var tbTorIcon = mainWindow.FindControl<Path>("TorIcon");
                        var tbTorStatus = mainWindow.FindControl<TextBlock>("TBTorStatus");
                        if (torUp)
                        {
                            tbTorIcon.Classes = new Classes(new[] { "StatusBarIconGreen" });
                            tbTorStatus.Text = SharedResources.ResourceManager.GetString("State_Running");
                        }
                        else
                        {
                            tbTorIcon.Classes = new Classes(new[] { "StatusBarIconRed" });
                            tbTorStatus.Text = SharedResources.ResourceManager.GetString("State_Down");
                        }

                        //publishing Base Pool info
                        var tbBasePool = mainWindow.FindControl<TextBlock>("TBBasePool");
                        var tbBaseHeight = mainWindow.FindControl<TextBlock>("TBBaseHeight");
                        tbBasePool.Text = string.Format("{0}/{1}", poolBaseLocalItemsCount, poolBaseStagedItemsCount);
                        tbBaseHeight.Text = baseHeight.ToString();

                        //publishing Market Pool info
                        var tbMarketPools = mainWindow.FindControl<TextBlock>("TBMarketPools");
                        var tbMarketHeight = mainWindow.FindControl<TextBlock>("TBMarketHeight");
                        tbMarketPools.Text = string.Format("{0}/{1}", poolMarketLocalItemsCount, poolMarketStagedItemsCount);
                        tbMarketHeight.Text = marketHeight.ToString();
                    }
                }
                catch (Exception)
                {
                    //swallow not important that can't update UI
                }               
            });           
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

        internal static void HideOrSetValueToTextBlock(UserControl pageInstance, string targetTextBlockId, string labelTextBlockId, string value)
        {
            var targetTextBlock = pageInstance.FindControl<TextBlock>(targetTextBlockId);
            var labelTextBlock = pageInstance.FindControl<TextBlock>(labelTextBlockId);

            if (!string.IsNullOrEmpty(value))
            {
                targetTextBlock.Text = value;
            }
            else
            {
                labelTextBlock.IsVisible = false;
                targetTextBlock.IsVisible = false;
            }
        }
    }
}

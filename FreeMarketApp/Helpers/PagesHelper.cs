﻿using Avalonia.Controls;
using FreeMarketApp.Views.Pages;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

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
            Panel panel = mainWindow.FindControl<Panel>("PanelContent");
            if (!panel.Children.Contains(pageAddInstance)) panel.Children.Add(pageAddInstance);

            if ((pageAddInstance.GetType() != typeof(MainPage)) && panel.Children.Contains(MainPage.Instance)) panel.Children.Remove(MainPage.Instance);
            if ((pageAddInstance.GetType() != typeof(MyProfilePage)) && panel.Children.Contains(MyProfilePage.Instance)) panel.Children.Remove(MyProfilePage.Instance);
            if ((pageAddInstance.GetType() != typeof(MyProductsPage)) && panel.Children.Contains(MyProductsPage.Instance)) panel.Children.Remove(MyProductsPage.Instance);
            if ((pageAddInstance.GetType() != typeof(ChatPage)) && panel.Children.Contains(ChatPage.Instance)) panel.Children.Remove(ChatPage.Instance);
            if ((pageAddInstance.GetType() != typeof(AddEditProductPage)) && panel.Children.Contains(AddEditProductPage.Instance)) panel.Children.Remove(AddEditProductPage.Instance);
            if ((pageAddInstance.GetType() != typeof(AddEditProfilePage)) && panel.Children.Contains(AddEditProfilePage.Instance)) panel.Children.Remove(AddEditProfilePage.Instance);
            if ((pageAddInstance.GetType() != typeof(MyItemPage)) && panel.Children.Contains(MyItemPage.Instance)) panel.Children.Remove(MyItemPage.Instance);
            if ((pageAddInstance.GetType() != typeof(MyReviewsPage)) && panel.Children.Contains(MyReviewsPage.Instance)) panel.Children.Remove(MyReviewsPage.Instance);
            if ((pageAddInstance.GetType() != typeof(PublicProfilePage)) && panel.Children.Contains(PublicProfilePage.Instance)) panel.Children.Remove(PublicProfilePage.Instance);
            if ((pageAddInstance.GetType() != typeof(SearchResultsPage)) && panel.Children.Contains(SearchResultsPage.Instance)) panel.Children.Remove(SearchResultsPage.Instance);
            if ((pageAddInstance.GetType() != typeof(ProductPage)) && panel.Children.Contains(ProductPage.Instance)) panel.Children.Remove(ProductPage.Instance);
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

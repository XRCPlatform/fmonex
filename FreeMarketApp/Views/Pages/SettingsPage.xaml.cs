using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using FreeMarketApp.Helpers;
using FreeMarketOne.ServerCore;
using Serilog;
using System;

namespace FreeMarketApp.Views.Pages
{
    public class SettingsPage : UserControl
    {
        private static SettingsPage _instance;
        private ILogger _logger;
        private UserControl _returnToInstanceOfPage;

        public static SettingsPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SettingsPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public static SettingsPage GetInstance()
        {
            return _instance;
        }

        public void SetReturnTo(UserControl page)
        {
            Instance._returnToInstanceOfPage = page;
        }

        public SettingsPage()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(SettingsPage).Namespace, typeof(SettingsPage).Name));

            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonBack_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, _returnToInstanceOfPage);
        }

        public void ButtonSave_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);


 
    //        mainWindow.Styles.
    //        var baseLight = (IStyle)AvaloniaXamlLoader.Load(
    //new Uri("resm:Avalonia.Themes.Default.Accents.BaseLight.xaml?assembly=Avalonia.Themes.Default"));

            //  mainWindow..Resources
        }

    }
}

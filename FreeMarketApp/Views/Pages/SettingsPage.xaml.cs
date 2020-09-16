using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.ServerCore;
using Serilog;

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

            var rbApperanceLight = this.FindControl<RadioButton>("RBApperanceLight");
            var rbApperanceDark = this.FindControl<RadioButton>("RBApperanceDark");

            var theme = ThemeHelper.GetThemeName();
            if (theme == ThemeHelper.DARK_THEME)
            {
                rbApperanceDark.IsChecked = true;
            }
            else
            {
                rbApperanceLight.IsChecked = true;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonBack_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, _returnToInstanceOfPage);

            ClearForm();
        }

        public async void ButtonSave_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            var rbApperanceLight = Instance.FindControl<RadioButton>("RBApperanceLight");
            var rbApperanceDark = Instance.FindControl<RadioButton>("RBApperanceDark");

            if ((rbApperanceDark.IsChecked.HasValue) && (rbApperanceDark.IsChecked.Value))
            {
                ThemeHelper.SetTheme(ThemeHelper.DARK_THEME);
            } 
            else
            {
                ThemeHelper.SetTheme(ThemeHelper.LIGHT_THEME);
            }

            var result = await MessageBox.Show(mainWindow,
                SharedResources.ResourceManager.GetString("Dialog_Information_ChangeTheme"),
                SharedResources.ResourceManager.GetString("Dialog_Information_Title"),
                MessageBox.MessageBoxButtons.Ok);
        }

        private void ClearForm()
        {
            _instance = null;
        }
    }
}

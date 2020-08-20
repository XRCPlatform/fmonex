using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.Views.Controls;
using Libplanet.Extensions.Helpers;
using System.Threading.Tasks;
using static FreeMarketApp.Views.Controls.MessageBox;

namespace FreeMarketApp.Views.Pages
{
    public class AddEditProductPage : UserControl
    {
        private static AddEditProductPage _instance;
        public static AddEditProductPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AddEditProductPage();
                return _instance;
            }
        }

        public AddEditProductPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonBack_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyProductsPage.Instance);
        }

        private async void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);
            var result = await MessageBox.Show(mainWindow,
                SharedResources.ResourceManager.GetString("Dialog_Confirmation"),
                SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"), 
                MessageBox.MessageBoxButtons.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                ClearForm();
                PagesHelper.Switch(mainWindow, MyProductsPage.Instance);
            }
        }

        public void ButtonSave_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyProductsPage.Instance);
        }


        private void ClearForm()
        {
            var tbTitle = this.FindControl<TextBox>("TBTitle");
            tbTitle.Text = string.Empty;
        }
    }
}

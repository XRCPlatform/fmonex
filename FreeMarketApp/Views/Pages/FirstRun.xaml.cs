using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.ViewModels;
using FreeMarketOne.ServerCore;
using System.Threading.Tasks;

namespace FreeMarketApp.Views.Pages
{
    public class FirstRunPage : UserControl
    {
        private static FirstRunPage _instance;
        public static FirstRunPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new FirstRunPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public FirstRunPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonSave_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            ////check form
            //var tbTitle = this.FindControl<TextBox>("TBTitle");
            //var tbDescription = this.FindControl<TextBox>("TBDescription");
            //var tbShipping = this.FindControl<TextBox>("TBShipping");
            //var cbCategory = this.FindControl<ComboBox>("CBCategory");
            //var cbDealType = this.FindControl<ComboBox>("CBDealType");

            //var errorCount = 0;

            //if (string.IsNullOrEmpty(tbTitle.Text)) errorCount++;
            //if (string.IsNullOrEmpty(tbDescription.Text)) errorCount++;
            //if (string.IsNullOrEmpty(tbShipping.Text)) errorCount++;

            //var cbCategoryValue = cbCategory.SelectedItem as ComboBoxItem;
            //if (cbCategoryValue.Tag.ToString() == "0") errorCount++;

            //var cbDealTypeValue = cbDealType.SelectedItem as ComboBoxItem;
            //if (cbDealTypeValue.Tag.ToString() == "0") errorCount++;

            //if (_marketItemData.Photos.Count() == 0) errorCount++;

            //if (errorCount == 0)
            //{
            //    //save to chain
            //    _marketItemData.Title = tbTitle.Text;
            //    _marketItemData.Description = tbDescription.Text;
            //    _marketItemData.Shipping = tbShipping.Text;
            //    _marketItemData.Category = cbCategoryValue.Tag.ToString();
            //    _marketItemData.DealType = cbDealTypeValue.Tag.ToString();

            //    //get time to next block
            //    //upload to sia
            //    for (int i = _marketItemData.Photos.Count(); i > 0; i--)
            //    {
            //        if (!_marketItemData.Photos[i - 1].Contains(SkynetWebPortal.SKYNET_PREFIX))
            //        {
            //            var skynetUrl = UploadToSkynet(_marketItemData.Photos[i - 1]);
            //            if (skynetUrl == null)
            //            {
            //                _marketItemData.Photos.RemoveAt(i - 1);
            //            }
            //            else
            //            {
            //                _marketItemData.Photos[i - 1] = skynetUrl;
            //            }
            //        }
            //    }

            //    PagesHelper.Switch(mainWindow, MyProductsPage.Instance);
            //    ClearForm();

            //}
            //else
            //{

            //    await MessageBox.Show(mainWindow,
            //       SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_EmptyForm"),
            //        SharedResources.ResourceManager.GetString("Dialog_Information_Title"),
            //        MessageBox.MessageBoxButtons.Ok);
            //}


            async void AppAsyncLoadingStart()
            {
                var splashViewModel = new SplashWindowViewModel();
                splashViewModel.StartupProgressText = "Reloading...";
                var splashWindow = new SplashWindow { DataContext = splashViewModel };
                splashWindow.Show();
                await Task.Delay(10);

                //reloading server with splash window
                FreeMarketOneServer.Current.Initialize();
                PagesHelper.Switch(mainWindow, MainPage.Instance);

                if (splashWindow != null)
                {
                    splashWindow.Close();
                }
            }

            AppAsyncLoadingStart();
        }

        public void ButtonRandomSeed_Click(object sender, RoutedEventArgs args)
        {
            var tbSeed = this.FindControl<TextBox>("TBSeed");
            tbSeed.Text = FreeMarketOneServer.Current.UserManager.CreateRandomSeed();
        }
    }
}

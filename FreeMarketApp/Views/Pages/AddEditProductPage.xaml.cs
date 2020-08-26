using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.DataStructure.Objects.MarketItems;
using Libplanet.Extensions.Helpers;
using System.Linq;
using System.Threading.Tasks;
using static FreeMarketApp.Views.Controls.MessageBox;

namespace FreeMarketApp.Views.Pages
{
    public class AddEditProductPage : UserControl
    {
        private static MarketItemV1 marketItemData;

        private static AddEditProductPage _instance;
        public static AddEditProductPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AddEditProductPage();
                return _instance;
            }
            set 
            {
                _instance = value;
            }
        }

        public AddEditProductPage()
        {
            marketItemData = new MarketItemV1();

            this.InitializeComponent();

            if (marketItemData.Photos.Count >= 8)
            {
                var btAddPhoto = this.FindControl<Button>("BTAddPhoto");
                btAddPhoto.IsVisible = false;
            }
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
                SharedResources.ResourceManager.GetString("Dialog_Confirmation_Cancel"),
                SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"), 
                MessageBox.MessageBoxButtons.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                PagesHelper.Switch(mainWindow, MyProductsPage.Instance);
                ClearForm();
            }
        }

        public async void ButtonSave_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);
            var result = await MessageBox.Show(mainWindow,
                string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_SaveMyItem"), 300),
                SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                MessageBox.MessageBoxButtons.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                //check form
                var tbTitle = this.FindControl<TextBox>("TBTitle");
                var tbDescription = this.FindControl<TextBox>("TBDescription");
                var tbShipping = this.FindControl<TextBox>("TBShipping");
                var cbCategory = this.FindControl<ComboBox>("CBCategory");
                var cbDealType = this.FindControl<ComboBox>("CBDealType");

                var errorCount = 0;

                if (string.IsNullOrEmpty(tbTitle.Text)) errorCount++;
                if (string.IsNullOrEmpty(tbDescription.Text)) errorCount++;
                if (string.IsNullOrEmpty(tbShipping.Text)) errorCount++;

                var cbCategoryValue = cbCategory.SelectedItem as ComboBoxItem;
                if (cbCategoryValue.Tag.ToString() == "0") errorCount++;

                var cbDealTypeValue = cbDealType.SelectedItem as ComboBoxItem;
                if (cbDealTypeValue.Tag.ToString() == "0") errorCount++;

                if (marketItemData.Photos.Count() == 0) errorCount++;

                if (errorCount == 0)
                {
                    //save to chain
                    marketItemData.Title = tbTitle.Text;
                    marketItemData.Description = tbDescription.Text;
                    marketItemData.Shipping = tbShipping.Text;
                    marketItemData.Category = cbCategoryValue.Tag.ToString();
                    marketItemData.DealType = cbDealTypeValue.Tag.ToString();

                    //get time to next block
                    //upload to sia

                    PagesHelper.Switch(mainWindow, MyProductsPage.Instance);
                    ClearForm();

                } else {

                    await MessageBox.Show(mainWindow,
                       SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_EmptyForm"),
                        SharedResources.ResourceManager.GetString("Dialog_Information_Title"),
                        MessageBox.MessageBoxButtons.Ok);
                }
            }
        }

        public async Task<string> GetPhotoPath(Window mainWindow)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filters.Add(new FileDialogFilter() { Name = "Photo", Extensions = { "jpg" } });

            string[] result = await dialog.ShowAsync(mainWindow);

            if ((result != null) && result.Any())
            {
                return result.First();
            } 
            else
            {
                return null;
            }
        }

        public async void ButtonAddPhoto_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);
            string photoPath = await GetPhotoPath(mainWindow);

            if (!string.IsNullOrEmpty(photoPath))
            {
                var itemIndex = marketItemData.Photos.Count;
                var spPhoto = this.FindControl<StackPanel>("SPPhoto_" + itemIndex);
                var iPhoto = this.FindControl<Image>("IPhoto_" + itemIndex);
 
                spPhoto.IsVisible = true;
                iPhoto.Source = new Bitmap(photoPath);
                
                marketItemData.Photos.Add(photoPath);

                if (marketItemData.Photos.Count >= 8)
                {
                    var btAddPhoto = this.FindControl<Button>("BTAddPhoto");
                    btAddPhoto.IsVisible = false;
                }
            }
        }

        public void ButtonRemove_Click(object sender, RoutedEventArgs args)
        {
            var itemIndex = int.Parse(((Button)sender).Tag.ToString());
            var lastIndex = marketItemData.Photos.Count - 1;

            if (itemIndex != lastIndex)
            {
                for (int i = itemIndex; i < (marketItemData.Photos.Count - 1); i++)
                {
                    var iPhoto = this.FindControl<Image>("IPhoto_" + i);
                    var iPhotoNext = this.FindControl<Image>("IPhoto_" + (i + 1));

                    iPhoto.Source = iPhotoNext.Source;
                    marketItemData.Photos[i] = marketItemData.Photos[i + 1];
                }
            }

            //hide lastindex
            var spLastPhoto = this.FindControl<StackPanel>("SPPhoto_" + lastIndex);
            spLastPhoto.IsVisible = false;
            marketItemData.Photos.RemoveAt(lastIndex);
        }

        private void ClearForm()
        {
            _instance = null;
        }
    }
}

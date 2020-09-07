using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore;
using FreeMarketOne.Skynet;
using Serilog;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FreeMarketApp.Views.Controls.MessageBox;

namespace FreeMarketApp.Views.Pages
{
    public class AddEditProductPage : UserControl
    {
        private MarketItemV1 _marketItemData;
        private ILogger _logger;

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
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(AddEditProductPage).Namespace, typeof(AddEditProductPage).Name));

            _marketItemData = new MarketItemV1();

            this.InitializeComponent();

            PagesHelper.Log(_logger, string.Format("Loading product data of {0} to profile page.", "?"));

            if (_marketItemData.Photos.Count >= 8)
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

        public async void ButtonCancel_Click(object sender, RoutedEventArgs e)
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
                var tbPrice = this.FindControl<TextBox>("TBPrice");
                var cbCategory = this.FindControl<ComboBox>("CBCategory");
                var cbDealType = this.FindControl<ComboBox>("CBDealType");
                var cbPriceType = this.FindControl<ComboBox>("CBPriceType");

                var errorCount = 0;
                var errorMessages = new StringBuilder();

                if (string.IsNullOrEmpty(tbTitle.Text)) errorCount++;
                if (string.IsNullOrEmpty(tbDescription.Text)) errorCount++;
                if (string.IsNullOrEmpty(tbShipping.Text)) errorCount++;

                var cbCategoryValue = cbCategory.SelectedItem as ComboBoxItem;
                if (cbCategoryValue.Tag.ToString() == "0") errorCount++;

                var cbDealTypeValue = cbDealType.SelectedItem as ComboBoxItem;
                if (cbDealTypeValue.Tag.ToString() == "0") errorCount++;

                if (_marketItemData.Photos.Count() == 0) errorCount++;

                if (errorCount == 0)
                {
                    PagesHelper.Log(_logger, string.Format("Saving new data of product."));

                    //save to chain
                    _marketItemData.Title = tbTitle.Text;
                    _marketItemData.Description = tbDescription.Text;
                    _marketItemData.Shipping = tbShipping.Text;
                    _marketItemData.Category = int.Parse(cbCategoryValue.Tag.ToString());
                    _marketItemData.DealType = int.Parse(cbDealTypeValue.Tag.ToString());
                    _marketItemData.Price = int.Parse(tbPrice.Text.Trim());
                    _marketItemData.PriceType = int.Parse(cbPriceType.Tag.ToString());

                    //get time to next block
                    //upload to sia
                    for (int i = _marketItemData.Photos.Count(); i > 0; i--)
                    {
                        if (!_marketItemData.Photos[i - 1].Contains(SkynetWebPortal.SKYNET_PREFIX))
                        {
                            PagesHelper.Log(_logger, string.Format("Uploading to Skynet {0}.", _marketItemData.Photos[i - 1]));

                            var skynetUrl = PagesHelper.UploadToSkynet(_marketItemData.Photos[i - 1], _logger);
                            if (skynetUrl == null)
                            {
                                _marketItemData.Photos.RemoveAt(i - 1);
                            } 
                            else
                            {
                                _marketItemData.Photos[i - 1] = skynetUrl;
                            }
                        }
                    }

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
                var itemIndex = _marketItemData.Photos.Count;
                var spPhoto = this.FindControl<StackPanel>("SPPhoto_" + itemIndex);
                var iPhoto = this.FindControl<Image>("IPhoto_" + itemIndex);

                spPhoto.IsVisible = true;
                iPhoto.Source = new Bitmap(photoPath);

                _marketItemData.Photos.Add(photoPath);

                if (_marketItemData.Photos.Count >= 8)
                {
                    var btAddPhoto = this.FindControl<Button>("BTAddPhoto");
                    btAddPhoto.IsVisible = false;
                }
            }
        }

        public void ButtonRemove_Click(object sender, RoutedEventArgs args)
        {
            var itemIndex = int.Parse(((Button)sender).Tag.ToString());
            var lastIndex = _marketItemData.Photos.Count - 1;

            if (itemIndex != lastIndex)
            {
                for (int i = itemIndex; i < (_marketItemData.Photos.Count - 1); i++)
                {
                    var iPhoto = this.FindControl<Image>("IPhoto_" + i);
                    var iPhotoNext = this.FindControl<Image>("IPhoto_" + (i + 1));

                    iPhoto.Source = iPhotoNext.Source;
                    _marketItemData.Photos[i] = _marketItemData.Photos[i + 1];
                }
            }

            //hide lastindex
            var spLastPhoto = this.FindControl<StackPanel>("SPPhoto_" + lastIndex);
            spLastPhoto.IsVisible = false;
            _marketItemData.Photos.RemoveAt(lastIndex);
        }

        private void ClearForm()
        {
            _instance = null;
        }
    }
}

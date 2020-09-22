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
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FreeMarketApp.Views.Controls.MessageBox;
using static FreeMarketOne.ServerCore.MarketManager;

namespace FreeMarketApp.Views.Pages
{
    public class AddEditProductPage : UserControl
    {
        private MarketItemV1 _offer;
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
        public static AddEditProductPage GetInstance()
        {
            return _instance;
        }
        public AddEditProductPage()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(AddEditProductPage).Namespace, typeof(AddEditProductPage).Name));

            _offer = new MarketItemV1();

            this.InitializeComponent();

            PagesHelper.Log(_logger, string.Format("Loading product data of {0} to profile page.", "?"));

            if (_offer.Photos.Count >= 8)
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

            ClearForm();
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

        public void LoadProduct(string signature)
        {
            var offer = FreeMarketOneServer.Current.MarketManager.GetOfferBySignature(signature);

            if (offer != null)
            {
                _offer = offer;

                PagesHelper.Log(Instance._logger, string.Format("Loading editation of my product signature {0}", offer.Signature));

                var tbTitle = Instance.FindControl<TextBox>("TBTitle");
                var tbDescription = Instance.FindControl<TextBox>("TBDescription");
                var tbShipping = Instance.FindControl<TextBox>("TBShipping");
                
                var tbManufacturer = this.FindControl<TextBox>("TBManufacturer");
                var tbFineness = this.FindControl<TextBox>("TBFineness");
                var tbSize = this.FindControl<TextBox>("TBSize");
                var tbWeightInGrams = this.FindControl<TextBox>("TBWeightInGrams");

                var tbPrice = Instance.FindControl<TextBox>("TBPrice");
                var tbPageName = Instance.FindControl<TextBlock>("TBPageName");

                var cbCategory = this.FindControl<ComboBox>("CBCategory");
                var cbDealType = this.FindControl<ComboBox>("CBDealType");
                var cbPriceType = this.FindControl<ComboBox>("CBPriceType");

                tbTitle.Text = _offer.Title;
                tbDescription.Text = _offer.Description;
                tbShipping.Text = _offer.Shipping;
                tbPrice.Text = _offer.Price.ToString();

                tbManufacturer.Text = _offer.Manufacturer;
                tbFineness.Text = _offer.Fineness;
                tbSize.Text = _offer.Size;
                tbWeightInGrams.Text = _offer.WeightInGrams.ToString();

                tbPageName.Text = SharedResources.ResourceManager.GetString("AddEditProduct_EditPageName");

                cbCategory.SelectedItem = cbCategory.Items.OfType<ComboBoxItem>().Single(t => t.Tag.Equals(offer.Category.ToString()));
                cbDealType.SelectedItem = cbDealType.Items.OfType<ComboBoxItem>().Single(t => t.Tag.Equals(offer.DealType.ToString()));
                cbPriceType.SelectedItem = cbPriceType.Items.OfType<ComboBoxItem>().Single(t => t.Tag.Equals(offer.PriceType.ToString()));

                //photos loading
                if ((_offer.Photos != null) && (_offer.Photos.Any()))
                {
                    var skynetHelper = new SkynetHelper();
                    skynetHelper.PreloadPhotos(_offer, Instance._logger);

                    for (int i = 0; i < _offer.Photos.Count; i++)
                    {
                        var spPhoto = Instance.FindControl<StackPanel>("SPPhoto_" + i);
                        var iPhoto = Instance.FindControl<Image>("IPhoto_" + i);

                        spPhoto.IsVisible = true;
                        iPhoto.Source = _offer.PrePhotos[i];
                    }
                }
            }
        }

        public async void ButtonSave_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);
            var approxSpanToNewBlock = FreeMarketOneServer.Current.Configuration.BlockChainMarketPolicy.GetApproxTimeSpanToMineNextBlock();
            var result = await MessageBox.Show(mainWindow,
                string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_SaveMyItem"), approxSpanToNewBlock.TotalSeconds),
                SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                MessageBox.MessageBoxButtons.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                //check form
                var tbTitle = this.FindControl<TextBox>("TBTitle");
                var tbDescription = this.FindControl<TextBox>("TBDescription");
                var tbShipping = this.FindControl<TextBox>("TBShipping");

                var tbManufacturer = this.FindControl<TextBox>("TBManufacturer");
                var tbFineness = this.FindControl<TextBox>("TBFineness");
                var tbSize = this.FindControl<TextBox>("TBSize");
                var tbWeightInGrams = this.FindControl<TextBox>("TBWeightInGrams");

                var tbPrice = this.FindControl<TextBox>("TBPrice");
                var cbCategory = this.FindControl<ComboBox>("CBCategory");
                var cbDealType = this.FindControl<ComboBox>("CBDealType");
                var cbPriceType = this.FindControl<ComboBox>("CBPriceType");

                var errorCount = 0;
                var errorMessages = new StringBuilder();
                var textHelper = new TextHelper();

                if (string.IsNullOrEmpty(tbTitle.Text) || (tbTitle.Text.Length < 10))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_ShortTitle"));
                    errorCount++;
                }
                else
                {
                    if (!textHelper.IsTextValid(tbTitle.Text, true))
                    {
                        errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_InvalidCharsTitle"));
                        errorCount++;
                    }
                }
                if (string.IsNullOrEmpty(tbDescription.Text) || (tbDescription.Text.Length < 50))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_ShortDescription"));
                    errorCount++;
                }
                else
                {
                    if (!textHelper.IsTextValid(tbDescription.Text, true))
                    {
                        errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_InvalidCharsDescription"));
                        errorCount++;
                    }
                }
                if (string.IsNullOrEmpty(tbShipping.Text) || (tbShipping.Text.Length < 2))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_ShortShipping"));
                    errorCount++;
                }
                else
                {
                    if (!textHelper.IsTextValid(tbShipping.Text))
                    {
                        errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_InvalidCharsShipping"));
                        errorCount++;
                    }
                }
                if (string.IsNullOrEmpty(tbPrice.Text) || (tbPrice.Text.Length < 1))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_ShortPrice"));
                    errorCount++;
                }
                else
                {
                    if (!textHelper.IsNumberValid(tbPrice.Text))
                    {
                        errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_InvalidCharsPrice"));
                        errorCount++;
                    }
                }

                if (string.IsNullOrEmpty(tbWeightInGrams.Text) || (tbWeightInGrams.Text.Length < 1))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_WeightInGrams"));
                    errorCount++;
                }
                else
                {
                    if (!textHelper.IsNumberValid(tbWeightInGrams.Text))
                    {
                        errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_InvalidCharsWeightInGrams"));
                        errorCount++;
                    }
                }

                var cbCategoryValue = cbCategory.SelectedItem as ComboBoxItem;
                if (cbCategoryValue.Tag.ToString() == "0")
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_EmptyCategory"));
                    errorCount++;
                }
                var cbDealTypeValue = cbDealType.SelectedItem as ComboBoxItem;
                if (cbDealTypeValue.Tag.ToString() == "0")
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_EmptyDealValue"));
                    errorCount++;
                }
                if (_offer.Photos.Count() == 0)
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_EmptyPhoto"));
                    errorCount++;
                }

                if (errorCount == 0)
                {
                    PagesHelper.Log(_logger, string.Format("Saving new data of product."));

                    //save to chain
                    _offer.Title = tbTitle.Text;
                    _offer.Description = tbDescription.Text;
                    _offer.Shipping = tbShipping.Text;

                    _offer.Manufacturer = tbManufacturer.Text;
                    _offer.Fineness = tbFineness.Text;
                    _offer.Size = tbSize.Text;
                    _offer.WeightInGrams = long.Parse(tbWeightInGrams.Text);             

                    _offer.Category = int.Parse(cbCategoryValue.Tag.ToString());
                    _offer.DealType = int.Parse(cbDealTypeValue.Tag.ToString());
                    _offer.Price = float.Parse(tbPrice.Text.Trim());
                    _offer.PriceType = (cbPriceType.Tag != null && cbPriceType.Tag.ToString() == "1" ? 1 : 0);
                    _offer.State = (int)ProductStateEnum.Default;

                    //get time to next block
                    //upload to sia
                    for (int i = _offer.Photos.Count(); i > 0; i--)
                    {
                        if (!_offer.Photos[i - 1].Contains(SkynetWebPortal.SKYNET_PREFIX))
                        {
                            PagesHelper.Log(_logger, string.Format("Uploading to Skynet {0}.", _offer.Photos[i - 1]));

                            var skynetHelper = new SkynetHelper();
                            var skynetUrl = skynetHelper.UploadToSkynet(_offer.Photos[i - 1], _logger);
                            if (skynetUrl == null)
                            {
                                _offer.Photos.RemoveAt(i - 1);
                            } 
                            else
                            {
                                _offer.Photos[i - 1] = skynetUrl;
                            }
                        }
                    }

                    //sign market data and generating chain connection
                    _offer = FreeMarketOneServer.Current.MarketManager.SignMarketData(_offer);

                    PagesHelper.Log(_logger, string.Format("Propagate new product to chain."));

                    FreeMarketOneServer.Current.MarketPoolManager.AcceptActionItem(_offer);
                    FreeMarketOneServer.Current.MarketPoolManager.PropagateAllActionItemLocal();
                    FreeMarketOneServer.Current.SearchIndexer.Index(_offer,"pending");

                    await MessageBox.Show(mainWindow,
                        string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_Waiting")),
                        SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                        MessageBox.MessageBoxButtons.Ok);

                    MyProductsPage.Instance = null;
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
                var itemIndex = _offer.Photos.Count;
                var spPhoto = this.FindControl<StackPanel>("SPPhoto_" + itemIndex);
                var iPhoto = this.FindControl<Image>("IPhoto_" + itemIndex);

                spPhoto.IsVisible = true;
                iPhoto.Source = new Bitmap(photoPath);

                _offer.Photos.Add(photoPath);

                if (_offer.Photos.Count >= 8)
                {
                    var btAddPhoto = this.FindControl<Button>("BTAddPhoto");
                    btAddPhoto.IsVisible = false;
                }
            }
        }

        public void ButtonRemove_Click(object sender, RoutedEventArgs args)
        {
            var itemIndex = int.Parse(((Button)sender).Tag.ToString());
            var lastIndex = _offer.Photos.Count - 1;

            if (itemIndex != lastIndex)
            {
                for (int i = itemIndex; i < (_offer.Photos.Count - 1); i++)
                {
                    var iPhoto = this.FindControl<Image>("IPhoto_" + i);
                    var iPhotoNext = this.FindControl<Image>("IPhoto_" + (i + 1));

                    iPhoto.Source = iPhotoNext.Source;
                    _offer.Photos[i] = _offer.Photos[i + 1];
                }
            }

            //hide lastindex
            var spLastPhoto = this.FindControl<StackPanel>("SPPhoto_" + lastIndex);
            spLastPhoto.IsVisible = false;
            _offer.Photos.RemoveAt(lastIndex);
        }

        private void ClearForm()
        {
            _instance = null;
        }
    }
}

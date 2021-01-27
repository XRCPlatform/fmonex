using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Common;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Markets;
using FreeMarketOne.Pools;
using FreeMarketOne.Skynet;
using Serilog;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FreeMarketOne.Markets.MarketManager;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

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
            if (FMONE.Current.Logger != null)
                _logger = FMONE.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
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

            if (result == MessageBox.MessageBoxResult.Yes)
            {
                PagesHelper.Switch(mainWindow, MyProductsPage.Instance);
                ClearForm();
            }
        }

        public void LoadProduct(string signature)
        {
            var offer = FMONE.Current.MarketManager.GetOfferBySignature(
                signature,
                FMONE.Current.MarketPoolManager,
                FMONE.Current.MarketBlockChainManager);

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
                var tbTBXRCReceivingAddress = this.FindControl<TextBox>("TBXRCReceivingAddress");

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
                tbTBXRCReceivingAddress.Text = _offer.XRCReceivingAddress;
                tbPageName.Text = SharedResources.ResourceManager.GetString("AddEditProduct_EditPageName");

                cbCategory.SelectedItem = cbCategory.Items.OfType<ComboBoxItem>().Single(t => t.Tag.Equals(offer.Category.ToString()));
                cbDealType.SelectedItem = cbDealType.Items.OfType<ComboBoxItem>().Single(t => t.Tag.Equals(offer.DealType.ToString()));

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
            var approxSpanToNewBlock = FMONE.Current.Configuration.BlockChainMarketPolicy.GetApproxTimeSpanToMineNextBlock();
            var result = await MessageBox.Show(mainWindow,
                string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_SaveMyItem"), approxSpanToNewBlock.TotalSeconds),
                SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                MessageBox.MessageBoxButtons.YesNo);

            if (result == MessageBox.MessageBoxResult.Yes)
            {
                //check form
                var tbTitle = this.FindControl<TextBox>("TBTitle");
                var tbDescription = this.FindControl<TextBox>("TBDescription");
                var tbShipping = this.FindControl<TextBox>("TBShipping");

                var tbManufacturer = this.FindControl<TextBox>("TBManufacturer");
                var tbFineness = this.FindControl<TextBox>("TBFineness");
                var tbSize = this.FindControl<TextBox>("TBSize");
                var tbWeightInGrams = this.FindControl<TextBox>("TBWeightInGrams");
                var tbTBXRCReceivingAddress = this.FindControl<TextBox>("TBXRCReceivingAddress");
                var tbPrice = this.FindControl<TextBox>("TBPrice");
                var cbCategory = this.FindControl<ComboBox>("CBCategory");
                var cbDealType = this.FindControl<ComboBox>("CBDealType");
                //var cbPriceType = this.FindControl<ComboBox>("CBPriceType");

                var errorCount = 0;
                var errorMessages = new StringBuilder();
                var textHelper = new TextHelper();

                var cbCategoryValue = cbCategory.SelectedItem as ComboBoxItem;
                if (cbCategoryValue.Tag.ToString() == "0")
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_EmptyCategory"));
                    errorCount++;
                }

                if (string.IsNullOrEmpty(tbTitle.Text) || (tbTitle.Text.Length < 10))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_ShortTitle"));
                    errorCount++;
                }
                else
                {
                    if (!textHelper.IsCleanTextValid(tbTitle.Text, true))
                    {
                        errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_InvalidCharsShortTitle"));
                        errorCount++;
                    }

                    if (!textHelper.IsWithoutBannedWords(tbTitle.Text))
                    {
                        errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_BannedWordsShortTitle"));
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
                    if (!textHelper.IsTextNotDangerous(tbDescription.Text))
                    {
                        errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_InvalidCharsShortDescription"));
                        errorCount++;
                    }

                    if (!textHelper.IsWithoutBannedWords(tbDescription.Text))
                    {
                        errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_BannedWordsShortDescription"));
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
                    if (!textHelper.IsCleanTextValid(tbShipping.Text, true))
                    {
                        errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_InvalidCharsShortShipping"));
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

                if (string.IsNullOrEmpty(tbTBXRCReceivingAddress.Text) || (tbTBXRCReceivingAddress.Text.Length < 20))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_EmptyTBXRCReceivingAddress"));
                    errorCount++;
                }
                else
                {
                    if (!textHelper.IsCleanTextValid(tbTBXRCReceivingAddress.Text))
                    {
                        errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_InvalidCharsTBXRCReceivingAddress"));
                        errorCount++;
                    }
                }

                //validation based on category type
                var selectedCategory = (MarketCategoryEnum)Enum.Parse(typeof(MarketCategoryEnum), cbCategoryValue.Tag.ToString());
                switch (selectedCategory)
                {
                    case MarketCategoryEnum.Copper:
                    case MarketCategoryEnum.Gold:
                    case MarketCategoryEnum.Jewelry:
                    case MarketCategoryEnum.Palladium:
                    case MarketCategoryEnum.Platinum:
                    case MarketCategoryEnum.Rhodium:
                    case MarketCategoryEnum.Silver:

                        if (string.IsNullOrEmpty(tbWeightInGrams.Text) || !textHelper.IsNumberValid(tbWeightInGrams.Text))
                        {
                            errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_InvalidCharsWeightInGrams"));
                            errorCount++;
                        }

                        if (string.IsNullOrEmpty(tbManufacturer.Text) || (tbManufacturer.Text.Length < 1))
                        {
                            errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_EmptyManufacturer"));
                            errorCount++;
                        }
                        else
                        {
                            if (!textHelper.IsCleanTextValid(tbManufacturer.Text, true))
                            {
                                errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_InvalidCharsManufacturer"));
                                errorCount++;
                            }
                        }

                        if (string.IsNullOrEmpty(tbFineness.Text) || (tbFineness.Text.Length < 1))
                        {
                            errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_EmptyFineness"));
                            errorCount++;
                        }
                        else
                        {
                            if (!textHelper.IsCleanTextValid(tbFineness.Text, true))
                            {
                                errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_InvalidCharsFineness"));
                                errorCount++;
                            }
                        }

                        if (string.IsNullOrEmpty(tbSize.Text) || (tbSize.Text.Length < 1))
                        {
                            errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_EmptySize"));
                            errorCount++;
                        }
                        else
                        {
                            if (!textHelper.IsCleanTextValid(tbSize.Text, true))
                            {
                                errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_AddEditProduct_InvalidCharsSize"));
                                errorCount++;
                            }
                        }

                        break;
                    default:
                        break;
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
                    if (!string.IsNullOrEmpty(tbWeightInGrams.Text)) _offer.WeightInGrams = long.Parse(tbWeightInGrams.Text);

                    _offer.Category = int.Parse(cbCategoryValue.Tag.ToString());
                    _offer.DealType = int.Parse(cbDealTypeValue.Tag.ToString());
                    _offer.Price = float.Parse(tbPrice.Text.Trim());
                    _offer.State = (int)ProductStateEnum.Default;
                    _offer.XRCReceivingAddress = tbTBXRCReceivingAddress.Text;

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
                    //for (int i = 0; i < 50; i++)
                    //{
                    //    MarketItemV1 newOffer = new MarketItemV1();
                    //    newOffer.Category = _offer.Category;
                    //    newOffer.DealType = _offer.DealType;
                    //    newOffer.Description = _offer.Description;
                    //    newOffer.Fineness = _offer.Fineness;
                    //    newOffer.Manufacturer = _offer.Manufacturer;
                    //    newOffer.Photos = _offer.Photos;
                    //    newOffer.PrePhotos = _offer.PrePhotos;
                    //    newOffer.PreTitlePhoto = _offer.PreTitlePhoto;
                    //    newOffer.Price = _offer.Price;
                    //    newOffer.PriceType = _offer.PriceType;
                    //    newOffer.Shipping = _offer.Shipping;
                    //    newOffer.Size = _offer.Size;
                    //    newOffer.Title = _offer.Title + " " + i;
                    //    newOffer.WeightInGrams = _offer.WeightInGrams;
                    //    newOffer.XRCReceivingAddress = _offer.XRCReceivingAddress;
                    //    newOffer = FMONE.Current.Markets.SignMarketData(newOffer, FMONE.Current.Users.PrivateKey);
                    //    var resultPool2 = FMONE.Current.MarketPoolManager.AcceptActionItem(newOffer);
                    //    if (resultPool2 == null)
                    //    {
                    //        FMONE.Current.MarketPoolManager.PropagateAllActionItemLocal();
                    //    }
                    //}

                    //sign market data and generating chain connection
                    var signedOffer = FMONE.Current.MarketManager.SignMarketData(_offer, FMONE.Current.UserManager.PrivateKey);
                    ////things like photos could be accidentaly mutated on different thread
                    //reference types could mutate, but we signed binary values. Must send disconected copy to network, 
                    //and initialize locally from clone to prevent mutations
                    _offer = signedOffer.Clone<MarketItemV1>();
                   
                    PagesHelper.Log(_logger, string.Format("Propagate new product to chain."));

                    var resultPool = FMONE.Current.MarketPoolManager.AcceptActionItem(signedOffer);
                    if (resultPool == null)
                    {
                        await MessageBox.Show(mainWindow,
                            string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_Waiting")),
                            SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                            MessageBox.MessageBoxButtons.Ok);

                        MyProductsPage.Instance = null;
                        PagesHelper.Switch(mainWindow, MyProductsPage.Instance);
                        ClearForm();
                    } 
                    else
                    {
                        await MessageBox.Show(mainWindow,
                            string.Format(SharedResources.ResourceManager.GetString("Dialog_Error_" + resultPool.Value.ToString())),
                            SharedResources.ResourceManager.GetString("Dialog_Error_Title"),
                            MessageBox.MessageBoxButtons.Ok);

                        //not allow change in case of another state is in process
                        if (resultPool == PoolManagerStates.Errors.StateOfItemIsInProgress)
                        {
                            MyProductsPage.Instance = null;
                            PagesHelper.Switch(mainWindow, MyProductsPage.Instance);
                            ClearForm();
                        }
                    }

                } else {

                    await MessageBox.Show(mainWindow,
                       errorMessages.ToString(),
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
            await Task.Run(
                () => {
                    for (int i = _offer.Photos.Count(); i > 0; i--)
                    {
                        if (!_offer.Photos[i - 1].Contains(SkynetWebPortal.SKYNET_PREFIX))
                        {
                            PagesHelper.Log(_logger, string.Format("Uploading to Skynet {0}.", _offer.Photos[i - 1]));

                            var skynetHelper = new SkynetHelper();
                            var skynetUrl = skynetHelper.UploadToSkynet(_offer.Photos[i - 1], _logger);
                            if (skynetUrl != null && !_offer.Photos[i - 1].Contains(SkynetWebPortal.SKYNET_PREFIX)) //do not modify if data was set
                            {
                                _offer.Photos[i - 1] = skynetUrl;
                            }
                        }
                    }
                });
            
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

            if (_offer.Photos.Count < 8)
            {
                var btAddPhoto = this.FindControl<Button>("BTAddPhoto");
                btAddPhoto.IsVisible = true;
            }
        }

        private void ClearForm()
        {
            _instance = null;
        }
    }
}

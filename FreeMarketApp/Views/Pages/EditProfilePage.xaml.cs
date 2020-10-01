using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Skynet;
using Serilog;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketApp.Views.Pages
{
    public class EditProfilePage : UserControl
    {
        private static EditProfilePage _instance;
        private ILogger _logger;
        private UserDataV1 _userData;

        public static EditProfilePage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new EditProfilePage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
        public static EditProfilePage GetInstance()
        {
            return _instance;
        }
        public EditProfilePage()
        {
            if (FMONE.Current.Logger != null)
                _logger = FMONE.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(EditProfilePage).Namespace, typeof(EditProfilePage).Name));

            this.InitializeComponent();

            if (FMONE.Current.Users != null)
            {
                PagesHelper.Log(_logger, string.Format("Loading user data of current user to edit profile page."));

                _userData = FMONE.Current.Users.UserData;

                var tbUserName = this.FindControl<TextBox>("TBUserName");
                var tbDescription = this.FindControl<TextBox>("TBDescription");

                tbUserName.Text = _userData.UserName;
                tbDescription.Text = _userData.Description;

                if (!string.IsNullOrEmpty(_userData.Photo) && (_userData.Photo.Contains(SkynetWebPortal.SKYNET_PREFIX)))
                {
                    var iPhoto = this.FindControl<Image>("IPhoto");

                    var skynetHelper = new SkynetHelper();
                    var skynetStream = skynetHelper.DownloadFromSkynet(_userData.Photo, _logger);
                    iPhoto.Source = new Bitmap(skynetStream);
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonBack_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyProfilePage.Instance);

            ClearForm();
        }

        public void ButtonMyReviews_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyReviewsPage.Instance);

            ClearForm();
        }

        public void ButtonMyProfile_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyProfilePage.Instance);

            ClearForm();
        }

        public void ButtonCancel_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyProfilePage.Instance);

            ClearForm();
        }

        public async void ButtonSave_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);
            var approxSpanToNewBlock = FMONE.Current.Configuration.BlockChainMarketPolicy.GetApproxTimeSpanToMineNextBlock();
            var result = await MessageBox.Show(mainWindow,
                string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_SaveMyProfile"), approxSpanToNewBlock.TotalSeconds),
                SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                MessageBox.MessageBoxButtons.YesNo);

            if (result == MessageBox.MessageBoxResult.Yes)
            {

                ////check form
                var tbUserName = this.FindControl<TextBox>("TBUserName");
                var tbDescription = this.FindControl<TextBox>("TBDescription");

                var errorCount = 0;
                var errorMessages = new StringBuilder();
                var textHelper = new TextHelper();

                if (string.IsNullOrEmpty(tbUserName.Text) || (tbUserName.Text.Length < 10))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_ShortUserName"));
                    errorCount++;
                } 
                else
                {
                    if (!textHelper.IsTextValid(tbUserName.Text))
                    {
                        errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_InvalidCharsUserName"));
                        errorCount++;
                    }
                }

                if (string.IsNullOrEmpty(tbDescription.Text) || (tbDescription.Text.Length < 50))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_ShortDescription"));
                    errorCount++;
                } 
                else
                {
                    if (!textHelper.IsTextValid(tbDescription.Text, true))
                    {
                        errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_FirstRun_InvalidCharsDescription"));
                        errorCount++;
                    }
                }

                if (errorCount == 0)
                {
                    PagesHelper.Log(_logger, string.Format("Saving new user data of current user to chain username {0} description {1}.", tbUserName.Text, tbDescription.Text));

                    //upload to sia
                    if (_userData.Photo != null && !_userData.Photo.Contains(SkynetWebPortal.SKYNET_PREFIX))
                    {
                        PagesHelper.Log(_logger, string.Format("Uploading to Skynet {0}.", _userData.Photo));

                        var skynetHelper = new SkynetHelper();
                        var skynetUrl = skynetHelper.UploadToSkynet(_userData.Photo, _logger);
                        if (skynetUrl == null)
                        {
                            _userData.Photo = null;;
                        }
                        else
                        {
                            _userData.Photo = skynetUrl;
                        }
                    }

                    var updatedUserData = FMONE.Current.Users.SignUserData(tbUserName.Text, tbDescription.Text, _userData);

                    FMONE.Current.Users.SaveUserData(
                        updatedUserData,
                        FMONE.Current.Configuration.FullBaseDirectory,
                        FMONE.Current.Configuration.BlockChainUserPath);

                    PagesHelper.SetUserData(_logger, mainWindow);
                    PagesHelper.Log(_logger, string.Format("Propagating new user data of current user to chain."));

                    FMONE.Current.BasePoolManager.AcceptActionItem(updatedUserData);
                    FMONE.Current.BasePoolManager.PropagateAllActionItemLocal();

                    await MessageBox.Show(mainWindow,
                        string.Format(SharedResources.ResourceManager.GetString("Dialog_Confirmation_Waiting")),
                        SharedResources.ResourceManager.GetString("Dialog_Confirmation_Title"),
                        MessageBox.MessageBoxButtons.Ok);

                    PagesHelper.Switch(mainWindow, MainPage.Instance);
                    ClearForm();
                }
                else
                {
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

        public async void ButtonChangePhoto_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);
            string photoPath = await GetPhotoPath(mainWindow);

            if (!string.IsNullOrEmpty(photoPath))
            {
                PagesHelper.Log(_logger, string.Format("We have a new photo for current user profile."));

                var iPhoto = this.FindControl<Image>("IPhoto");

                iPhoto.Source = new Bitmap(photoPath);
                _userData.Photo = photoPath;
            }
        }

        private void ClearForm()
        {
            _instance = null;
        }
    }
}

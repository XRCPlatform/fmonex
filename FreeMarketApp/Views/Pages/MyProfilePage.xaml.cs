using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore;
using Serilog;

namespace FreeMarketApp.Views.Pages
{
    public class MyProfilePage : UserControl
    {
        private static MyProfilePage _instance;
        private UserDataV1 _userData;
        private ILogger _logger;

        public static MyProfilePage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MyProfilePage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public MyProfilePage()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(MyProfilePage).Namespace, typeof(MyProfilePage).Name));

            this.InitializeComponent();

            PagesHelper.Log(_logger, string.Format("Loading user data of current user to profile page."));

            _userData = FreeMarketOneServer.Current.UserManager.UserData;

            var tbUserName = this.FindControl<TextBox>("TBUserName");
            var tbDescription = this.FindControl<TextBox>("TBDescription");

            tbUserName.Text = _userData.UserName;
            tbDescription.Text = _userData.Description;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonBack_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MainPage.Instance);

            ClearForm();
        }

        public void ButtonMyReviews_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyReviewsPage.Instance);

            ClearForm();
        }

        public void ButtonEdit_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, EditProfilePage.Instance);

            ClearForm();
        }

        private void ClearForm()
        {
            _instance = null;
        }
    }
}

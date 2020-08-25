﻿using Avalonia.Controls;
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
            set 
            {
                _instance = value;
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

                if (errorCount == 0)
                {
                    //save to chain
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

        private void ClearForm()
        {
            _instance = null;
        }
    }
}

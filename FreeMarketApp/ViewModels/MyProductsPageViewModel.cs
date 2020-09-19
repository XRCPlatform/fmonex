using FreeMarketOne.DataStructure.Objects.BaseItems;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FreeMarketApp.ViewModels
{
    public class MyProductsPageViewModel : ViewModelBase
    {
        public MyProductsPageViewModel(IEnumerable<MarketItem> activeItems, IEnumerable<MarketItem> soldItems)
        {
            Items = new ObservableCollection<MarketItem>(activeItems);
            SoldItems = new ObservableCollection<MarketItem>(soldItems);
        }

        public ObservableCollection<MarketItem> Items { get; set; }
        public ObservableCollection<MarketItem> SoldItems { get; set; }
    }
}

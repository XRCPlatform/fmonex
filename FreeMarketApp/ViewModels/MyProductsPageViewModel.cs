using FreeMarketOne.DataStructure.Objects.BaseItems;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FreeMarketApp.ViewModels
{
    public class MyProductsPageViewModel : ViewModelBase
    {
        public MyProductsPageViewModel(IEnumerable<MarketItemV1> activeItems, IEnumerable<MarketItemV1> soldItems)
        {
            Items = new ObservableCollection<MarketItemV1>(activeItems);
            SoldItems = new ObservableCollection<MarketItemV1>(soldItems);
        }

        public ObservableCollection<MarketItemV1> Items { get; set; }
        public ObservableCollection<MarketItemV1> SoldItems { get; set; }
    }
}

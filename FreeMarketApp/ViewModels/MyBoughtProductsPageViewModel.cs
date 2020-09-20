using FreeMarketOne.DataStructure.Objects.BaseItems;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FreeMarketApp.ViewModels
{
    public class MyBoughtProductsPageViewModel : ViewModelBase
    {
        public MyBoughtProductsPageViewModel(IEnumerable<MarketItem> items)
        {
            Items = new ObservableCollection<MarketItem>(items);
        }

        public ObservableCollection<MarketItem> Items { get; set; }
    }
}

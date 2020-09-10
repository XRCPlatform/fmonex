using FreeMarketOne.DataStructure.Objects.BaseItems;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FreeMarketApp.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        public MainPageViewModel(IEnumerable<MarketItemV1> items)
        {
            Items = new ObservableCollection<MarketItemV1>(items);
        }

        public ObservableCollection<MarketItemV1> Items { get; set; }
    }
}

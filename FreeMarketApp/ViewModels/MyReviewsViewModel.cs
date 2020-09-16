using FreeMarketOne.DataStructure.Objects.BaseItems;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FreeMarketApp.ViewModels
{
    public class MyReviewsViewModel : ViewModelBase
    {
        public MyReviewsViewModel(IEnumerable<ReviewUserDataV1> items)
        {
            Items = new ObservableCollection<ReviewUserDataV1>(items);
        }

        public ObservableCollection<ReviewUserDataV1> Items { get; set; }
    }
}
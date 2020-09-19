using FreeMarketOne.DataStructure.Objects.BaseItems;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FreeMarketApp.ViewModels
{
    public class PublicProfileReviewsViewModel : ViewModelBase
    {
        public PublicProfileReviewsViewModel(IEnumerable<ReviewUserDataV1> items)
        {
            Items = new ObservableCollection<ReviewUserDataV1>(items);
        }

        public ObservableCollection<ReviewUserDataV1> Items { get; set; }
    }
}
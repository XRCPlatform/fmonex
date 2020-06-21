using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.ServerCore.ViewModels
{

    public class SplashWindowViewModel : ViewModelBase
    {
        private string _startupProgressText;

        public string StartupProgressText
        {
            get => _startupProgressText;
            set => this.RaiseAndSetIfChanged(ref _startupProgressText, value);
        }

        public SplashWindowViewModel()
        {

        }
    }
}

using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FreeMarketApp.ViewModels;
using FreeMarketApp.Views;
using System.Net.Http;
using System.Security.Authentication;

namespace FreeMarketApp.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string caption;

        public string Caption
        {
            get => caption;
            set => this.RaiseAndSetIfChanged(ref caption, value);
        }

        public string Greeting = "Hello World!x";

        public MainWindowViewModel()
        {

        }
    }

}

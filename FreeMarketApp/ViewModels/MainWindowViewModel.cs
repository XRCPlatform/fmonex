using ReactiveUI;

namespace FreeMarketApp.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string _startupProgressText;

        public string StartupProgressText
        {
            get => _startupProgressText;
            set => this.RaiseAndSetIfChanged(ref _startupProgressText, value);
        }

        public MainWindowViewModel()
        {

        }
    }
}

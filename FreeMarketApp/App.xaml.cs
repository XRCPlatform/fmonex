using Avalonia;
using Avalonia.Markup.Xaml;

namespace FreeMarketApp
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

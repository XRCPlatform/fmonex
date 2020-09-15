using Avalonia.Markup.Xaml;
using AvaloniaStyles = Avalonia.Styling.Styles;

namespace FreeMarketApp.Styles
{
    public class DarkTheme : AvaloniaStyles
    {
        public DarkTheme() => AvaloniaXamlLoader.Load(this);
    }
}
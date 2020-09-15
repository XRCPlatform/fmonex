using Avalonia.Markup.Xaml.Styling;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace FreeMarketApp.Helpers
{
    internal static class ThemeHelper
    {
        internal static StyleInclude DarkTheme = new StyleInclude(new Uri("avares://FreeMarketApp/App.xaml"))
        {
            Source = new Uri("avares://FreeMarketApp/Styles/DarkTheme.xml")
        };

        internal static StyleInclude LightTheme = new StyleInclude(new Uri("avares://FreeMarketApp/App.xaml"))
        {
            Source = new Uri("avares://FreeMarketApp/Styles/LightTheme.xml")
        };

        internal static StyleInclude GetTheme()
        {
            var fullBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!fullBaseDirectory.StartsWith('/'))
                {
                    fullBaseDirectory.Insert(0, "/");
                }
            }

            //Configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(fullBaseDirectory)
                .AddJsonFile("appsettings.json", true, false);
            var configFile = builder.Build();

            var defaultTheme = LightTheme;

            var configTheme = configFile.GetSection("FreeMarketOneConfiguration")["Theme"];
            if (!string.IsNullOrEmpty(configTheme))
            {
                if (configTheme.ToLower() == "dark")
                {
                    return DarkTheme;
                }
            }

            return defaultTheme;
        }
    }
}

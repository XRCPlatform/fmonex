using Avalonia.Markup.Xaml.Styling;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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

        internal const string DARK_THEME = "Dark";
        internal const string LIGHT_THEME = "Light";

        private static string GetBaseDirectory()
        {
            var fullBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!fullBaseDirectory.StartsWith('/'))
                {
                    fullBaseDirectory.Insert(0, "/");
                }
            }

            return fullBaseDirectory;
        }

        internal static string GetThemeName()
        {
            //Configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(GetBaseDirectory())
                .AddJsonFile("appsettings.json", true, false);
            var configFile = builder.Build();

            var defaultTheme = LightTheme;

            var configTheme = configFile.GetSection("FreeMarketOneConfiguration")["Theme"];
            if (!string.IsNullOrEmpty(configTheme))
            {
                if (configTheme.ToLower() == DARK_THEME.ToLower())
                {
                    return DARK_THEME;
                }
            }

            return LIGHT_THEME;
        }

        internal static StyleInclude GetTheme()
        {
            //Configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(GetBaseDirectory())
                .AddJsonFile("appsettings.json", true, false);
            var configFile = builder.Build();

            var defaultTheme = LightTheme;

            var configTheme = configFile.GetSection("FreeMarketOneConfiguration")["Theme"];
            if (!string.IsNullOrEmpty(configTheme))
            {
                if (configTheme.ToLower() == DARK_THEME.ToLower())
                {
                    return DarkTheme;
                }
            }

            return defaultTheme;
        }

        internal static void SetTheme(string theme)
        {
            var fullBaseDirectory = GetBaseDirectory();

            var filePath = Path.Combine(fullBaseDirectory, "appsettings.json");
            string json = File.ReadAllText(filePath);
            dynamic jsonObj = JsonConvert.DeserializeObject(json);

            jsonObj["FreeMarketOneConfiguration"]["Theme"] = theme;

            string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
            File.WriteAllText(filePath, output);
        }
    }
}

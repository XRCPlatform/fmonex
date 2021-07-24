using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace FreeMarketOne.ServerCore
{
    public interface IDirectoryHelper
    {
        void CreateDirectory(string dir);
    }
    public class DirectoryHelper : IDirectoryHelper
    {
        public virtual void CreateDirectory(string dir)
        {
            Directory.CreateDirectory(dir);
        }
    }
    
    /**
     * Process data directory strings and their defaults.
     */
    public class DataDir
    {
        public string DataDirPath { get; set; }
        public string ConfigFileName { get; } = "appsettings.json";

        public IDirectoryHelper DirectoryHelper { get; } = new DirectoryHelper();

        public string ConfigFile => Path.GetFullPath(Path.Combine(DataDirPath, ConfigFileName));

        public DataDir() :
            this(Path.GetFullPath(AppContext.BaseDirectory))
        {
        }

        public DataDir(string dataDir = null, string configFileName = null)
        {
            if (configFileName != null)
            {
                ConfigFileName = configFileName;
            }
            
            DataDirPath = dataDir;
            if (dataDir == null)
            {
                dataDir = GetDefaultDataDirPath();
            }

            DataDirPath = Path.GetFullPath(dataDir);
            GeneratePathAndConfig(DataDirPath);
        }
        
        /**
         * Build a default appsettings.json JSON.
         */
        public string MakeAppSettings()
        {
            dynamic appSettings = new ExpandoObject();
            appSettings.FreeMarketOneConfiguration = new ExpandoObject();
            appSettings.FreeMarketOneConfiguration.ServerEnvironment = "Main";
            appSettings.FreeMarketOneConfiguration.ListenersUseTor = true;
            appSettings.FreeMarketOneConfiguration.MinimalPeerAmount = 1;
            appSettings.FreeMarketOneConfiguration.OnionSeeds = new List<string>();
            ((List<string>)appSettings.FreeMarketOneConfiguration.OnionSeeds).Append(
                "zx2lvuufqqdurahsgc7jf3qaugc7yme7t4ypjmji2lx75zmnddluotad.onion:80:zx2lvuufqqdurahsgc7jf3qaugc7yme7t4ypjmji2lx75zmnddluotad.onion:9111:9112:04d70636420fb40caa0974afa63b29506a727cce777f89bf7f22d368edf4be49e2fcfc307a98b093a79ca095069edb42cce1ce830ef8dcc5a3b40f75c5c687d518"
            );
            return JsonConvert.SerializeObject(appSettings);
        }

        /**
         * Get appsettings.json
         */
        public IConfigurationRoot GetAppSettings(string path)
        {
            //Configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(DataDirPath)
                .AddJsonFile(ConfigFile, true, false);
            var configFile = builder.Build();
            return configFile;
        }
        
        /**
         * Create a path and default appsettings.json file
         */
        public void GeneratePathAndConfig(string path)
        {
            if (!Directory.Exists(path))
            {
                DirectoryHelper.CreateDirectory(path);
            }
            
            if (File.Exists(ConfigFile)) return;

            using (var file = File.Open(ConfigFile, FileMode.OpenOrCreate))
            {
                file.Write(Encoding.UTF8.GetBytes(MakeAppSettings()));
            }
        }

        /**
         * Make a OS-specific data directory path.
         * 
         * Linux, macOS: /home/$USER/.fmone/
         * Windows: C:\Users\$USER\AppData\.fmone
         */
        private string GetDefaultDataDirPath()
        {
            var path = "";
            if (OSPlatform.Windows == default)
            {
                var localAppData = Environment.GetEnvironmentVariable("APPDATA");
                if (string.IsNullOrEmpty(localAppData))
                {
                    throw new DirectoryNotFoundException("APPDATA directory was not found for creating a datadir.");
                }

                path = Path.Combine(localAppData, ".fmone");
            }
            else
            {

                // Linux, macOS

                var home = Environment.GetEnvironmentVariable("HOME");
                if (string.IsNullOrEmpty(home))
                {
                    throw new DirectoryNotFoundException("HOME directory was not found for creating a datadir.");
                }

                path = Path.Combine(home, ".fmone");
            }
            
            DataDirPath = path;
            return path;
        }
    }
}
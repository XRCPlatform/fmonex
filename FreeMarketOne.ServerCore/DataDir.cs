using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Lucene.Net.Search;
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
            appSettings.FreeMarketOneConfiguration.ServerEnvironment = "Test";
            appSettings.FreeMarketOneConfiguration.ListenersUseTor = true;
            appSettings.FreeMarketOneConfiguration.MinimalPeerAmount = 1;
            appSettings.FreeMarketOneConfiguration.OnionSeeds = new List<string>();
            ((List<string>)appSettings.FreeMarketOneConfiguration.OnionSeeds).Append(
                "dleu464lfj6xqjyp.onion:80:dleu464lfj6xqjyp.onion:9113:9114:04dd48b8ce0cf21d1c37e7e460ac0cfcb88ddc7c08b2f25e93c13399f9920e8ec406728238fdcb34693a104d59d35cfd317e9e20bc98ac1aacb0086b565fc9f676"
            );
            return JsonConvert.SerializeObject(appSettings);
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
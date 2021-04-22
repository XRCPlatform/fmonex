using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FreeMarketOne.ServerCore;
using FreeMarketOne.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FreeMarketOne.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var fmone = FreeMarketOneServer.Current;
            CommandLineOptions.Parse(args, o =>
            {
                if (o.ConfigFile != null)
                {
                    o.DataDir = Path.GetDirectoryName(Path.GetFullPath(o.ConfigFile));
                    o.ConfigFile = (Path.GetFileName(o.ConfigFile));
                }
                fmone.DataDir = o.DataDir != null ? 
                    new DataDir(o.DataDir, o.ConfigFile) : 
                    new DataDir();

            });
            var userManager = new UserManager(FreeMarketOneServer.Current.MakeConfiguration());
            var appSettings = fmone.DataDir.GetAppSettings(fmone.DataDir.ConfigFile);
            var password = appSettings.GetSection("FreeMarketOneConfiguration")["Password"];

            if (password == null)
            {
                Console.WriteLine("Need a password to boot API server.");
                Environment.Exit(1);
                return;
            }
            
            FreeMarketOneServer.Current.Initialize(password, null);
            Thread.Sleep(2000);
            
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}
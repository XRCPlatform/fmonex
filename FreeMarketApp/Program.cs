using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Avalonia;
using Avalonia.Logging.Serilog;
using FreeMarketApp.ViewModels;
using FreeMarketApp.Views;
using TorSocksWebProxy;

namespace FreeMarketApp
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args) => BuildAvaloniaApp().Start(AppMain, args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug()
                .UseReactiveUI();

        // Your application's entry point. Here you can initialize your MVVM framework, DI
        // container, etc.
        private static void AppMain(Application app, string[] args)
        {
            //var window = new MainWindow
            //{
            //    Content new MainWindowViewModel(),
            //    DataContext = new MainWindowViewModel(),
            //};

            //((MainWindowViewModel)window.DataContext).Greeting = "test";
            //    RunParallel(2, "https://check.torproject.org/");

            Console.WriteLine("loading Storage-Providers:");
            jaindb.jDB.loadPlugins(); //never forget this step !
            Console.WriteLine("");

            //Genereate a test JSON
            string sJson = "{ \"prop1\": \"id1\", \"prop2\" : \"bla bla\" }";

            //Store JSON and set Object-Identifier to "OBJ1"
            string sHash = jaindb.jDB.UploadFull(sJson, "OBJ1");

            //Get OBJ1 back from JainDB
            var jObj1 = jaindb.jDB.GetFull("OBJ1");

            //Add an additional key Attribute
            jObj1.Add("#name", "Object1");

            //Upload JSON again
            string sHash2 = jaindb.jDB.UploadFull(jObj1.ToString(), "OBJ1");

            //Get OBJ1 back from JainDB by using the key #name
            string sID = jaindb.jDB.LookupID("#name", "Object1");
            var jObj2 = jaindb.jDB.GetFull(sID);
            Console.WriteLine(jObj2.ToString(Newtonsoft.Json.Formatting.Indented));



            var window = new MainWindow();
            window.DataContext = new MainWindowViewModel();
            ((MainWindowViewModel)window.DataContext).Caption = "tesxt";
            ((MainWindowViewModel)window.DataContext).Greeting = "test";

            ////// wait until the user presses enter
            ////Console.WriteLine("");
            ////Console.WriteLine("Press enter to continue...");
            ////Console.ReadLine();

            app.Run(window);

            //((MainWindowViewModel)window.DataContext).Caption = "tesxtXXX";
            //((MainWindowViewModel)window.DataContext).Greeting = "testCCCC";
        }
    }
}


using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.ViewModels;
using FreeMarketApp.Views;
using FreeMarketOne.DataStructure;
using FreeMarketOne.ServerCore;
using Libplanet;
using Libplanet.Blockchain;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketApp
{
    public class App : Application
    {
        private static MainWindow mainWindow;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                 desktop.Exit += OnExit;
                 desktop.Startup += OnStart;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static void OnStart(object sender, EventArgs e)
        {
            if (sender is IClassicDesktopStyleApplicationLifetime desktop)
            {
                async void AppAsyncLoadingStart()
                {
                    var splashViewModel = new SplashWindowViewModel();
                    splashViewModel.StartupProgressText = "Loading...";
                    var splashWindow = new SplashWindow { DataContext = splashViewModel };
                    splashWindow.Show();
                    await Task.Delay(10);

                    desktop.MainWindow = await GetAppLoadingAsync();
                    desktop.MainWindow.Show();
                    desktop.MainWindow.Activate();

                    if (splashWindow != null)
                    {
                        splashWindow.Close();
                    }
                }

                AppAsyncLoadingStart();
            }
        }

        private static async Task<MainWindow> GetAppLoadingAsync()
        {
            await Task.Run(() =>
            {
                FMONE.Current.BaseBlockChainChangedEvent += new EventHandler<BlockChain<BaseAction>.TipChangedEventArgs>(BaseBlockChainChanged);
                FMONE.Current.FreeMarketOneServerLoadedEvent += ServerLoadedEvent;
                FMONE.Current.MarketBlockClearedOldersEvent += new EventHandler<List<HashDigest<SHA256>>>(MarketBlockClearedOldersChanged);
                FMONE.Current.MarketBlockChainChangedEvent += new EventHandler<BlockChain<MarketAction>.TipChangedEventArgs>(MarketBlockChainChangedEvent);
                FMONE.Current.NetworkHearbeatEvent += new EventHandler<NetworkHearbeatArgs>(NetworkHeartbeatEvent);

                FMONE.Current.Initialize();
            }).ConfigureAwait(true);

            mainWindow = new MainWindow { DataContext = new MainWindowViewModel() };

            return mainWindow;
        }

        private static void NetworkHeartbeatEvent(object sender, NetworkHearbeatArgs e)
        {
            PagesHelper.SetServerData(
                mainWindow, 
                e.IsBaseChainNetworkConnected && e.IsMarketChainNetworkConnected, 
                e.IsTorUp, 
                e.PeerCount);
        }

        private static void MarketBlockChainChangedEvent(object sender, BlockChain<MarketAction>.TipChangedEventArgs e)
        {
            var block = FMONE.Current.MarketBlockChainManager.BlockChain?.Tip;
            FMONE.Current.SearchIndexer.IndexBlock(block);
        }

        private static void BaseBlockChainChanged(object sender, EventArgs e)
        {
            //we have a new block
        }

        private static void MarketBlockClearedOldersChanged(object sender, List<HashDigest<SHA256>> deletedHashes)
        {
            foreach (var item in deletedHashes)
            {
                FMONE.Current.SearchIndexer.DeleteMarketItemsByBlockHash(item.ToString());
            }
            
        }

        private static void ServerLoadedEvent(object sender, EventArgs e)
        {
            var state = FMONE.Current.GetServerState();

            //FreeMarketOneServer.Current.SearchIndexer.DeleteAll();
            //this is temporary and will be shortly removed. just to get search bootstrapped for testing.
            //var list = FreeMarketOneServer.Current.MarketManager.GetAllActiveOffers();
            //foreach (var item in list)
            //{
            //    FreeMarketOneServer.Current.SearchIndexer.Index(item, "unknown");
            //}

            //var testActionItem2 = new ReviewUserDataV1();
            //testActionItem2.ReviewDateTime = DateTime.UtcNow.AddMinutes(-1);
            //testActionItem2.Message = "This is a test message";
            //testActionItem2.Hash = testActionItem2.GenerateHash();

            //FreeMarketOneServer.Current.BasePoolManager.AcceptActionItem(testActionItem2);

            ////complete tx and send it to network
            //SpinWait.SpinUntil(() => FreeMarketOneServer.Current.BaseBlockChainManager.SwarmServer.Running);
            //FreeMarketOneServer.Current.BasePoolManager.PropagateAllActionItemLocal();

            ////now waiting for mining
            //SpinWait.SpinUntil((() => FreeMarketOneServer.Current.BasePoolManager.GetAllActionItemLocal().Count == 0));

            ////now wait until mining will start
            //SpinWait.SpinUntil(() => FreeMarketOneServer.Current.BasePoolManager.IsMiningWorkerRunning());

            ////now wait until we havent a new block
            //SpinWait.SpinUntil((() => _newBlock == true));

            ////wait until end of mining
            //SpinWait.SpinUntil(() => !FreeMarketOneServer.Current.BasePoolManager.IsMiningWorkerRunning());

            //var hashSets = FreeMarketOneServer.Current.BaseBlockChainManager.Storage.IterateBlockHashes().ToHashSet();

            //var chainId = FreeMarketOneServer.Current.BaseBlockChainManager.Storage.GetCanonicalChainId();
            //var block0HashId = FreeMarketOneServer.Current.BaseBlockChainManager.Storage.IndexBlockHash(chainId.Value, 0);
            //var block1HashId = FreeMarketOneServer.Current.BaseBlockChainManager.Storage.IndexBlockHash(chainId.Value, 1);

            //var blockO = FreeMarketOneServer.Current.BaseBlockChainManager.Storage.GetBlock<BaseAction>(block0HashId.Value);
            //var block1 = FreeMarketOneServer.Current.BaseBlockChainManager.Storage.GetBlock<BaseAction>(block1HashId.Value);
        }

        private static void OnExit(object sender, EventArgs e)
        {
            FMONE.Current.Stop();
        }
    }
}

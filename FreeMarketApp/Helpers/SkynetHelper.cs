using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Skynet;
using Microsoft.Extensions.FileProviders;
using MihaZupan;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Caching;

namespace FreeMarketApp.Helpers
{
    internal class SkynetHelper
    {
        private static readonly MemoryCache imageCache;
        private static readonly CacheItemPolicy itemPolicy;
        private static HttpClient _httpClient = null;

        static SkynetHelper()
        {
            imageCache = new MemoryCache("skynet.images");
            itemPolicy = new CacheItemPolicy();
            //one day
            itemPolicy.SlidingExpiration = new TimeSpan(1, 0, 0, 0);
        }

        internal string UploadToSkynet(string localPath, ILogger logger)
        {
            string skylinkUrl = null;

            try
            {
                PagesHelper.Log(logger, string.Format("Skynet Upload File: {0}", localPath));

                var applicationRoot = Path.GetDirectoryName(localPath);
                var fileName = Path.GetFileName(localPath);
                IFileProvider provider = new PhysicalFileProvider(applicationRoot);

                PagesHelper.Log(logger, string.Format("Skynet Gateway: {0}", SkynetWebPortal.SKYNET_GATEURL));

                HttpClient httpClient = GetHttpClient(false);
                httpClient.Timeout = TimeSpan.FromMinutes(5);

                var skynetWebPortal = new SkynetWebPortal(httpClient);
                var fileInfo = provider.GetFileInfo(fileName);

                var uniqueIndex = Guid.NewGuid();
                PagesHelper.Log(logger, string.Format("Procesing upload with GUID: {0}", uniqueIndex));

                var uploadInfo = skynetWebPortal.UploadFiles(uniqueIndex.ToString(), new UploadItem[] { new UploadItem(fileInfo) }).Result;

                skylinkUrl = string.Format("{0}{1}", SkynetWebPortal.SKYNET_PREFIX, uploadInfo.Skylink);

                PagesHelper.Log(logger, string.Format("Skynet Link: {0}", skylinkUrl));
            }
            catch (Exception e)
            {
                PagesHelper.Log(logger, string.Format("Skynet Link: {0} - {1}", e.Message, e.StackTrace), Serilog.Events.LogEventLevel.Error);
            }

            return skylinkUrl;
        }

        internal Stream DownloadFromSkynet(string skylink, ILogger logger)
        {
            PagesHelper.Log(logger, string.Format("Skynet Download File: {0}", skylink));

            if (!string.IsNullOrEmpty(skylink))
            {
                skylink = skylink.Replace(SkynetWebPortal.SKYNET_PREFIX, string.Empty);
                if (!imageCache.Contains(skylink))
                {
                    HttpClient httpClient = GetHttpClient();

                    var skynetWebPortal = new SkynetWebPortal(httpClient);

                    try
                    {
                        var content = skynetWebPortal.DownloadFile(skylink).Result;
                        var stream = content.ReadAsStreamAsync().Result;
                        MemoryStream readOnlyStream = new MemoryStream();
                        stream.CopyTo(readOnlyStream);

                        stream.Position = 0;
                        readOnlyStream.Position = 0;

                        imageCache.Add(skylink, readOnlyStream, itemPolicy);
                        return stream;
                    }
                    catch (Exception e)
                    {
                        PagesHelper.Log(logger, string.Format("Error during skynet Download File: {0} {1}", skylink, e.Message), Serilog.Events.LogEventLevel.Error);
                    }

                    return null;
                }
                else
                {
                    MemoryStream clonedStream = new MemoryStream();
                    var cachedStream = imageCache.Get(skylink) as Stream;
                    if (cachedStream != null)
                    {
                        cachedStream.Position = 0;
                        cachedStream.CopyTo(clonedStream);
                        cachedStream.Position = 0;
                    }
                    clonedStream.Position = 0;
                    return clonedStream;
                }
            }
            else
            {
                return null;
            }           
        }

        private static HttpClient GetHttpClient(bool reuseHttpClient = true)
        {
            if (_httpClient == null || !reuseHttpClient)
            {
                var proxy = new HttpToSocks5Proxy("127.0.0.1", 9050);
                var handler = new HttpClientHandler { Proxy = proxy };
                HttpClient httpClient = new HttpClient(handler, true);
                httpClient.BaseAddress = new Uri(SkynetWebPortal.SKYNET_GATEURL);
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
                {
                    Public = true
                };
                _httpClient = httpClient;
            }
         
            return _httpClient;
        }

        internal void PreloadTitlePhotos(List<MarketItemV1> offers, ILogger logger)
        {
            if (offers.Any())
            {
                for (int i = 0; i < offers.Count; i++)
                {
                    if (offers[i].Photos.Any())
                    {
                        if (!string.IsNullOrEmpty(offers[i].Photos[0]) && (offers[i].Photos[0].Contains(SkynetWebPortal.SKYNET_PREFIX)))
                        {
                            PagesHelper.Log(logger, string.Format("Skynet Preloading Title File: {0}", offers[i].Photos[0]));

                            var skynetStream = DownloadFromSkynet(offers[i].Photos[0], logger);
                            if (skynetStream !=null) {
                                offers[i].PreTitlePhoto = new Bitmap(skynetStream);
                            }
                            else
                            {
                                var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                                var missingImage = assets.Open(new Uri("avares://FreeMarketApp/Assets/imagemissing.png"));
                                offers[i].PreTitlePhoto = new Bitmap(missingImage);
                            }
                        }
                    }
                }
            }
        }

        internal void PreloadPhotos(MarketItemV1 offer, ILogger logger)
        {
            if (offer != null)
            {
                PreloadPhotos(new List<MarketItemV1>() { offer }, logger);
            }    
        }

        internal void PreloadPhotos(List<MarketItemV1> offers, ILogger logger)
        {
            if (offers.Any())
            {
                for (int i = 0; i < offers.Count; i++)
                {
                    if (offers[i].Photos.Any())
                    {
                        offers[i].PrePhotos = new List<Bitmap>();

                        for (int n = 0; n < offers[i].Photos.Count; n++)
                        {
                            if (!string.IsNullOrEmpty(offers[i].Photos[n]) && (offers[i].Photos[n].Contains(SkynetWebPortal.SKYNET_PREFIX)))
                            {
                                PagesHelper.Log(logger, string.Format("Skynet Preloading Title File: {0}", offers[i].Photos[n]));

                                var skynetStream = DownloadFromSkynet(offers[i].Photos[n], logger);
                                if (skynetStream != null)
                                {
                                    offers[i].PrePhotos.Add(new Bitmap(skynetStream));
                                } 
                                else
                                {
                                    var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                                    var missingImage = assets.Open(new Uri("avares://FreeMarketApp/Assets/imagemissingbig.png"));
                                    offers[i].PrePhotos.Add(new Bitmap(missingImage));
                                }                               
                            }
                        }
                    }
                }
            }
        }
    }
}

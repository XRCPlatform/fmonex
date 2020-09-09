using Avalonia.Media.Imaging;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Skynet;
using Microsoft.Extensions.FileProviders;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace FreeMarketApp.Helpers
{
    internal static class SkynetHelper
    {
        internal static string UploadToSkynet(string localPath, ILogger logger)
        {
            string skylinkUrl = null;

            try
            {
                PagesHelper.Log(logger, string.Format("Skynet Upload File: {0}", localPath));

                var applicationRoot = Path.GetDirectoryName(localPath);
                var fileName = Path.GetFileName(localPath);
                IFileProvider provider = new PhysicalFileProvider(applicationRoot);

                PagesHelper.Log(logger, string.Format("Skynet Gateway: {0}", SkynetWebPortal.SKYNET_GATEURL));

                var httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(SkynetWebPortal.SKYNET_GATEURL)
                };

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

        internal static Stream DownloadFromSkynet(string skylink, ILogger logger)
        {
            PagesHelper.Log(logger, string.Format("Skynet Download File: {0}", skylink));

            if (!string.IsNullOrEmpty(skylink))
            {
                skylink = skylink.Replace(SkynetWebPortal.SKYNET_PREFIX, string.Empty);

                var httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(SkynetWebPortal.SKYNET_GATEURL)
                };

                var skynetWebPortal = new SkynetWebPortal(httpClient);

                var content = skynetWebPortal.DownloadFile(skylink).Result;

                return content.ReadAsStreamAsync().Result;
            }
            else
            {
                return null;
            }
        }

        internal static void PreloadTitlePhotos(List<MarketItemV1> offers, ILogger logger)
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

                            var skynetStream = SkynetHelper.DownloadFromSkynet(offers[i].Photos[0], logger);
                            offers[i].PreTitlePhoto = new Bitmap(skynetStream);
                        }
                    }
                }
            }
        }

        internal static void PreloadPhotos(List<MarketItemV1> offers, ILogger logger)
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

                                var skynetStream = SkynetHelper.DownloadFromSkynet(offers[i].Photos[n], logger);
                                offers[i].PrePhotos.Add(new Bitmap(skynetStream));
                            }
                        }
                    }
                }
            }
        }
    }
}

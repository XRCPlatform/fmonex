using FreeMarketOne.BlockChain;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Pools;
using FreeMarketOne.Users;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FreeMarketOne.Search
{
    public static class SearchHelper 
    {
        public static string GetDataFolder(IBaseConfiguration baseConfiguration)
        {
            return Path.Combine(baseConfiguration.FullBaseDirectory, baseConfiguration.SearchEnginePath);
        }


        public static List<string> GenerateSellerPubKeyHashes(List<byte[]> pubKeys)
        {
            List<string> list = new List<string>();
            if (pubKeys == null)
            {
                return list;
            }
            foreach (var pubKey in pubKeys)
            {
                list.Add(Hash(pubKey));
            }
            return list;
        }


        /// <summary>
        /// Choosing lower grade hash for speed and disk space efficiency as colisions are irrelevant in this context
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static string Hash(byte[] rawData)
        {
            using (SHA1 shaHash = SHA1.Create())
            {
                byte[] bytes = shaHash.ComputeHash(rawData);
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static SellerAggregate CalculateSellerXRCTotal(MarketItemV1 marketItem, IBaseConfiguration baseConfiguration, List<byte[]> sellerPubKeys, IXRCHelper xrcDataProvider, IUserManager userManager, BasePoolManager basePoolManager, IBlockChainManager<BaseAction> blockChainManager)
        {
            SellerAggregate seller = null;
            byte[] publicKey = null;
            string pubKeyHash = null;
            UserDataV1 user = null;
            List<ReviewUserDataV1> reviews = null;

            List<string> sellerPubKeyHashes = GenerateSellerPubKeyHashes(sellerPubKeys);

            var sellerDataFolder = GetSellerDataFolder(baseConfiguration);

            //seek for cache in local fs
            foreach (var key in sellerPubKeys)
            {
                var tempPubKeyHash = Hash(key);
                if (SeekLocalSellerInfoBySellerHash(sellerDataFolder, tempPubKeyHash))
                {
                    seller = ReadSellerInfoBySellerHash(sellerDataFolder, tempPubKeyHash);
                    if (seller != null)
                    {
                        publicKey = key;
                        pubKeyHash = Hash(key);
                        break;
                    }
                }               
            }
            
            //only run if cache miss
            if (pubKeyHash == null && seller == null)
            {
                foreach (var key in sellerPubKeys)
                {
                    List<byte[]> singleKey = new List<byte[]>();
                    singleKey.Add(key);
                    user = userManager.GetUserDataByPublicKey(singleKey, basePoolManager, blockChainManager);
                    reviews = userManager.GetAllReviewsForPubKey(singleKey, basePoolManager, blockChainManager);
                    if (user != null)
                    {
                        publicKey = key;
                        pubKeyHash = Hash(key);
                        break;
                    }
                }
            }


            if (pubKeyHash != null && user != null && user?.Signature != null)
            {
                //TODO: consider caching for perf optimizations
                if (seller == null)
                {
                    seller = new SellerAggregate()
                    {
                        PublicKeyHashes = sellerPubKeyHashes,
                        PublicKeys = sellerPubKeys,
                        SellerName = user.UserName,
                        StarRating = userManager.GetUserReviewStars(reviews)
                    };
                }

                if (!String.IsNullOrEmpty(marketItem.XRCTransactionHash) 
                    && !String.IsNullOrEmpty(marketItem.XRCReceivingAddress) 
                    && !seller.XRCTransactions.ContainsKey(marketItem.XRCTransactionHash))
                {
                    var summary = xrcDataProvider.GetTransaction(baseConfiguration, marketItem.XRCReceivingAddress, marketItem.XRCTransactionHash);
                    //can't handle confirmations now. will need some block notify or long poll later or some zMq broadcast
                    //when xrc node is embeded block notify could fix this
                    //if (summary.Confirmations > 5)
                    //summary.Date.CompareTo(marketItem.SaleDate) saleDate is missing so for this round won't do validation on dates
                    //TODO: tighten this later XRC transaction should be within 24 hrs of checkout time
                    if (summary != null && summary.Total > 0)
                    {
                        seller.XRCTransactions.Add(marketItem.XRCTransactionHash, summary.Total);
                        double total = 0;
                        foreach (var item in seller.XRCTransactions.Values)
                        {
                            total += item;
                        }
                        seller.TotalXRCVolume = total;
                    }

                }

                SaveSellerInfo(seller, sellerDataFolder, pubKeyHash);
            }
            //for sellers that could not be found. // should really never happen
            if(seller == null)
            {
                seller = new SellerAggregate()
                {
                    PublicKeyHashes = sellerPubKeyHashes,
                    PublicKeys = sellerPubKeys
                };
            }
            return seller;
        }

        public static string GetSellerDataFolder(IBaseConfiguration baseConfiguration)
        {
            return Path.Combine(baseConfiguration.FullBaseDirectory, baseConfiguration.SearchEnginePath,"sellers");
        }


        public static void SaveSellerInfo(SellerAggregate seller, string filePath, string sellerPublicKeyHash)
        {
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            string sellerFilePath = Path.Combine(filePath, $"{sellerPublicKeyHash}.json");
            using (StreamWriter file = File.CreateText(sellerFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, seller);
            }
        }

        public static SellerAggregate ReadSellerInfoBySellerHash(string filePath, string sellerPublicKeyHash)
        {
            string sellerFilePath = Path.Combine(filePath, $"{sellerPublicKeyHash}.json");
            return ReadSellerInfo(sellerFilePath);
        }

        public static bool SeekLocalSellerInfoBySellerHash(string filePath, string sellerPublicKeyHash)
        {
            string sellerFilePath = Path.Combine(filePath, $"{sellerPublicKeyHash}.json");
            return File.Exists(sellerFilePath);
        }

        public static SellerAggregate ReadSellerInfo(string filePath)
        {
            SellerAggregate seller = null;
            if (File.Exists(filePath))
            {
                using (StreamReader file = File.OpenText(filePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    //nongeneric version use textreader as opposed to string 
                    seller = serializer.Deserialize(file, typeof(SellerAggregate)) as SellerAggregate;
                }
            }

            return seller;
        }

    }
}

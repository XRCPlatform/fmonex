﻿using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using Libplanet.Blocks;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using FreeMarketOne.Markets;
using System.IO;
using FreeMarketOne.Users;
using FreeMarketOne.Pools;
using FreeMarketOne.BlockChain;

namespace FreeMarketOne.Search
{
    public class SearchIndexer : IDisposable
    {
        private readonly IndexWriter _writer;
        private readonly DirectoryTaxonomyWriter _taxoWriter;
        private readonly IndexWriterConfig _indexConfig;
        private readonly FacetsConfig facetConfig = new FacetsConfig();
        private readonly IMarketManager _marketManager;
        private readonly IXRCHelper _xrcCalculator;
        private readonly string _indexLocation;
        private readonly IBaseConfiguration _configuration;
        private readonly IUserManager _userManager;
        private readonly BasePoolManager _basePoolManager;
        private readonly IBlockChainManager<BaseAction> _baseBlockChain;

        public SearchIndexer(IMarketManager marketManager, IBaseConfiguration baseConfiguration, IXRCHelper XRCHelper, IUserManager userManager, BasePoolManager basePoolManager, IBlockChainManager<BaseAction> baseBlockChain)
        {
            var AppLuceneVersion = LuceneVersion.LUCENE_48;
            string indexLocation = SearchHelper.GetDataFolder(baseConfiguration);

            var dir = FSDirectory.Open(indexLocation);
            
            var dirTaxonomy = FSDirectory.Open(System.IO.Path.Combine(indexLocation, "taxonomy").ToString());
            SearchIndexPath = indexLocation;
            //create an analyzer to process the text
            var analyzer = new StandardAnalyzer(AppLuceneVersion);

            //create an index writer
            _indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
            _writer = new IndexWriter(dir, _indexConfig);
            _taxoWriter = new DirectoryTaxonomyWriter(dirTaxonomy);
            _marketManager = marketManager;
            _indexLocation = indexLocation;
            _xrcCalculator = XRCHelper;
            _configuration = baseConfiguration;
            _userManager = userManager;
            _basePoolManager = basePoolManager;
            _baseBlockChain = baseBlockChain;
        }
        
        public bool Initialize()
        {
            Document doc = new Document()
            {
                new StringField("PrimeID", "prime-id", Field.Store.YES)
            };

            Writer.AddDocument(facetConfig.Build(_taxoWriter, doc));
            Writer.Flush(triggerMerge: true, applyAllDeletes: true);
            Writer.Commit();
            _taxoWriter.Commit();

            Writer.DeleteDocuments(new Term("PrimeID", "prime-id"));
            Writer.Flush(triggerMerge: true, applyAllDeletes: true);
            Writer.Commit();
            _taxoWriter.Commit();

            return true;
        }

        private IndexWriter Writer
        {
            get
            {
                return _writer;
            }
        }

        public string SearchIndexPath { get; }

        /// <summary>
        /// Indexes market item for search, includes block hash so that old blocks can be wiped after new genesis
        /// </summary>
        /// <param name="marketItem"></param>
        /// <param name="blockHash"></param>
        public void Index(MarketItemV1 marketItem, string blockHash, bool updateAllSellerDocuments = true, SellerAggregate currentSellerAggregate = null)
        {
            double pricePerGram = 0F;
            MarketItemCategory cat = (MarketItemCategory)marketItem.Category;
            //new DealType().TryGetValue(marketItem.DealType, out string dealTypeString);
            
            //if we are re-running the block and same hash comes again, delete and add it again
            Writer.DeleteDocuments(new Term("ID", marketItem.Signature));
            //if market item has a baseSignature then it's previous version's signature
            //to remove previous versions of document from search we delete by signature
            if (!string.IsNullOrEmpty(marketItem.BaseSignature)) {
                Writer.DeleteDocuments(new Term("ID", marketItem.BaseSignature));
            }

            if (marketItem.WeightInGrams > 0 && marketItem.Price>0) {
                pricePerGram = marketItem.Price / marketItem.WeightInGrams;
            }

            if (marketItem.State == (int)ProductStateEnum.Removed || marketItem.State == (int)ProductStateEnum.Sold)
            {
                Writer.DeleteDocuments(new Term("ID", marketItem.Signature));
                Writer.Flush(triggerMerge: true, applyAllDeletes: true);
                return;
            }
            
            SellerAggregate sellerAggregate = null;

            if (currentSellerAggregate == null)
            {
                var sellerPubKeys = _marketManager.GetSellerPubKeyFromMarketItem(marketItem);

                sellerAggregate = SearchHelper.CalculateSellerXRCTotal(marketItem, _configuration, sellerPubKeys, _xrcCalculator, _userManager, _basePoolManager, _baseBlockChain);
                if (sellerAggregate == null)
                {
                    sellerAggregate = new SellerAggregate()
                    {
                        TotalXRCVolume = 0
                    };
                }
            }
            else
            {
                sellerAggregate = currentSellerAggregate;
            }

            Document doc = new Document
            {
                new StringField("ID", marketItem.Signature, Field.Store.YES),
                new StringField("BlockHash", blockHash, Field.Store.YES),
                new TextField("Title",marketItem.Title,Field.Store.NO),
                new TextField("Manufacturer",string.IsNullOrEmpty(marketItem.Manufacturer) ? "Unspecified":marketItem.Manufacturer,Field.Store.NO),
                new TextField("Category",cat.ToString(),Field.Store.NO),
                new TextField("Description",marketItem.Description,Field.Store.NO),
                new FacetField("Category",cat.ToString()),
                new FacetField("Shipping",marketItem.Shipping),
                new FacetField("Fineness",string.IsNullOrEmpty(marketItem.Fineness) ? "Unspecified":marketItem.Fineness),
                new FacetField("Manufacturer",string.IsNullOrEmpty(marketItem.Manufacturer) ? "Unspecified":marketItem.Manufacturer),
                new FacetField("Size",string.IsNullOrEmpty(marketItem.Size) ? "Unspecified":marketItem.Size),
                //new FacetField("Sold",string.IsNullOrEmpty(marketItem.BuyerSignature) ? "No": "Yes"),
                new NumericDocValuesField("WeightInGrams",marketItem.WeightInGrams),                
                new DoubleDocValuesField("PricePerGram", pricePerGram),
                new DoubleDocValuesField("Price", marketItem.Price),
                new StoredField("MarketItem", JsonConvert.SerializeObject(marketItem)),
                new StoredField("XrcTotal", (long)sellerAggregate?.TotalXRCVolume)
            };

            //append all seller sha-hashes to a single multivalue field so that we can find by any.
            //this should support find all seller items usecase, provided we hash seller's keys for query
            foreach (var sellerPubKeyHash in sellerAggregate?.PublicKeyHashes)
            {
                doc.Add(new StringField("SellerPubKeyHash", sellerPubKeyHash, Field.Store.NO));
            }

            Writer.AddDocument(facetConfig.Build(_taxoWriter, doc));
            Writer.Flush(triggerMerge: true, applyAllDeletes: true);
            Writer.Commit();
            _taxoWriter.Commit();

            if (updateAllSellerDocuments)
            {
                updateAllSellerDocumentsWithLatest(sellerAggregate, marketItem.Signature, blockHash);
            }
            
        }

        private void updateAllSellerDocumentsWithLatest(SellerAggregate sellerAggregate, string skipSignature, string blockHash)
        {
            //search all market items by seller pubkey hash
            SearchEngine engine = new SearchEngine(_marketManager, _indexLocation, 50);
            var query = engine.BuildQueryBySellerPubKeys(sellerAggregate.PublicKeys);
            var searchResult = engine.Search(query, false);

            int pages = searchResult.TotalHits / searchResult.PageSize;
            
            //increment to 1
            pages++;

            for (int i = 0; i < pages; i++)
            {
                //skip first (re-query) as it's built above
                //on second page and up use the interal search result. the top one was needed to get number of pages
                if (i > 1)
                {
                    searchResult = engine.Search(query, false, i+1);
                }                

                for (int y = 0; y < searchResult.Results.Count; y++)
                {
                    //items that have allready been indexed shall be skipped for efficiency
                    if (!searchResult.Results[y].Signature.Equals(skipSignature) 
                        && !searchResult.Documents[y].GetField("BlockHash").GetStringValue().Equals(blockHash))
                    {
                        Index(searchResult.Results[y], searchResult.Documents[y].GetField("BlockHash").GetStringValue(), false, sellerAggregate);
                    }
                }
            }           
        }

        /// <summary>
        /// Iterates over transactions in block and idexes marketItems.
        /// </summary>
        /// <param name="block"></param>
        public void IndexBlock(Block<MarketAction> block)
        {
            Type[] types = new Type[] { typeof(MarketItemV1) };
            foreach (var itemTx in block.Transactions)
            {
                foreach (var itemAction in itemTx.Actions)
                {
                    foreach (var item in itemAction.BaseItems)
                    {
                        if (types.Contains(item.GetType()))
                        {
                            Index((MarketItemV1)item, block.Hash.ToString());
                        }
                    }
                }
            }
        }

        public void DeleteMarketItem(MarketItemV1 marketItem)
        {
            Writer.DeleteDocuments(new Term("ID", marketItem.Signature));
            //if market item has a baseSignature then it's previous version's signature
            //to remove previous versions of document from search we delete by signature
            if (!string.IsNullOrEmpty(marketItem.BaseSignature))
            {
                Writer.DeleteDocuments(new Term("ID", marketItem.BaseSignature));
            }
            Writer.Flush(triggerMerge: true, applyAllDeletes: true);
            Writer.Commit();
            _taxoWriter.Commit();
        }

        public void DeleteMarketItemsByBlockHash(string blockHash)
        {
            DeleteMarketItemByFieldNameValue("BlockHash", blockHash);
        }

        private void DeleteMarketItemByFieldNameValue(string fieldName ,string fieldValue)
        {
            Writer.DeleteDocuments(new Term(fieldName, fieldValue));
            Writer.Flush(triggerMerge: true, applyAllDeletes: true);
            Writer.Commit();
            _taxoWriter.Commit();
        }

        public void DeleteAll()
        {
            Writer.DeleteAll();
            Writer.Commit();
            _taxoWriter.Commit();
        }

        public void Commit()
        {
            Writer.Commit();
            _taxoWriter.Commit();
        }

        public void Dispose()
        {
            Writer.Commit();
            Writer.Dispose();
            _taxoWriter.Dispose();
        }

    }
}

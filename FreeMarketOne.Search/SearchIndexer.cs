using FreeMarketOne.DataStructure;
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
using System.Linq;
using Newtonsoft.Json;
using FreeMarketOne.Markets;
using FreeMarketOne.Users;
using FreeMarketOne.Pools;
using FreeMarketOne.BlockChain;
using static FreeMarketOne.Markets.MarketManager;
using System.Threading.Tasks;
using System.Threading;
using Serilog;
using static FreeMarketOne.Extensions.Common.ServiceHelper;

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
        private BasePoolManager _basePoolManager;
        private IBlockChainManager<BaseAction> _baseBlockChain;
        private NormalizedStore _normalizedStore;
        private SearchEngine _engine;

        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped
        /// </summary>
        private CommonStates _running;
        public bool IsRunning => _running == CommonStates.Running;
        private ILogger _logger { get; set; }

        private static object lockobject = new object();

        public SearchIndexer(IMarketManager marketManager, IBaseConfiguration baseConfiguration, IXRCHelper XRCHelper, IUserManager userManager)
        {
            _logger = Log.Logger.ForContext<MarketManager>();
            _logger.Information("Initializing Search Indexer");

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
            _engine = new SearchEngine(_marketManager, _indexLocation, 10);
            _normalizedStore = new NormalizedStore(indexLocation);

            InitStorage();

            _running = CommonStates.Running;

            _logger.Information("Initialized Search Indexer Storage");
        }

        private bool InitStorage()
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

            Thread.Sleep(250);

            return true;
        }

        public bool IsSearchIndexerRunning()
        {
            if (_running == CommonStates.Running)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Initialize(BasePoolManager basePoolManager, IBlockChainManager<BaseAction> baseBlockChain)
        {
            _logger.Information("Initialized Search Indexer base pool and block manager.");

            _basePoolManager = basePoolManager;
            _baseBlockChain = baseBlockChain;
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
        public void Index(MarketItemV1 marketItem, string blockHash, bool updateAllSellerDocuments = true, SellerAggregate currentSellerAggregate = null, bool isMyOffer = false, OfferDirection offerDirection = OfferDirection.Undetermined)
        {

            // this can only be running single threaded as transaction totals and sequencing matters.
            lock (lockobject)
            {
                double pricePerGram = 0F;
                MarketCategoryEnum cat = (MarketCategoryEnum)marketItem.Category;
                //new DealType().TryGetValue(marketItem.DealType, out string dealTypeString);

                //if we are re-running the block and same hash comes again, delete and add it again
                Writer.DeleteDocuments(new Term("ID", marketItem.Signature));
                //if market item has a baseSignature then it's previous version's signature
                //to remove previous versions of document from search we delete by signature
                if (!string.IsNullOrEmpty(marketItem.BaseSignature))
                {
                    Writer.DeleteDocuments(new Term("ID", marketItem.BaseSignature));
                }

                if (marketItem.WeightInGrams > 0 && marketItem.Price > 0)
                {
                    pricePerGram = marketItem.Price / marketItem.WeightInGrams;
                }

                SellerAggregate sellerAggregate = null;

                if (currentSellerAggregate == null)
                {
                    var sellerPubKeys = _marketManager.GetSellerPubKeyFromMarketItem(marketItem);
                    //if market offer has corrupted keys exlude from listings as it 
                    //will not be possible for seller to support that transaction
                    if (!sellerPubKeys.Any())
                    {
                        return;
                    }

                    sellerAggregate = SearchHelper.CalculateSellerXRCTotal(marketItem, _configuration, sellerPubKeys, _xrcCalculator, _userManager, _basePoolManager, _baseBlockChain, _engine);
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
                    new StringField("ItemHash", marketItem.Hash, Field.Store.NO),
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
                    new StoredField("XrcTotal", (long)sellerAggregate?.TotalXRCVolume),
                    new TextField("SellerName", sellerAggregate?.SellerName, Field.Store.NO),
                    new StoredField("SellerStarsRating", (double)sellerAggregate?.StarRating)
                    //new NumericDocValuesField("XrcTotal", (long)sellerAggregate?.TotalXRCVolume)
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

                //remove now after all the toltals calculations
                if (marketItem.State == (int)ProductStateEnum.Removed || marketItem.State == (int)ProductStateEnum.Sold)
                {
                    DeleteMarketItem(marketItem);
                }

                if (marketItem.State == (int)ProductStateEnum.Sold && updateAllSellerDocuments)
                {
                    var direction = offerDirection;
                    if (offerDirection == OfferDirection.Undetermined)
                    {
                        var currentUserPubKey = _userManager.GetCurrentUserPublicKey();
                        if (sellerAggregate.PublicKeys.Exists(x => x.SequenceEqual(currentUserPubKey)))
                        {
                            isMyOffer = true;
                            direction = OfferDirection.Sold;
                        }
                        else
                        {
                            var buyerPubKeys = _marketManager.GetBuyerPubKeyFromMarketItem(marketItem);
                            if (buyerPubKeys.Exists(x => x.SequenceEqual(currentUserPubKey)))
                            {
                                isMyOffer = true;
                                direction = OfferDirection.Bought;
                            }
                        }
                    }

                    //only save here sold / bought items
                    if (isMyOffer)
                    {
                        _normalizedStore.Save(marketItem, direction);
                    }
                    //UpdateAllSellerDocumentsWithLatestFast(sellerAggregate);
                    //Task.Run(() => UpdateAllSellerDocumentsWithLatest(sellerAggregate, marketItem.Signature, blockHash));
                    UpdateAllSellerDocumentsWithLatest(sellerAggregate, marketItem.Signature, blockHash);
                }
            }

        }

        private void UpdateAllSellerDocumentsWithLatest(SellerAggregate sellerAggregate)
        {
            foreach (var sellerPubKeyHash in sellerAggregate.PublicKeyHashes)
            {
                Writer.UpdateNumericDocValue(new Term("SellerPubKeyHash", sellerPubKeyHash), "XrcTotal", (long?)sellerAggregate?.TotalXRCVolume);
                //add star rating update 
            }
            Writer.Flush(triggerMerge: true, applyAllDeletes: true);
            Writer.Commit();
        }

        private void UpdateAllSellerDocumentsWithLatestFast(SellerAggregate sellerAggregate)
        {
            foreach (var sellerPubKeyHash in sellerAggregate.PublicKeyHashes)
            {
                Writer.UpdateBinaryDocValue(new Term("SellerPubKeyHash", sellerPubKeyHash), "XrcTotal", ToBytes((long)sellerAggregate?.TotalXRCVolume));
                //add star rating update 
            }
            Writer.Flush(triggerMerge: true, applyAllDeletes: true);
            Writer.Commit();
        }

        // encodes a long into a BytesRef as VLong so that we get varying number of bytes when we update
        internal static BytesRef ToBytes(long value)
        {
            //    long orig = value;
            BytesRef bytes = new BytesRef(10); // negative longs may take 10 bytes
            while ((value & ~0x7FL) != 0L)
            {
                bytes.Bytes[bytes.Length++] = unchecked((byte)((value & 0x7FL) | 0x80L));
                value = (long)((ulong)value >> 7);
            }
            bytes.Bytes[bytes.Length++] = (byte)value;
            //    System.err.println("[" + Thread.currentThread().getName() + "] value=" + orig + ", bytes=" + bytes);
            return bytes;
        }

        private void UpdateAllSellerDocumentsWithLatest(SellerAggregate sellerAggregate, string skipSignature, string blockHash)
        {
            try
            {
                //search all market items by seller pubkey hash

                var query = _engine.BuildQueryBySellerPubKeys(sellerAggregate.PublicKeys);
                var searchResult = _engine.Search(query, false);

                int pages = searchResult.TotalHits / searchResult.PageSize;

                //increment to 1
                pages++;

                for (int i = 0; i < pages; i++)
                {
                    //skip first (re-query) as it's built above
                    //on second page and up use the interal search result. the top one was needed to get number of pages
                    if (i > 1)
                    {
                        searchResult = _engine.Search(query, false, i + 1);
                    }

                    for (int y = 0; y < searchResult.Results.Count; y++)
                    {
                        //items that have allready been indexed shall be skipped for efficiency
                        if (!searchResult.Results[y].Signature.Equals(skipSignature)
                            && (searchResult.Documents.Count == searchResult.Results.Count 
                            && !searchResult.Documents[y].GetField("BlockHash").GetStringValue().Equals(blockHash))
                           )
                        {
                            Index(searchResult.Results[y], searchResult.Documents[y].GetField("BlockHash").GetStringValue(), false, sellerAggregate, true, OfferDirection.Sold);
                        }
                    }
                }
            }
            catch (Exception)
            {
                //swallow 
            }
        }

        /// <summary>
        /// Iterates over transactions in block and idexes marketItems.
        /// </summary>
        /// <param name="block"></param>
        public void IndexBlock(Block<MarketAction> block)
        {
            IterateBlocks(block, (IBaseItem item) => Index((MarketItemV1)item, block.Hash.ToString()));
        }

        /// <summary>
        /// Iterates over transactions in block and UNidexes marketItems.
        /// </summary>
        /// <param name="block"></param>
        public void UnIndexBlock(Block<MarketAction> block)
        {
            IterateBlocks(block, (IBaseItem marketItem) => {
                DeleteMarketItem((MarketItemV1)marketItem);
                _normalizedStore.Delete((MarketItemV1)marketItem);
            });
        }

        private void IterateBlocks(Block<MarketAction> block, Action<IBaseItem> doAction)
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
                            doAction(item);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Iterates over transactions in block and idexes baseItems.
        /// </summary>
        /// <param name="block"></param>
        public void IndexBlock(Block<BaseAction> block)
        {
            IterateBlocks(block, (IBaseItem item) =>
            {
                if (item.GetType() == typeof(UserDataV1))
                {
                    Index((UserDataV1)item, block.Hash.ToString());
                }
                if (item.GetType() == typeof(ReviewUserDataV1))
                {
                    Index((ReviewUserDataV1)item, block.Hash.ToString());
                }
            });
        }

        /// <summary>
        /// Iterates over transactions in block and UNidexes baseItems.
        /// </summary>
        /// <param name="block"></param>
        public void UnIndexBlock(Block<BaseAction> block)
        {
            IterateBlocks(block, (IBaseItem item) =>
            {
                if (item.GetType() == typeof(UserDataV1))
                {
                    _normalizedStore.Delete((UserDataV1)item);
                }
                if (item.GetType() == typeof(ReviewUserDataV1))
                {
                    _normalizedStore.Delete((ReviewUserDataV1)item);
                }
            });
        }

        private void IterateBlocks(Block<BaseAction> block, Action<IBaseItem> doAction)
        {
            Type[] types = new Type[] { typeof(UserDataV1), typeof(ReviewUserDataV1) };
            foreach (var itemTx in block.Transactions)
            {
                foreach (var itemAction in itemTx.Actions)
                {
                    foreach (var item in itemAction.BaseItems)
                    {
                        if (types.Contains(item.GetType()))
                        {
                            doAction(item);
                        }
                    }
                }
            }
        }

        private void Index(UserDataV1 item, string blockHash)
        {
            _normalizedStore.Save(item, blockHash);
        }

        private void Index(ReviewUserDataV1 item, string blockHash)
        {
            _normalizedStore.Save(item, blockHash);
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

        private void DeleteMarketItemByFieldNameValue(string fieldName, string fieldValue)
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
            _running = CommonStates.Stopping;

            Writer.Commit();
            Writer.Dispose();
            _taxoWriter.Dispose();

            _running = CommonStates.Stopped;
        }

    }
}

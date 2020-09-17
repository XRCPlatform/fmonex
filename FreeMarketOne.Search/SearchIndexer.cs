using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore;
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

namespace FreeMarketOne.Search
{
    public class SearchIndexer : IDisposable
    {
        private readonly IndexWriter writer;
        private readonly DirectoryTaxonomyWriter taxoWriter;
        private readonly IndexWriterConfig indexConfig;
        private readonly FacetsConfig facetConfig = new FacetsConfig();
        private IMarketManager _marketManager;

        public SearchIndexer(string indexLocation, IMarketManager marketManager)
        {
            var AppLuceneVersion = LuceneVersion.LUCENE_48;

            var dir = FSDirectory.Open(indexLocation);
            
            var dirTaxonomy = FSDirectory.Open(System.IO.Path.Combine(indexLocation, "taxonomy").ToString());
            SearchIndexPath = indexLocation;
            //create an analyzer to process the text
            var analyzer = new StandardAnalyzer(AppLuceneVersion);

            //create an index writer
            indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
            writer = new IndexWriter(dir, indexConfig);
            taxoWriter = new DirectoryTaxonomyWriter(dirTaxonomy);
            _marketManager = marketManager;
        }

        private IndexWriter Writer
        {
            get
            {
                return writer;
            }
        }

        public string SearchIndexPath { get; }

        /// <summary>
        /// Indexes market item for search, includes block hash so that old blocks can be wiped after new genesis
        /// </summary>
        /// <param name="marketItem"></param>
        /// <param name="blockHash"></param>
        public void Index(MarketItem marketItem, string blockHash)
        {
            double pricePerGram = 0F;
            MarketItemCategory cat = (MarketItemCategory)marketItem.Category;
            new DealType().TryGetValue(marketItem.DealType, out string dealTypeString);
            
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

            if (marketItem.State == (int)ProductStateEnum.Removed)
            {
                Writer.DeleteDocuments(new Term("ID", marketItem.Signature));
                Writer.Flush(triggerMerge: true, applyAllDeletes: true);
                return;
            }

            //transform seller pubkeys to sha hashes for simplified search use
            var sellerPubKeys = _marketManager.GetSellerPubKeyFromMarketItem(marketItem);
            List<string> sellerPubKeyHashes = GenerateSellerPubKeyHashes(sellerPubKeys);

            Document doc = new Document
            {
                new StringField("ID", marketItem.Signature, Field.Store.YES),
                new StringField("BlockHash", blockHash, Field.Store.NO),
                new TextField("Title",marketItem.Title,Field.Store.NO),
                new TextField("Description",marketItem.Description,Field.Store.NO),
                new FacetField("Category",cat.ToString()),
                new FacetField("Shipping",marketItem.Shipping),
                new FacetField("DealType",dealTypeString),
                new FacetField("Fineness",string.IsNullOrEmpty(marketItem.Fineness) ? "Unspecified":marketItem.Fineness),
                new FacetField("Manufacturer",string.IsNullOrEmpty(marketItem.Manufacturer) ? "Unspecified":marketItem.Manufacturer),
                new FacetField("Size",string.IsNullOrEmpty(marketItem.Size) ? "Unspecified":marketItem.Size),
                new FacetField("Sold",string.IsNullOrEmpty(marketItem.BuyerSignature) ? "No": "Yes"),
                new NumericDocValuesField("WeightInGrams",marketItem.WeightInGrams),                
                new DoubleDocValuesField("PricePerGram", pricePerGram),
                new DoubleDocValuesField("Price", marketItem.Price),
                new StoredField("MarketItem", JsonConvert.SerializeObject(marketItem))
            };

            //append all seller sha-hashes to a single multivalue field so that we can find by any.
            //this should support find all seller items usecase, provided we hash seller's keys for query
            foreach (var sellerPubKeyHash in sellerPubKeyHashes)
            {
                doc.Add(new StringField("SellerPubKeyHash", sellerPubKeyHash, Field.Store.NO));
            }

            Writer.AddDocument(facetConfig.Build(taxoWriter, doc));
            Writer.Flush(triggerMerge: true, applyAllDeletes: true);
            Writer.Commit();
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

        private List<string> GenerateSellerPubKeyHashes(List<byte[]> pubKeys)
        {
            List<string> list = new List<string>();
            if (pubKeys == null)
            {
                return list;
            }
            foreach (var pubKey in pubKeys)
            {
                list.Add(Sha256Hash(pubKey));
            }
            return list;
        }

       //TODO: consider migrating to lower grade hash for speed and disk space efficiency as colisions are irrelevant in this context
        private static string Sha256Hash(byte[] rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(rawData);
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public void DeleteMarketItem(MarketItem marketItem)
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
        }

        public void DeleteAll()
        {
            Writer.DeleteAll();
            Writer.Commit();
        }

        public void Commit()
        {
            Writer.Commit();
        }

        public void Dispose()
        {
            Writer.Commit();
            Writer.Dispose();
            taxoWriter.Dispose();
        }
    }
}

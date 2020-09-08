using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore;
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

namespace FreeMarketOne.Search
{
    public class SearchIndexer
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
            var dirTaxonomy = FSDirectory.Open(indexLocation + "/taxonomy/");

            //create an analyzer to process the text
            var analyzer = new StandardAnalyzer(AppLuceneVersion);

            //create an index writer
            indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
            writer = new IndexWriter(dir, indexConfig);
            taxoWriter = new DirectoryTaxonomyWriter(dirTaxonomy);
            _marketManager = marketManager;
        }

        public IndexWriter Writer
        {
            get
            {
                return writer;
            }
        }
        /// <summary>
        /// Indexes market items posted on blockchain for search. This does not index blocks.  
        /// </summary>
        /// <param name="MarketItem"></param>
        public void Index(MarketItem MarketItem)
        {
            double pricePerGram = 0F;
            MarketManager.MarketCategoryEnum cat = (MarketManager.MarketCategoryEnum)MarketItem.Category;
            new DealType().TryGetValue(MarketItem.DealType, out string dealTypeString);
            
            //if we are re-running the block and same hash comes again, delete and add it again
            Writer.DeleteDocuments(new Term("ID", MarketItem.Hash));
            if (!string.IsNullOrEmpty(MarketItem.Signature))
            {
                Writer.DeleteDocuments(new Term("Signature", MarketItem.Signature));
            }
            //if market item has a baseSignature then it's previous version's signature
            //to remove previous versions of document from search we delete by signature
            if (!string.IsNullOrEmpty(MarketItem.BaseSignature)) {
                Writer.DeleteDocuments(new Term("Signature", MarketItem.BaseSignature));
            }

            if (MarketItem.WeightInGrams > 0 && MarketItem.Price>0) {
                pricePerGram = MarketItem.Price / MarketItem.WeightInGrams;
            }

            //transform seller pubkeys to sha hashes for simplified search use
            var sellerPubKeys = _marketManager.GetSellerPubKeyFromMarketItem(MarketItem);
            List<string> sellerPubKeyHashes = GenerateSellerPubKeyHashes(sellerPubKeys);

            Document doc = new Document
            {
                new StringField("ID", MarketItem.Hash, Field.Store.YES),
                new TextField("Title",MarketItem.Title,Field.Store.YES),
                new TextField("Description",MarketItem.Description,Field.Store.YES),
                new FacetField("Category",cat.ToString()),
                new FacetField("Shipping",MarketItem.Shipping),
                new FacetField("DealType",dealTypeString),
                new FacetField("Fineness",string.IsNullOrEmpty(MarketItem.Fineness) ? "Unspecified":MarketItem.Fineness),
                new FacetField("Manufacturer",string.IsNullOrEmpty(MarketItem.Manufacturer) ? "Unspecified":MarketItem.Manufacturer),
                new FacetField("Size",string.IsNullOrEmpty(MarketItem.Size) ? "Unspecified":MarketItem.Size),
                new FacetField("Sold",string.IsNullOrEmpty(MarketItem.BuyerSignature) ? "No": "Yes"),
                new NumericDocValuesField("WeightInGrams",MarketItem.WeightInGrams),                
                new DoubleDocValuesField("PricePerGram", pricePerGram),
                new DoubleDocValuesField("Price", MarketItem.Price)
            };

            //append all seller sha-hashes to a single multivalue field so that we can find by any.
            //this should support find all seller items usecase, provided we hash seller's keys for query
            foreach (var sellerPubKeyHash in sellerPubKeyHashes)
            {
                doc.Add(new StringField("SellerPubKeyHash", sellerPubKeyHash, Field.Store.NO));
            }

            Writer.AddDocument(facetConfig.Build(taxoWriter, doc));
            Writer.Flush(triggerMerge: true, applyAllDeletes: true);

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

        public void DeleteMarketItem(MarketItem MarketItem)
        {
            Writer.DeleteDocuments(new Term("ID", MarketItem.Hash));
            if (!string.IsNullOrEmpty(MarketItem.Signature))
            {
                Writer.DeleteDocuments(new Term("Signature", MarketItem.Signature));
            }
            //if market item has a baseSignature then it's previous version's signature
            //to remove previous versions of document from search we delete by signature
            if (!string.IsNullOrEmpty(MarketItem.BaseSignature))
            {
                Writer.DeleteDocuments(new Term("Signature", MarketItem.BaseSignature));
            }
            Writer.Flush(triggerMerge: true, applyAllDeletes: true);
        }

        public void DeleteAll()
        {
            Writer.DeleteAll();
            Writer.Commit();
            taxoWriter.Commit();
        }

        public void Commit()
        {
            Writer.Commit();
            taxoWriter.Commit();
        }
    }
}

using FreeMarketOne.DataStructure.Objects.BaseItems;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using static FreeMarketOne.ServerCore.MarketManager;

namespace FreeMarketOne.Search
{
    public class SearchIndexer
    {
        private readonly IndexWriter writer;
        private readonly DirectoryTaxonomyWriter taxoWriter;
        private readonly IndexWriterConfig indexConfig;
        private readonly FacetsConfig facetConfig = new FacetsConfig();

        public SearchIndexer(string indexLocation)
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
            MarketCategoryEnum cat = (MarketCategoryEnum)MarketItem.Category;
            new DealType().TryGetValue(MarketItem.DealType, out string dealTypeString);
            Writer.DeleteDocuments(new Term("ID", MarketItem.Hash));

            Document doc = new Document
            {
                new StringField("ID", MarketItem.Hash, Field.Store.YES),
                new StringField("ExactTitle",MarketItem.Title,Field.Store.YES),
                new TextField("Title",MarketItem.Title,Field.Store.YES),
                new TextField("Description",MarketItem.Description,Field.Store.YES),
                new FacetField("Category",cat.ToString()),
                new FacetField("Shipping",MarketItem.Shipping),
                new FacetField("DealType",dealTypeString),
                new FacetField("Fineness",MarketItem.Fineness),
                new FacetField("Manufacturer",MarketItem.Manufacturer),
                new FacetField("Size",MarketItem.Size),
                new FacetField("WeightInGrams",MarketItem.WeightInGrams)
            };
            Writer.AddDocument(facetConfig.Build(taxoWriter, doc));
            Writer.Flush(triggerMerge: true, applyAllDeletes: true);

        }

        public void DeleteMarketItem(string hash)
        {
            Writer.DeleteDocuments(new Term("ID", hash));
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

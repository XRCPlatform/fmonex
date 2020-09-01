using FreeMarketOne.DataStructure.Objects.MarketItems;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy.Directory;

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

            var dir = new RAMDirectory(); //FSDirectory.Open(indexLocation);
            var dirTaxonomy = new RAMDirectory(); //FSDirectory.Open(indexLocation + "/taxonomy/");

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

        public void Index(IMarketItem MarketItem)
        {
            MarketItemCategory cat = (MarketItemCategory)MarketItem.Category;
            new DealType().TryGetValue(MarketItem.DealType, out string dealTypeString);
            Writer.DeleteDocuments(new Term(MarketItem.Hash));

            Document doc = new Document
            {
                new StringField("ID", MarketItem.Hash, Field.Store.YES),
                new StringField("Title",MarketItem.Title,Field.Store.YES),
                new TextField("TokenizedTile",MarketItem.Title,Field.Store.YES),
                new TextField("Description",MarketItem.Description,Field.Store.YES),
                new FacetField("Category",cat.ToString()),
                new FacetField("Shipping",MarketItem.Shipping),
                new FacetField("DealType",dealTypeString)
            };
            Writer.AddDocument(facetConfig.Build(taxoWriter, doc));
            //writer.AddDocument(doc);
            Writer.Flush(triggerMerge: true, applyAllDeletes: true);
        }
    }
}

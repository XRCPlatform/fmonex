using FreeMarketOne.DataStructure.Objects.MarketItems;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Facet;

namespace FreeMarketOne.Search
{
    public class SearchIndexer
    {
        private readonly IndexWriter writer;

        public SearchIndexer(string indexLocation)
        {
            var AppLuceneVersion = LuceneVersion.LUCENE_48;

            var dir = FSDirectory.Open(indexLocation);

            //create an analyzer to process the text
            var analyzer = new StandardAnalyzer(AppLuceneVersion);

            //create an index writer
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
            writer = new IndexWriter(dir, indexConfig);

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
            Document doc = new Document
            {
                // StringField indexes but doesn't tokenize
                new StringField("Title",
                    MarketItem.Title,
                    Field.Store.YES),
                new TextField("TokenizedTile",
                    MarketItem.Title,
                    Field.Store.YES),
                new TextField("Description",
                    MarketItem.Description,
                    Field.Store.YES),
                 new FacetField("Category",
                    MarketItem.Category),
                 new FacetField("Shipping",
                    MarketItem.Shipping)
                
            };

            writer.AddDocument(doc);
            writer.Flush(triggerMerge: false, applyAllDeletes: false);
        }
    }
}

using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Text;


namespace FreeMarketOne.Search
{
    public partial class XRCTransactionScoreQuery : CustomScoreQuery
    {

        public XRCTransactionScoreQuery(Query subQuery) : base(subQuery)
        {
           
        }

        protected override CustomScoreProvider GetCustomScoreProvider(AtomicReaderContext context)
        {
            return new XRCScoreProvider(context) as CustomScoreProvider;
        }

        public class XRCScoreProvider : CustomScoreProvider
        {

            private readonly IndexReader _reader;
            private readonly ISet<string> _fieldsToLoad;

            public XRCScoreProvider(AtomicReaderContext context) : base(context)
            {
                _reader = context.Reader;
                _fieldsToLoad = new HashSet<string>();
                _fieldsToLoad.Add("XrcTotal");
            }


            public override float CustomScore(int doc_id, float currentScore, float valSrcScore)
            {
                Document doc = _reader.Document(doc_id, _fieldsToLoad);
                //  Get boost value from data
                float influence = 1;
                IIndexableField field = doc.GetField("XrcTotal");
                long? number = field?.GetInt64Value();
                float boost = ((float)(number.GetValueOrDefault() * influence));
                return (currentScore + boost);
            }


        }
    }

   

}
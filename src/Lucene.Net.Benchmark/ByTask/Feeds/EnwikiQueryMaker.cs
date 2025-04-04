﻿using J2N.Collections.Generic.Extensions;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Benchmarks.ByTask.Tasks;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// A QueryMaker that uses common and uncommon actual Wikipedia queries for
    /// searching the English Wikipedia collection. 90 queries total.
    /// </summary>
    public class EnwikiQueryMaker : AbstractQueryMaker, IQueryMaker
    {
        // common and a few uncommon queries from wikipedia search logs
        private static readonly string[] STANDARD_QUERIES = { "Images catbox gif", // LUCENENET: marked readonly
            "Imunisasi haram", "Favicon ico", "Michael jackson", "Unknown artist",
            "Lily Thai", "Neda", "The Last Song", "Metallica", "Nicola Tesla",
            "Max B", "Skil Corporation", "\"The 100 Greatest Artists of All Time\"",
            "\"Top 100 Global Universities\"", "Pink floyd", "Bolton Sullivan",
            "Frank Lucas Jr", "Drake Woods", "Radiohead", "George Freeman",
            "Oksana Grigorieva", "The Elder Scrolls V", "Deadpool", "Green day",
            "\"Red hot chili peppers\"", "Jennifer Bini Taylor",
            "The Paradiso Girls", "Queen", "3Me4Ph", "Paloma Jimenez", "AUDI A4",
            "Edith Bouvier Beale: A Life In Pictures", "\"Skylar James Deleon\"",
            "Simple Explanation", "Juxtaposition", "The Woody Show", "London WITHER",
            "In A Dark Place", "George Freeman", "LuAnn de Lesseps", "Muhammad.",
            "U2", "List of countries by GDP", "Dean Martin Discography", "Web 3.0",
            "List of American actors", "The Expendables",
            "\"100 Greatest Guitarists of All Time\"", "Vince Offer.",
            "\"List of ZIP Codes in the United States\"", "Blood type diet",
            "Jennifer Gimenez", "List of hobbies", "The beatles", "Acdc",
            "Nightwish", "Iron maiden", "Murder Was the Case", "Pelvic hernia",
            "Naruto Shippuuden", "campaign", "Enthesopathy of hip region",
            "operating system", "mouse",
            "List of Xbox 360 games without region encoding", "Shakepearian sonnet",
            "\"The Monday Night Miracle\"", "India", "Dad's Army",
            "Solanum melanocerasum", "\"List of PlayStation Portable Wi-Fi games\"",
            "Little Pixie Geldof", "Planes, Trains & Automobiles", "Freddy Ingalls",
            "The Return of Chef", "Nehalem", "Turtle", "Calculus", "Superman-Prime",
            "\"The Losers\"", "pen-pal", "Audio stream input output", "lifehouse",
            "50 greatest gunners", "Polyfecalia", "freeloader", "The Filthy Youth" };

        private static Query[] GetPrebuiltQueries(string field)
        {
            WildcardQuery wcq = new WildcardQuery(new Term(field, "fo*"));
            wcq.MultiTermRewriteMethod = MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE;
            // be wary of unanalyzed text
            return new Query[] {
                new SpanFirstQuery(new SpanTermQuery(new Term(field, "ford")), 5),
                new SpanNearQuery(new SpanQuery[] {
                new SpanTermQuery(new Term(field, "night")),
                new SpanTermQuery(new Term(field, "trading")) }, 4, false),
                new SpanNearQuery(new SpanQuery[] {
                new SpanFirstQuery(new SpanTermQuery(new Term(field, "ford")), 10),
                new SpanTermQuery(new Term(field, "credit")) }, 10, false), wcq, };
        }

        /// <summary>
        /// Parse the strings containing Lucene queries.
        /// </summary>
        /// <param name="qs">array of strings containing query expressions</param>
        /// <param name="a">analyzer to use when parsing queries</param>
        /// <returns>array of Lucene queries</returns>
        private static Query[] CreateQueries(IList<object> qs, Analyzer a)
        {
            QueryParser qp = new QueryParser(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
                DocMaker.BODY_FIELD, a);
            IList<Query> queries = new JCG.List<Query>();
            for (int i = 0; i < qs.Count; i++)
            {
                try
                {
                    object query = qs[i];
                    Query q = null;
                    if (query is string queryString)
                    {
                        q = qp.Parse(queryString);

                    }
                    else if (query is Query queryObj)
                    {
                        q = queryObj;
                    }
                    else
                    {
                        Console.WriteLine("Unsupported Query Type: " + query);
                    }

                    if (q != null)
                    {
                        queries.Add(q);
                    }

                }
                catch (Exception e) when (e.IsException())
                {
                    e.PrintStackTrace();
                }
            }

            return queries.ToArray();
        }

        protected override Query[] PrepareQueries()
        {
            // analyzer (default is standard analyzer)
            Analyzer anlzr = NewAnalyzerTask.CreateAnalyzer(m_config.Get("analyzer", typeof(StandardAnalyzer).AssemblyQualifiedName));

            JCG.List<object> queryList = new JCG.List<object>(20);
            queryList.AddRange(STANDARD_QUERIES);
            if (!m_config.Get("enwikiQueryMaker.disableSpanQueries", false))
                queryList.AddRange(GetPrebuiltQueries(DocMaker.BODY_FIELD));
            return CreateQueries(queryList, anlzr);
        }
    }
}

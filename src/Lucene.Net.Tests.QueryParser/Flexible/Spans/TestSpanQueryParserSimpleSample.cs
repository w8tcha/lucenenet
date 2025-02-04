﻿using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Parser;
using Lucene.Net.Search.Spans;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.QueryParsers.Flexible.Spans
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
    /// This test case demonstrates how the new query parser can be used.
    /// <para/>
    /// It tests queries likes "term", "field:term" "term1 term2" "term1 OR term2",
    /// which are all already supported by the current syntax parser (
    /// <see cref="StandardSyntaxParser"/>).
    /// <para/>
    /// The goals is to create a new query parser that supports only the pair
    /// "field:term" or a list of pairs separated or not by an OR operator, and from
    /// this query generate <see cref="SpanQuery"/> objects instead of the regular
    /// <see cref="Search.Query"/> objects. Basically, every pair will be converted to a
    /// <see cref="SpanTermQuery"/> object and if there are more than one pair they will be
    /// grouped by an <see cref="OrQueryNode"/>.
    /// <para/>
    /// Another functionality that will be added is the ability to convert every
    /// field defined in the query to an unique specific field.
    /// <para/>
    /// The query generation is divided in three different steps: parsing (syntax),
    /// processing (semantic) and building.
    /// <para/>
    /// The parsing phase, as already mentioned will be performed by the current
    /// query parser: <see cref="StandardSyntaxParser"/>.
    /// <para/>
    /// The processing phase will be performed by a processor pipeline which is
    /// compound by 2 processors: <see cref="SpansValidatorQueryNodeProcessor"/> and
    /// <see cref="UniqueFieldQueryNodeProcessor"/>.
    /// <para/>
    ///     <see cref="SpansValidatorQueryNodeProcessor"/>: as it's going to use the current
    ///     query parser to parse the syntax, it will support more features than we want,
    ///     this processor basically validates the query node tree generated by the parser
    ///     and just let got through the elements we want, all the other elements as
    ///     wildcards, range queries, etc...if found, an exception is thrown.
    ///     <para/>
    ///     <see cref="UniqueFieldQueryNodeProcessor"/>: this processor will take care of reading
    ///     what is the &quot;unique field&quot; from the configuration and convert every field defined
    ///     in every pair to this &quot;unique field&quot;. For that, a <see cref="SpansQueryConfigHandler"/> is
    ///     used, which has the <see cref="IUniqueFieldAttribute"/> defined in it.
    /// <para/>
    /// The building phase is performed by the <see cref="SpansQueryTreeBuilder"/>, which
    /// basically contains a map that defines which builder will be used to generate
    /// <see cref="SpanQuery"/> objects from <see cref="IQueryNode"/> objects.
    /// </summary>
    /// <seealso cref="TestSpanQueryParser">for a more advanced example</seealso>
    ///
    /// <seealso cref="SpansQueryConfigHandler"/>
    /// <seealso cref="SpansQueryTreeBuilder"/>
    /// <seealso cref="SpansValidatorQueryNodeProcessor"/>
    /// <seealso cref="SpanOrQueryNodeBuilder"/>
    /// <seealso cref="SpanTermQueryNodeBuilder"/>
    /// <seealso cref="StandardSyntaxParser"/>
    /// <seealso cref="UniqueFieldQueryNodeProcessor"/>
    /// <seealso cref="IUniqueFieldAttribute"/>
    public class TestSpanQueryParserSimpleSample : LuceneTestCase
    {
        [Test]
        public void TestBasicDemo()
        {
            ISyntaxParser queryParser = new StandardSyntaxParser();

            // convert the CharSequence into a QueryNode tree
            IQueryNode queryTree = queryParser.Parse("body:text", null);

            // create a config handler with a attribute used in
            // UniqueFieldQueryNodeProcessor
            QueryConfigHandler spanQueryConfigHandler = new SpansQueryConfigHandler();
            spanQueryConfigHandler.Set(SpansQueryConfigHandler.UNIQUE_FIELD, "index");

            // set up the processor pipeline with the ConfigHandler
            // and create the pipeline for this simple demo
            QueryNodeProcessorPipeline spanProcessorPipeline = new QueryNodeProcessorPipeline(
                spanQueryConfigHandler);
            // @see SpansValidatorQueryNodeProcessor
            spanProcessorPipeline.Add(new SpansValidatorQueryNodeProcessor());
            // @see UniqueFieldQueryNodeProcessor
            spanProcessorPipeline.Add(new UniqueFieldQueryNodeProcessor());

            // print to show out the QueryNode tree before being processed
            if (Verbose) Console.WriteLine(queryTree);

            // Process the QueryTree using our new Processors
            queryTree = spanProcessorPipeline.Process(queryTree);

            // print to show out the QueryNode tree after being processed
            if (Verbose) Console.WriteLine(queryTree);

            // create a instance off the Builder
            SpansQueryTreeBuilder spansQueryTreeBuilder = new SpansQueryTreeBuilder();

            // convert QueryNode tree to span query Objects
            SpanQuery spanquery = (SpanQuery)spansQueryTreeBuilder.Build(queryTree); // LUCENENET TODO: Find a way to remove the cast

            assertTrue(spanquery is SpanTermQuery);
            assertEquals(spanquery.toString(), "index:text");

        }
    }
}

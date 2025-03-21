﻿using J2N.Text;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

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
    /// Base class for source of data for benchmarking.
    /// </summary>
    /// <remarks>
    /// Keeps track of various statistics, such as how many data items were generated,
    /// size in bytes etc.
    /// <para/>
    /// Supports the following configuration parameters:
    /// <list type="bullet">
    ///     <item><term>content.source.forever</term><description>specifies whether to generate items forever (<b>default=true</b>).</description></item>
    ///     <item><term>content.source.verbose</term><description>specifies whether messages should be output by the content source (<b>default=false</b>).</description></item>
    ///     <item><term>content.source.encoding</term><description>
    ///         specifies which encoding to use when
    ///         reading the files of that content source. Certain implementations may define
    ///         a default value if this parameter is not specified. (<b>default=null</b>).
    ///     </description></item>
    ///     <item><term>content.source.log.step</term><description>
    ///         specifies for how many items a
    ///         message should be logged. If set to 0 it means no logging should occur.
    ///         <b>NOTE:</b> if verbose is set to false, logging should not occur even if
    ///         logStep is not 0 (<b>default=0</b>).
    ///     </description></item>
    /// </list>
    /// </remarks>
    public abstract class ContentItemsSource : IDisposable
    {
        private long bytesCount;
        private long totalBytesCount;
        private int itemCount;
        private int totalItemCount;
        private Config config;

        private int lastPrintedNumUniqueTexts = 0;
        private long lastPrintedNumUniqueBytes = 0;
        private int printNum = 0;

        protected bool m_forever;
        protected int m_logStep;
        protected bool m_verbose;
        protected Encoding m_encoding;

        /// <summary>update count of bytes generated by this source</summary>
        protected void AddBytes(long numBytes)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                bytesCount += numBytes;
                totalBytesCount += numBytes;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>update count of items generated by this source</summary>
        protected void AddItem()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                ++itemCount;
                ++totalItemCount;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// A convenience method for collecting all the files of a content source from
        /// a given directory. The collected <see cref="FileInfo"/> instances are stored in the
        /// given <paramref name="files"/>.
        /// </summary>
        protected void CollectFiles(DirectoryInfo dir, IList<FileInfo> files)
        {
            CollectFilesImpl(dir, files);
            files.Sort(FileNameComparer.Default);
        }

        private void CollectFilesImpl(DirectoryInfo dir, IList<FileInfo> files)
        {
            foreach (var sub in dir.EnumerateDirectories())
            {
                CollectFilesImpl(sub, files);
            }

            files.AddRange(dir.GetFiles());
        }

        private class FileNameComparer : IComparer<FileInfo>
        {
            private FileNameComparer() { } // LUCENENET: Made into singleton

            public static IComparer<FileInfo> Default { get; } = new FileNameComparer();

            public int Compare(FileInfo x, FileInfo y)
            {
                return x.FullName.CompareToOrdinal(y.FullName);
            }
        }

        /// <summary>
        /// Returns <c>true</c> whether it's time to log a message (depending on verbose and
        /// the number of items generated).
        /// </summary>
        /// <returns></returns>
        protected bool ShouldLog()
        {
            return m_verbose && m_logStep > 0 && itemCount % m_logStep == 0;
        }

        /// <summary>Called when reading from this content source is no longer required.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Called when reading from this content source is no longer required.</summary>
        protected abstract void Dispose(bool disposing);


        /// <summary>Returns the number of bytes generated since last reset.</summary>
        public long BytesCount => bytesCount;

        /// <summary>Returns the number of generated items since last reset.</summary>
        public int ItemsCount => itemCount;

        public Config Config => config;

        /// <summary>Returns the total number of bytes that were generated by this source.</summary>
        public long TotalBytesCount => totalBytesCount;

        /// <summary>Returns the total number of generated items.</summary>
        public int TotalItemsCount => totalItemCount;

        /// <summary>
        /// Resets the input for this content source, so that the test would behave as
        /// if it was just started, input-wise.
        /// <para/>
        /// <b>NOTE:</b> the default implementation resets the number of bytes and
        /// items generated since the last reset, so it's important to call
        /// <c>base.ResetInputs()</c> in case you override this method.
        /// </summary>
        public virtual void ResetInputs()
        {
            bytesCount = 0;
            itemCount = 0;
        }

        /// <summary>
        /// Sets the <see cref="Utils.Config"/> for this content source. If you override this
        /// method, you must call <c>base.SetConfig(config)</c>.
        /// </summary>
        /// <param name="config"></param>
        public virtual void SetConfig(Config config)
        {
            this.config = config;
            m_forever = config.Get("content.source.forever", true);
            m_logStep = config.Get("content.source.log.step", 0);
            m_verbose = config.Get("content.source.verbose", false);
            string encodingStr = config.Get("content.source.encoding", null);
            if (!string.IsNullOrWhiteSpace(encodingStr))
            {
                m_encoding = Encoding.GetEncoding(encodingStr);
            }
            else
            {
                m_encoding = Encoding.Default; // Default system encoding
            }
        }

        public virtual void PrintStatistics(string itemsName)
        {
            if (!m_verbose)
            {
                return;
            }
            bool print = false;
            string col = "                  ";
            StringBuilder sb = new StringBuilder();
            string newline = Environment.NewLine;
            sb.Append("------------> ").Append(GetType().Name).Append(" statistics (").Append(printNum).Append("): ").Append(newline);
            int nut = TotalItemsCount;
            if (nut > lastPrintedNumUniqueTexts)
            {
                print = true;
                sb.Append("total count of " + itemsName + ": ").Append(Formatter.Format(0, nut, col)).Append(newline);
                lastPrintedNumUniqueTexts = nut;
            }
            long nub = TotalBytesCount;
            if (nub > lastPrintedNumUniqueBytes)
            {
                print = true;
                sb.Append("total bytes of " + itemsName + ": ").Append(Formatter.Format(0, nub, col)).Append(newline);
                lastPrintedNumUniqueBytes = nub;
            }
            if (ItemsCount > 0)
            {
                print = true;
                sb.Append("num " + itemsName + " added since last inputs reset:   ").Append(Formatter.Format(0, ItemsCount, col)).Append(newline);
                sb.Append("total bytes added for " + itemsName + " since last inputs reset: ").Append(Formatter.Format(0, BytesCount, col)).Append(newline);
            }
            if (print)
            {
                Console.WriteLine(sb.Append(newline).ToString());
                printNum++;
            }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#nullable enable

namespace Lucene.Net.Util
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
    /// Allocation information (Thread, allocation stack) for tracking disposable
    /// resources.
    /// </summary>
    internal sealed class DisposableResourceInfo // From randomizedtesing
    {
        private readonly IDisposable resource;
        private readonly LifecycleScope scope;
        private readonly StackTrace stackTrace;
        private readonly string? threadName;

        public DisposableResourceInfo(IDisposable resource, LifecycleScope scope, string? threadName, StackTrace stackTrace)
        {
            Debug.Assert(resource != null);

            this.resource = resource!;
            this.scope = scope;
            this.stackTrace = stackTrace;
            this.threadName = threadName;
        }

        public IDisposable Resource => resource;

        public StackTrace StackTrace => stackTrace;

        public LifecycleScope Scope => scope;

        public string? ThreadName => threadName;
    }
}
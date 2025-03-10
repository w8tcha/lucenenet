﻿// Source: https://github.com/nunit/nunit/blob/v3.14.0/src/NUnitFramework/TestBuilder.cs

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using NUnit.Compatibility;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal.Builders;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using NUnit.Framework.Internal.Execution;
using System.Linq;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.NUnit.TestUtilities
{
    #region Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License.

    // Copyright (c) 2021 Charlie Poole, Rob Prouse
    // 
    // Permission is hereby granted, free of charge, to any person obtaining a copy
    // of this software and associated documentation files (the "Software"), to deal
    // in the Software without restriction, including without limitation the rights
    // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    // copies of the Software, and to permit persons to whom the Software is
    // furnished to do so, subject to the following conditions:
    // 
    // The above copyright notice and this permission notice shall be included in
    // all copies or substantial portions of the Software.
    // 
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    // THE SOFTWARE.

    #endregion

    /// <summary>
    /// Utility Class used to build and run NUnit tests used as test data
    /// </summary>
    public static class TestBuilder
    {
        #region Build Tests

        public static TestSuite MakeSuite(string name)
        {
            return new TestSuite(name);
        }

        public static TestSuite MakeFixture(Type type)
        {
            //return (TestSuite)new DefaultSuiteBuilder().BuildFrom(new TypeWrapper(type));
            TestSuite suite = (TestSuite)new DefaultSuiteBuilder().BuildFrom(new TypeWrapper(type));

            // LUCENENET: We wrapped our TestSuite in another TestSuite
            // (SetUpFixture), so we need to get the child test (the class).
            if (suite is not null && suite.TypeInfo.Type.Equals(typeof(LuceneTestCase.SetUpFixture)))
            {
                suite = (TestSuite)suite.Tests[0];
            }
            return suite;
        }

        public static TestSuite MakeFixture(object fixture)
        {
            TestSuite suite = MakeFixture(fixture.GetType());
            suite.Fixture = fixture;
            return suite;
        }

        public static TestSuite MakeParameterizedMethodSuite(Type type, string methodName)
        {
            var suite = MakeTestFromMethod(type, methodName) as TestSuite;
            Assert.NotNull(suite, "Unable to create parameterized suite - most likely there is no data provided");
            return suite;
        }

        public static TestMethod MakeTestCase(Type type, string methodName)
        {
            var test = MakeTestFromMethod(type, methodName) as TestMethod;
            Assert.NotNull(test, "Unable to create TestMethod from {0}", methodName);

            return test;
        }

        // Will return either a ParameterizedMethodSuite or an NUnitTestMethod
        // depending on whether the method takes arguments or not
        internal static Test MakeTestFromMethod(Type type, string methodName)
        {
            var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
                Assert.Fail("Method not found: " + methodName);
            var test = new DefaultTestCaseBuilder().BuildFrom(new MethodWrapper(type, method));
            // LUCENENET: We need to initialize the RandomizedContext for our custom attributes to work.
            InitializeTestFixture(test);
            return test;
        }

        internal static void InitializeTestFixture(Test fixture)
        {
            var random = new J2N.Randomizer(); // LUCENENET TOOD: Initilize this to make it repeatable?
            long initialSeed = random.NextInt64();
            long testSeed = random.NextInt64();
            var randomizedContext = new RandomizedContext(fixture, fixture.TypeInfo.Assembly, initialSeed, testSeed);
            _ = new LuceneRandomSeedInitializer().InitializeTestFixture(fixture, randomizedContext);
        }

        #endregion

        #region Create WorkItems

        public static WorkItem CreateWorkItem(Type type)
        {
            return CreateWorkItem(MakeFixture(type));
        }

        public static WorkItem CreateWorkItem(Type type, string methodName)
        {
            return CreateWorkItem(MakeTestFromMethod(type, methodName));
        }

        public static WorkItem CreateWorkItem(Test test)
        {
            var context = new TestExecutionContext();
            context.Dispatcher = new SuperSimpleDispatcher();

            return CreateWorkItem(test, context);
        }

        public static WorkItem CreateWorkItem(Test test, object testObject)
        {
            var context = new TestExecutionContext();
            context.TestObject = testObject;
            context.Dispatcher = new SuperSimpleDispatcher();

            return CreateWorkItem(test, context);
        }

        public static WorkItem CreateWorkItem(Test test, TestExecutionContext context)
        {
            // LUCENENET: IDebugger is internal, so our only options are to use this obsolete method or to use Reflection.
#pragma warning disable CS0618 // Type or member is obsolete
            var work = WorkItemBuilder.CreateWorkItem(test, TestFilter.Empty, true);
#pragma warning restore CS0618 // Type or member is obsolete
            work.InitializeContext(context);

            return work;
        }

        #endregion

        #region Run Tests

        public static ITestResult RunTestFixture(Type type)
        {
            return RunTest(MakeFixture(type), null);
        }

        public static ITestResult RunTestFixture(object fixture)
        {
            return RunTest(MakeFixture(fixture), fixture);
        }

        public static ITestResult RunParameterizedMethodSuite(Type type, string methodName)
        {
            var suite = MakeParameterizedMethodSuite(type, methodName);

            object testObject = null;
            if (!type.IsStatic())
                testObject = Reflect.Construct(type);

            return RunTest(suite, testObject);
        }

        public static ITestResult RunTestCase(Type type, string methodName)
        {
            var testMethod = MakeTestCase(type, methodName);

            object testObject = null;
            if (!type.IsStatic())
                testObject = Reflect.Construct(type);

            return RunTest(testMethod, testObject);
        }

        public static ITestResult RunTestCase(object fixture, string methodName)
        {
            var testMethod = MakeTestCase(fixture.GetType(), methodName);

            return RunTest(testMethod, fixture);
        }

        public static ITestResult RunAsTestCase(Action action)
        {
            var method = action.GetMethodInfo();
            var testMethod = MakeTestCase(method.DeclaringType, method.Name);
            return RunTest(testMethod);
        }

        public static ITestResult RunTest(Test test)
        {
            return RunTest(test, null);
        }

        public static ITestResult RunTest(Test test, object testObject)
        {
            return ExecuteWorkItem(CreateWorkItem(test, testObject));
        }

        public static ITestResult ExecuteWorkItem(WorkItem work)
        {
            work.Execute();

            // TODO: Replace with an event - but not while method is static
            while (work.State != WorkItemState.Complete)
            {
                Thread.Sleep(1);
            }

            return work.Result;
        }

        #endregion

        #region Nested TestDispatcher Class

        /// <summary>
        /// SuperSimpleDispatcher merely executes the work item.
        /// It is needed particularly when running suites, since
        /// the child items require a dispatcher in the context.
        /// </summary>
        class SuperSimpleDispatcher : IWorkItemDispatcher
        {
            public int LevelOfParallelism { get { return 0; } }

            public void Start(WorkItem topLevelWorkItem)
            {
                topLevelWorkItem.Execute();
            }

            public void Dispatch(WorkItem work)
            {
                work.Execute();
            }

            public void CancelRun(bool force)
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}

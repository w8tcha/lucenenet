﻿// Source: https://github.com/nunit/nunit/blob/v3.14.0/src/NUnitFramework/tests/Attributes/RepeatAttributeTests.cs

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal.Builders;
using NUnit.Framework.Internal.Commands;
using NUnit.Framework.Internal;
using System;
using Lucene.Net.NUnit.TestUtilities;
using Lucene.Net.TestData.RepeatingTests;
using System.Linq;

namespace Lucene.Net.Attributes
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

    [TestFixture]
    public partial class RepeatAttributeTests
    {
        [TestCase(typeof(RepeatFailOnFirstTryFixture), "Failed(Child)", 1)]
        [TestCase(typeof(RepeatFailOnSecondTryFixture), "Failed(Child)", 2)]
        [TestCase(typeof(RepeatFailOnThirdTryFixture), "Failed(Child)", 3)]
        [TestCase(typeof(RepeatSuccessFixture), "Passed", 3)]
        [TestCase(typeof(RepeatedTestWithIgnoreAttribute), "Skipped:Ignored(Child)", 0)]
        [TestCase(typeof(RepeatIgnoredOnFirstTryFixture), "Skipped:Ignored(Child)", 1)]
        [TestCase(typeof(RepeatIgnoredOnSecondTryFixture), "Skipped:Ignored(Child)", 2)]
        [TestCase(typeof(RepeatIgnoredOnThirdTryFixture), "Skipped:Ignored(Child)", 3)]
        [TestCase(typeof(RepeatErrorOnFirstTryFixture), "Failed(Child)", 1)]
        [TestCase(typeof(RepeatErrorOnSecondTryFixture), "Failed(Child)", 2)]
        [TestCase(typeof(RepeatErrorOnThirdTryFixture), "Failed(Child)", 3)]
        public void RepeatWorksAsExpected(Type fixtureType, string outcome, int nTries)
        {
            RepeatingTestsFixtureBase fixture = (RepeatingTestsFixtureBase)Reflect.Construct(fixtureType);
            ITestResult result = TestBuilder.RunTestFixture(fixture);

            Assert.That(result.ResultState.ToString(), Is.EqualTo(outcome));
            Assert.AreEqual(1, fixture.FixtureSetupCount);
            Assert.AreEqual(1, fixture.FixtureTeardownCount);
            Assert.AreEqual(nTries, fixture.SetupCount);
            Assert.AreEqual(nTries, fixture.TeardownCount);
            Assert.AreEqual(nTries, fixture.Count);
        }

        [Test]
        public void RepeatOnNonLuceneTestCaseSubclass_ShouldFail()
        {
            RepeatFailedOnNonLuceneTestCaseSubclass fixture = (RepeatFailedOnNonLuceneTestCaseSubclass)Reflect.Construct(typeof(RepeatFailedOnNonLuceneTestCaseSubclass));
            ITestResult result = TestBuilder.RunTestFixture(fixture);
            Assert.AreEqual(1, result.FailCount);
            Assert.IsTrue(result.HasChildren);
            Assert.AreEqual(1, result.Children.Count());
            Assert.That(result.Children.Single().Message, Is.EqualTo("Lucene.Net.Util.LuceneTestCase+RepeatAttribute may only be used on a test in a subclass of LuceneTestCase."));
            Assert.AreEqual(1, result.Children.Single().FailCount);
        }

        [Test]
        public void RepeatUpdatesCurrentRepeatCountPropertyOnEachAttempt()
        {
            RepeatingTestsFixtureBase fixture = (RepeatingTestsFixtureBase)Reflect.Construct(typeof(RepeatedTestVerifyAttempt));
            ITestResult result = TestBuilder.RunTestCase(fixture, nameof(RepeatedTestVerifyAttempt.PassesTwoTimes));

            Assert.AreEqual(fixture.TearDownResults.Count, fixture.Count + 1, "expected the CurrentRepeatCount property to be one less than the number of executions");
            Assert.AreEqual(result.FailCount, 1, "expected that the test failed the last repetition");
        }

        [Test]
        public void RepeatUpdatesCurrentRepeatCountPropertyOnGreenTest()
        {
            RepeatingTestsFixtureBase fixture = (RepeatingTestsFixtureBase)Reflect.Construct(typeof(RepeatedTestVerifyAttempt));
            ITestResult result = TestBuilder.RunTestCase(fixture, nameof(RepeatedTestVerifyAttempt.AlwaysPasses));

            Assert.AreEqual(fixture.TearDownResults.Count, fixture.Count + 1, "expected the CurrentRepeatCount property to be one less than the number of executions");
            Assert.AreEqual(result.FailCount, 0, "expected that the test passes all repetitions without a failure");
        }

        [Test]
        public void CategoryWorksWithRepeatedTest()
        {
            TestSuite suite = TestBuilder.MakeFixture(typeof(RepeatedTestWithCategory));
            Test test = suite.Tests[0] as Test;
            System.Collections.IList categories = test.Properties["Category"];
            Assert.IsNotNull(categories);
            Assert.AreEqual(1, categories.Count);
            Assert.AreEqual("SAMPLE", categories[0]);
        }

        [Test]
        public void NotRunnableWhenIMethodInfoAbstractionReturnsMultipleIRepeatTestAttributes()
        {
            var fixtureSuite = new DefaultSuiteBuilder().BuildFrom(new CustomTypeWrapper(
                new TypeWrapper(typeof(FixtureWithMultipleRepeatAttributesOnSameMethod)),
                extraMethodAttributes: new Attribute[]
                {
                    new CustomRepeatAttribute(),
                    new RepeatAttribute(2)
                }));

            var method = fixtureSuite.Tests.Single();

            Assert.That(method.RunState, Is.EqualTo(RunState.NotRunnable));
            Assert.That(method.Properties.Get(PropertyNames.SkipReason), Is.EqualTo("Multiple attributes that repeat a test may cause issues."));
        }

        [Test]
        public void IRepeatTestAttributeIsEffectiveWhenAddedThroughIMethodInfoAbstraction()
        {
            var fixtureSuite = new DefaultSuiteBuilder().BuildFrom(new CustomTypeWrapper(
                new TypeWrapper(typeof(FixtureWithMultipleRepeatAttributesOnSameMethod)),
                extraMethodAttributes: new Attribute[]
                {
                    new RepeatAttribute(2)
                }));

            var fixtureInstance = new FixtureWithMultipleRepeatAttributesOnSameMethod();
            fixtureSuite.Fixture = fixtureInstance;
            TestBuilder.RunTest(fixtureSuite, fixtureInstance);

            Assert.That(fixtureInstance.MethodRepeatCount, Is.EqualTo(2));
        }

        private sealed class FixtureWithMultipleRepeatAttributesOnSameMethod
        {
            public int MethodRepeatCount { get; private set; }

            // The IRepeatTest attributes are dynamically applied via CustomTypeWrapper.
            [Test]
            public void MethodWithMultipleRepeatAttributes()
            {
                MethodRepeatCount++;
            }
        }

        private sealed class CustomRepeatAttribute : Attribute, IRepeatTest
        {
            public TestCommand Wrap(TestCommand command)
            {
                throw new NotImplementedException();
            }
        }
    }
}

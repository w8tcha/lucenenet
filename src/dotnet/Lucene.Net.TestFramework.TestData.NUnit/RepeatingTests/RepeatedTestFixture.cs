﻿// Source: https://github.com/nunit/nunit/blob/v3.14.0/src/NUnitFramework/testdata/RepeatedTestFixture.cs

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.TestData.RepeatingTests
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

    public class RepeatSuccessFixture : RepeatingTestsFixtureBase
    {
        [Test, Repeat(3)]
        public void RepeatSuccess()
        {
            Count++;
            Assert.IsTrue(true);
        }
    }

    public class RepeatFailOnFirstTryFixture : RepeatingTestsFixtureBase
    {
        [Test, Repeat(3)]
        public void RepeatFailOnFirst()
        {
            Count++;
            Assert.IsFalse(true);
        }
    }

    public class RepeatFailOnSecondTryFixture : RepeatingTestsFixtureBase
    {
        [Test, Repeat(3)]
        public void RepeatFailOnThird()
        {
            Count++;

            if (Count == 2)
                Assert.IsTrue(false);
        }
    }

    public class RepeatFailOnThirdTryFixture : RepeatingTestsFixtureBase
    {
        [Test, Repeat(3)]
        public void RepeatFailOnThird()
        {
            Count++;

            if (Count == 3)
                Assert.IsTrue(false);
        }
    }

    public class RepeatedTestWithIgnoreAttribute : RepeatingTestsFixtureBase
    {
        [Test, Repeat(3), Ignore("Ignore this test")]
        public void RepeatShouldIgnore()
        {
            Assert.Fail("Ignored test executed");
        }
    }

    public class RepeatIgnoredOnFirstTryFixture : RepeatingTestsFixtureBase
    {
        [Test, Repeat(3)]
        public void Test()
        {
            Count++;
            Assert.Ignore("Ignoring");
        }
    }

    public class RepeatIgnoredOnSecondTryFixture : RepeatingTestsFixtureBase
    {
        [Test, Repeat(3)]
        public void Test()
        {
            Count++;

            if (Count == 2)
                Assert.Ignore("Ignoring");
        }
    }

    public class RepeatIgnoredOnThirdTryFixture : RepeatingTestsFixtureBase
    {
        [Test, Repeat(3)]
        public void Test()
        {
            Count++;

            if (Count == 3)
                Assert.Ignore("Ignoring");
        }
    }

    public class RepeatErrorOnFirstTryFixture : RepeatingTestsFixtureBase
    {
        [Test, Repeat(3)]
        public void Test()
        {
            Count++;
            throw new Exception("Deliberate Exception");
        }
    }

    public class RepeatErrorOnSecondTryFixture : RepeatingTestsFixtureBase
    {
        [Test, Repeat(3)]
        public void Test()
        {
            Count++;

            if (Count == 2)
                throw new Exception("Deliberate Exception");
        }
    }

    public class RepeatErrorOnThirdTryFixture : RepeatingTestsFixtureBase
    {
        [Test, Repeat(3)]
        public void Test()
        {
            Count++;

            if (Count == 3)
                throw new Exception("Deliberate Exception");
        }
    }

    public class RepeatedTestWithCategory : RepeatingTestsFixtureBase
    {
        [Test, Repeat(3), Category("SAMPLE")]
        public void TestWithCategory()
        {
            Count++;
            Assert.IsTrue(true);
        }
    }

    public class RepeatedTestVerifyAttempt : RepeatingTestsFixtureBase
    {
        [Test, Repeat(3)]
        public void AlwaysPasses()
        {
            Count = TestContext.CurrentContext.CurrentRepeatCount;
        }

        [Test, Repeat(3)]
        public void PassesTwoTimes()
        {
            Assert.That(Count, Is.EqualTo(TestContext.CurrentContext.CurrentRepeatCount), "expected CurrentRepeatCount to be incremented only after first two attempts");
            if (Count > 1)
            {
                Assert.Fail("forced failure on 3rd repetition");
            }
            Count++;
        }
    }

    public class RepeatFailedOnNonLuceneTestCaseSubclass
    {
        [Test, Lucene.Net.Util.LuceneTestCase.Repeat(3)]
        public void AlwaysFails()
        {
            // Intentionally empty
        }
    }
}

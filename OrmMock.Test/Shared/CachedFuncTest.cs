// Copyright(c) 2017, 2018 Johan Lindvall
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace Test.Shared
{
    using NUnit.Framework;
    using OrmMock.Shared;
    using System;

    [TestFixture]
    class CachedFuncTest
    {
        private int count;

        private int result;

        private bool fail;

        [SetUp]
        public void Setup()
        {
            this.count = 0;
            this.fail = false;
            this.result = 123;
        }

        public int Function()
        {
            ++this.count;

            if (this.fail)
            {
                throw new InvalidOperationException();
            }

            return this.result;
        }

        [Test]
        public void TestOnce()
        {
            var cached = CachedFunc.Create(this.Function);

            var actual = cached();

            Assert.AreEqual(123, actual);
            Assert.AreEqual(1, this.count);
        }

        [Test]
        public void TestTwice()
        {
            var cached = CachedFunc.Create(this.Function);

            // ReSharper disable once RedundantAssignment
            var actual = cached();
            actual = cached();

            Assert.AreEqual(123, actual);
            Assert.AreEqual(1, this.count);
        }

        [Test]
        public void TestFail()
        {
            var cached = CachedFunc.Create(this.Function);

            this.fail = true;

            Assert.Throws<InvalidOperationException>(() => cached());
        }

        [Test]
        public void TestFailRetry()
        {
            var cached = CachedFunc.Create(this.Function);

            this.fail = true;
            Assert.Throws<InvalidOperationException>(() => cached());
            this.fail = false;

            var actual = cached();

            Assert.AreEqual(123, actual);
            Assert.AreEqual(2, this.count);
        }
    }
}

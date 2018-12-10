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

namespace OrmMock.EF6.Test
{
    using MemDb;
    using NSubstitute;
    using NUnit.Framework;
    using Shared;

    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Threading.Tasks;

    [TestFixture]
    public class MemDbSetTest
    {
        private IMemDb memDb;

        private MemDbSet<TestObject> set;

        private IList<TestObject> testObjects;

        [SetUp]
        public void Setup()
        {
            this.testObjects = new List<TestObject>
            {
                new TestObject { Name = "foo" },
                new TestObject { Name = "bar "}
            };

            this.memDb = Substitute.For<IMemDb>();

            this.memDb.Get<TestObject>().Returns(_ => this.testObjects);
            this.memDb.Get<TestObject>(Arg.Any<Keys>()).Returns(ci => this.testObjects.SingleOrDefault(t => t.Name == ci.Arg<Keys>().Data[0] as string));

            this.set = this.memDb.DbSet<TestObject>();
        }

        [Test]
        public async Task TestQueryAsync()
        {
            var data = await this.set.Where(d => d.Name != "foo").ToListAsync();
            Assert.AreEqual(1, data.Count);

            this.memDb.Received(2).Get<TestObject>();
        }

        [Test]
        public void TestQuery()
        {
            var data = this.set.Where(d => d.Name != "foo").ToList();
            Assert.AreEqual(1, data.Count);

            this.memDb.Received(2).Get<TestObject>();
        }

        [Test]
        public async Task TestFindAsync()
        {
            var data = await this.set.FindAsync("foo");
            Assert.AreEqual("foo", data.Name);
            this.memDb.Received(1).Get<TestObject>(Arg.Any<Keys>());
        }

        public class TestObject
        {
            public Guid Id { get; set; }

            public string Name { get; set; }
        }
    }
}

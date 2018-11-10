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
// SOFTWARE

namespace Test
{
    using System;
    using NUnit.Framework;
    using OrmMock;

    [TestFixture]
    public class RelationsTest
    {
        public class TestClass1
        {
            public int Id { get; set; }
        }

        public class TestClass2
        {
            public int Id1 { get; set; }

            public short Id2 { get; set; }

        }

        public class TestClass3
        {
            public int KeyId { get; set; }

            public int TestClass1Id { get; set; }

            public TestClass1 TestClass1 { get; set; }

            public int TestClass2Id1 { get; set; }

            public short TestClass2Id2 { get; set; }
        }

        private Relations relations;

        [SetUp]
        public void Setup()
        {
            this.relations = new Relations();
        }

        [Test]
        public void TestGetPrimaryKeys()
        {
            this.relations.RegisterPrimaryKeys<TestClass2>(tc => new { tc.Id1, tc.Id2 });

            var pk1 = this.relations.GetPrimaryKeys(typeof(TestClass1));
            var pk2 = this.relations.GetPrimaryKeys(typeof(TestClass2));

            CollectionAssert.AreEqual(new[] { typeof(TestClass1).GetProperty(nameof(TestClass1.Id)) }, pk1);
            CollectionAssert.AreEqual(new[] { typeof(TestClass2).GetProperty(nameof(TestClass2.Id1)), typeof(TestClass2).GetProperty(nameof(TestClass2.Id2)) }, pk2);
            Assert.Throws<InvalidOperationException>(() => this.relations.GetPrimaryKeys(typeof(TestClass3)));
        }

        [Test]
        public void TestGetForeignKeys()
        {
            this.relations.RegisterPrimaryKeys<TestClass2>(tc => new { tc.Id1, tc.Id2 });
            this.relations.RegisterForeignKeys<TestClass3, TestClass2, object>(tc => new { tc.TestClass2Id1, tc.TestClass2Id2 });

            var fk1 = this.relations.GetForeignKeys(typeof(TestClass3), typeof(TestClass1));
            var fk2 = this.relations.GetForeignKeys(typeof(TestClass3), typeof(TestClass2));
            Assert.Throws<InvalidOperationException>(() => this.relations.GetForeignKeys(typeof(TestClass1), typeof(TestClass2)));

            CollectionAssert.AreEqual(new[] { typeof(TestClass3).GetProperty(nameof(TestClass3.TestClass1Id)) }, fk1);
            CollectionAssert.AreEqual(new[] { typeof(TestClass3).GetProperty(nameof(TestClass3.TestClass2Id1)), typeof(TestClass3).GetProperty(nameof(TestClass3.TestClass2Id2)) }, fk2);
        }

        [Test]
        public void TestGetForeignKeysMismatch()
        {
            Assert.Throws<InvalidOperationException>(() => this.relations.RegisterForeignKeys<TestClass3, TestClass1, object>(tc => tc.TestClass2Id2));
        }

        [Test]
        public void Test11Relation()
        {
            this.relations.RegisterPrimaryKeys<TestClass2>(tc => tc.Id1);
            this.relations.Register11Relation<TestClass1, TestClass2>(tc => tc.Id, tc => tc.Id1);

            var fk1 = this.relations.GetForeignKeys(typeof(TestClass1), typeof(TestClass2));
            var fk2 = this.relations.GetForeignKeys(typeof(TestClass2), typeof(TestClass1));

            CollectionAssert.AreEqual(new[] { typeof(TestClass1).GetProperty(nameof(TestClass1.Id)) }, fk1);
            CollectionAssert.AreEqual(new[] { typeof(TestClass2).GetProperty(nameof(TestClass2.Id1)) }, fk2);
        }
    }
}

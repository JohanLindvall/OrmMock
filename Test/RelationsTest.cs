using System;
using NUnit.Framework;
using OrmMock;

namespace Test
{
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

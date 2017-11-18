using System;
using System.Linq;
using DataGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
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

        [TestInitialize]
        public void Setup()
        {
            this.relations = new Relations();
        }

        [TestMethod]
        public void TestGetPrimaryKeys()
        {
            this.relations.RegisterPrimaryKeys<TestClass2>(tc => new { tc.Id1, tc.Id2 });

            var pk1 = this.relations.GetPrimaryKeys(typeof(TestClass1));
            var pk2 = this.relations.GetPrimaryKeys(typeof(TestClass2));

            CollectionAssert.AreEqual(new[] { typeof(TestClass1).GetProperty(nameof(TestClass1.Id)) }, pk1);
            CollectionAssert.AreEqual(new[] { typeof(TestClass2).GetProperty(nameof(TestClass2.Id1)), typeof(TestClass2).GetProperty(nameof(TestClass2.Id2)) }, pk2);
            Assert.ThrowsException<InvalidOperationException>(() => this.relations.GetPrimaryKeys(typeof(TestClass3)));
        }

        [TestMethod]
        public void TestGetForeignKeys()
        {
            this.relations.RegisterPrimaryKeys<TestClass2>(tc => new { tc.Id1, tc.Id2 });
            this.relations.RegisterForeignKeys<TestClass3, TestClass2>(tc => new { tc.TestClass2Id1, tc.TestClass2Id2 });

            var fk1 = this.relations.GetForeignKeys(typeof(TestClass3), typeof(TestClass1));
            var fk2 = this.relations.GetForeignKeys(typeof(TestClass3), typeof(TestClass2));
            var fk3 = this.relations.GetForeignKeys(typeof(TestClass1), typeof(TestClass2));

            CollectionAssert.AreEqual(new[] { typeof(TestClass3).GetProperty(nameof(TestClass3.TestClass1Id)) }, fk1);
            CollectionAssert.AreEqual(new[] { typeof(TestClass3).GetProperty(nameof(TestClass3.TestClass2Id1)), typeof(TestClass3).GetProperty(nameof(TestClass3.TestClass2Id2)) }, fk2);
            Assert.IsNull(fk3);
        }

        [TestMethod]
        public void TestGetForeignKeysMismatch()
        {
            this.relations.RegisterForeignKeys<TestClass3, TestClass1>(tc => tc.TestClass2Id2);

            Assert.ThrowsException<InvalidOperationException>(() => this.relations.GetForeignKeys(typeof(TestClass3), typeof(TestClass1)));
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using DataGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class GeneratorTest
    {
        private ObjectContext ctx;

        [TestInitialize]
        public void Setup()
        {
            this.ctx = new Generator().CreateContext();
        }

        public class TestClass1
        {
            public int Id { get; set; }

            public virtual TestClass2 Class2 { get; set; }

            public int Class2Id { get; set; }

            public string Name { get; set; }
        }

        public class TestClass2
        {
            public int Id { get; set; }

            public virtual ICollection<TestClass1> Class1 { get; set; }

            public string Name { get; set; }
        }

        public class TestClass3
        {
            public TestClass3(TestClass1 tc1, Guid id)
            {

            }
        }

        public class TestClass4
        {
            public int Id { get; set; }

            public TestClass5 Class5 { get; set; }

            public int? Class5Id { get; set; }
        }


        public class TestClass5
        {
            public int Id { get; set; }

            public virtual ICollection<TestClass4> Class4 { get; set; }
        }

        public class TestClass6
        {
            public Guid Id { get; set; }

            public Guid Class7Id { get; set; }

            public Guid Class8Id { get; set; }

            public TestClass7 Class7 { get; set; }

            public TestClass8 Class8 { get; set; }

        }

        public class TestClass7
        {
            public Guid Id { get; set; }
            public Guid Class8Id { get; set; }
            public TestClass8 Class8 { get; set; }

            public ICollection<TestClass6> Class6 { get; set; }

        }

        public class TestClass8
        {
            public Guid Id { get; set; }
            public ICollection<TestClass7> Class7 { get; set; }

            public ICollection<TestClass6> Class6 { get; set; }
        }

        [TestMethod]
        public void TestReferences()
        {
            var result = this.ctx.Create<TestClass2>();
            Assert.AreEqual(3, result.Class1.Count);
            Assert.IsTrue(result.Class1.All(t => t.Class2Id == t.Class2.Id));
            Assert.IsTrue(result.Class1.All(t => object.ReferenceEquals(t.Class2, result)));
        }

        [TestMethod]
        public void TestReferences2()
        {
            var result = this.ctx.Create<TestClass1>();
            Assert.AreEqual(result.Class2.Class1.Single().Id, result.Id);
        }

        [TestMethod]
        public void TestReferences3()
        {
            var result = this.ctx.CreateMany<TestClass4>(10).ToList();
        }

        [TestMethod]
        public void TestReferences4()
        {
            var result = this.ctx.Create<TestClass6>();
            var tc6 = this.ctx.GetObjects<TestClass6>().Single();
            var tc7 = this.ctx.GetObjects<TestClass7>().Single();
            var tc8 = this.ctx.GetObjects<TestClass8>().Single();
        }

        [TestMethod]
        public void TestConstructor()
        {
            var result = this.ctx.Create<TestClass3>();
            Assert.IsNotNull(result);
        }
    }
}

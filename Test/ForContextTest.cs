using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OrmMock;

namespace Test
{
    [TestFixture]
    public class ForContextTest
    {
        private ObjectContext ctx;

        [SetUp]
        public void Setup()
        {
            this.ctx = new ObjectContext();
        }

        public class ClassWithConstructor
        {
            private int id;

            public int Id => this.id;

            public ClassWithConstructor(int id)
            {
                this.id = id;
            }
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
            public TestClass1 TestClass1 { get; }

            public Guid Id { get; }

            public TestClass3(TestClass1 tc1, Guid id)
            {
                this.TestClass1 = tc1;
                this.Id = id;
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

        public class TestClass9
        {
            public Guid Id { get; set; }

            public TestClass10 Class10 { get; set; }
        }

        public class TestClass10
        {
            public Guid Id { get; set; }

            public TestClass9 Class9 { get; set; }
        }

        public class TestNullable1
        {

            public Guid Id { get; set; }

            public Guid? Nullable2Id { get; set; }

            public TestNullable2 Nullable2 { get; set; }
        }

        public class TestNullable2
        {
            public Guid Id { get; set; }

            public ICollection<TestNullable1> Nullable1 { get; set; }
        }

        public class TestCircularClass
        {
            public int Id { get; set; }

            public TestCircularClass2 Circular { get; set; }

            public int CircularId { get; set; }
        }

        public class TestCircularClass2
        {
            public int Id { get; set; }

            public ICollection<TestCircularClass> Circulars { get; set; }

            public int CircularId { get; set; }


            public TestCircularClass3 Circular { get; set; }
        }

        public class TestCircularClass3
        {
            public int Id { get; set; }

            public ICollection<TestCircularClass2> Circulars { get; set; }

            public int CircularId { get; set; }


            public TestCircularClass Circular { get; set; }
        }

        public class SimpleClass
        {
            public string Prop1 { get; set; }

            public string Prop2 { get; set; }
        }

        [Test]
        public void TestReferences()
        {
            var result = this.ctx.Create<TestClass2>();
            Assert.AreEqual(3, result.Class1.Count);
            Assert.IsTrue(result.Class1.All(t => t.Class2Id == t.Class2.Id));
            Assert.IsTrue(result.Class1.All(t => object.ReferenceEquals(t.Class2, result)));
        }

        [Test]
        public void TestReferences2()
        {
            var result = this.ctx.Create<TestClass1>();
            Assert.AreEqual(result.Class2.Class1.Single().Id, result.Id);
        }

        [Test]
        public void TestReferences3()
        {
            var result = this.ctx.CreateMany<TestClass4>(10).ToList();
        }

        [Test]
        public void TestReferences4()
        {
            var result = this.ctx.Create<TestClass6>();
            var tc6 = this.ctx.GetObjects<TestClass6>().Single();
            var tc7 = this.ctx.GetObjects<TestClass7>().Single();
            // var tc8 = this.ctx.GetObjects<TestClass8>().Single();
        }

        [Test]
        public void TestNullable()
        {
            this.ctx.ObjectLimit = 9999;
            var result = this.ctx.CreateMany<TestNullable1>(1000).ToList();

            var nonNullCount = result.Count(t => t.Nullable2 != null);
            var nullCount = result.Count(t => t.Nullable2 == null);

            Assert.NotZero(nonNullCount);
            Assert.NotZero(nullCount);
        }

        [Test]
        public void TestSingleton()
        {
            this.ctx.For<TestClass2>()
                .RegisterSingleton();

            var result = this.ctx.CreateMany<TestClass1>(10).ToList();

            var singleton = this.ctx.GetSingleton<TestClass2>();

            Assert.IsTrue(result.All(t => object.ReferenceEquals(t.Class2, singleton)));
        }

        [Test]
        public void TestSingleton2()
        {
            var singleton = new TestClass2
            {
                Id = 123
            };
            this.ctx.For<TestClass2>().Use(singleton);

            var result = this.ctx.CreateMany<TestClass1>(10).ToList();

            Assert.IsTrue(result.All(t => object.ReferenceEquals(t.Class2, singleton)));
        }

        [Test]
        public void TestSingletonReferenceMismatch()
        {
            this.ctx.For<TestClass2>()
                .For(tc => tc.Class1)
                .Include(1);

            var obj1 = this.ctx.Create<TestClass2>();
            this.ctx.Singleton(this.ctx.GetObject<TestClass1>());
            Assert.Throws<InvalidOperationException>(() => this.ctx.Create<TestClass2>());
        }

        [Test]
        public void TestCircular()
        {
            var objs = this.ctx.Create<TestCircularClass>();
            Assert.AreEqual(3, this.ctx.GetObjects().Count());
        }

        [Test]
        public void TestLimit()
        {
            this.ctx.CreateMany<SimpleClass>(this.ctx.ObjectLimit).ToList();
            Assert.Throws<System.InvalidOperationException>(() => this.ctx.Create<SimpleClass>());
        }

        [Test]
        public void TestCustomSetter()
        {
            this.ctx.For<string>()
                .Use(() => "str");

            var obj = this.ctx.Create<SimpleClass>();
            Assert.AreEqual("str", obj.Prop1);
            Assert.AreEqual("str", obj.Prop2);
        }

        [Test]
        public void TestCustomProperty()
        {
            this.ctx.For<string>()
                .Use(() => "str");

            this.ctx.For<SimpleClass>()
                .For(sc => sc.Prop1)
                .Use(_ => "str1")
                .For(sc => sc.Prop2)
                .Use(_ => "str2");

            var obj = this.ctx.Create<SimpleClass>();
            Assert.AreEqual("str1", obj.Prop1);
            Assert.AreEqual("str2", obj.Prop2);
        }

        [Test]
        public void TestWithoutType()
        {
            this.ctx.For<string>()
                .Skip();

            var obj = this.ctx.Create<SimpleClass>();
            Assert.IsNull(obj.Prop1);
            Assert.IsNull(obj.Prop2);
        }

        [Test]
        public void TestWithoutProperty()
        {
            this.ctx.For<SimpleClass>()
                .For(sc => sc.Prop2)
                .Skip();

            var obj = this.ctx.Create<SimpleClass>();
            Assert.IsNotEmpty(obj.Prop1);
            Assert.IsNull(obj.Prop2);
        }

        [Test]
        public void TestConstructor()
        {
            var result = this.ctx.Create<TestClass3>();

            Assert.IsNotNull(result.TestClass1);
            Assert.AreNotEqual(Guid.Empty, result.Id);
        }

        [Test]
        public void TestInclude()
        {
            this.ctx.For<TestClass2>()
                .For(tc => tc.Class1)
                .Include(2);

            var obj = this.ctx.Create<TestClass2>();
            Assert.AreEqual(2, obj.Class1.Count);
        }

        [Test]
        public void Test11Relation()
        {
            this.ctx.For<TestClass9>()
                .For(tc => tc.Id)
                .Has11Relation<TestClass10>(tc => tc.Id);

            var obj = this.ctx.Create<TestClass9>();
            Assert.AreEqual(2, this.ctx.GetObjects().Count());
            Assert.AreNotEqual(Guid.Empty, obj.Id);
            Assert.AreNotEqual(Guid.Empty, obj.Class10.Id);
            Assert.AreEqual(obj.Id, obj.Class10.Id);
        }

        [Test]
        public void TestCreateString()
        {
            for (int i = 0; i < 30; ++i)
            {
                var s = this.ctx.CreateString(i);
                Assert.AreEqual(i, s.Length);
            }
        }

        [Test]
        public void TestCreateStringPrefix()
        {
            for (int i = 0; i < 30; ++i)
            {
                var s = this.ctx.CreateString("xx", i);
                Assert.AreEqual(i, s.Length);
            }
        }

        [Test]
        public void TestSkipConstructorParameter()
        {
            this.ctx.For<int>()
                .ForConstructor()
                .Skip();

            var obj = this.ctx.Create<ClassWithConstructor>();

            Assert.AreEqual(default(int), obj.Id);
        }

        [Test]
        public void TestBuildContext()
        {
            var item = this.ctx.Build<SimpleClass>()
                .With(x => x.Prop1, "foo")
                .Create();

            Assert.AreEqual("foo", item.Prop1);
            Assert.AreNotEqual("foo", item.Prop2);
        }
    }
}

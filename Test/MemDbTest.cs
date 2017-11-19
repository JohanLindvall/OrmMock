using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OrmMock;

namespace Test
{
    [TestFixture]
    public class MemDbTest
    {
        public class TestClass1
        {
            public Guid Id { get; set; }
        }

        public class TestClass1B
        {
            public Guid Id { get; set; }
        }

        public class TestClass2
        {
            public int Key1 { get; set; }

            public int Key2 { get; set; }
        }

        public class TestClass3
        {
            public TestClass3()
            {
                this.List = new List<TestClass3>();
                this.List2 = new List<TestClass4>();
            }

            public int Id { get; set; }

            public IList<TestClass3> List { get; }

            public IList<TestClass4> List2 { get; }

            public TestClass3 Ref { get; set; }

            public TestClass4 Ref2 { get; set; }
        }


        public class TestClass4
        {
            public TestClass4()
            {
                this.List = new List<TestClass3>();
                this.List2 = new List<TestClass4>();
            }

            public int Id { get; set; }

            public List<TestClass3> List { get; }

            public IList<TestClass4> List2 { get; }

            public TestClass3 Ref { get; set; }

            public TestClass4 Ref2 { get; set; }
        }

        [Test]
        public void TestAdd()
        {
            var db = new MemDb();
            var obj = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(obj);
            Assert.AreEqual(1, db.Count<TestClass1>());
        }

        [Test]
        public void TestAddTwiceSameKey()
        {
            var db = new MemDb();
            var obj = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(obj);
            obj = new TestClass1 { Id = obj.Id };
            Assert.Throws<InvalidOperationException>(() => db.Add(obj));
            Assert.AreEqual(1, db.Count<TestClass1>());
        }

        [Test]
        public void TestCount()
        {
            var db = new MemDb();
            db.Add(new TestClass1 { Id = Guid.NewGuid() });
            db.Add(new TestClass1 { Id = Guid.NewGuid() });
            db.Add(new TestClass1B { Id = Guid.NewGuid() });
            db.Add(new TestClass1B { Id = Guid.NewGuid() });
            db.Add(new TestClass1B { Id = Guid.NewGuid() });

            Assert.AreEqual(2, db.Count<TestClass1>());
            Assert.AreEqual(3, db.Count<TestClass1B>());
            Assert.AreEqual(5, db.Count());
        }

        [Test]
        public void TestAddUnknownKey()
        {
            var db = new MemDb();
            var obj = new TestClass1 { Id = Guid.NewGuid() };
            Assert.Throws<InvalidOperationException>(() => db.Add(new TestClass2()));
            Assert.AreEqual(0, db.Count());
        }

        [Test]
        public void TestRegisterKey()
        {
            var db = new MemDb();
            db.RegisterKey<TestClass2>(i => i.Key1);
        }

        [Test]
        public void TestRegisterKeyFail()
        {
            var db = new MemDb();
            Assert.Throws<InvalidOperationException>(() => db.RegisterKey<TestClass2>(i => i.Key1 + 1));
        }

        [Test]
        public void TestRegisterKeyGet()
        {
            var db = new MemDb();
            db.RegisterKey<TestClass2>(i => i.Key1);
            db.Add(new TestClass2 { Key1 = 23, Key2 = 45 });
            var fetched = db.Get<TestClass2>(23);
            Assert.AreEqual(23, fetched.Key1);
            Assert.AreEqual(45, fetched.Key2);
        }

        [Test]
        public void TestRegisterKeyComposite()
        {
            var db = new MemDb();
            db.RegisterKey<TestClass2>(i => new { i.Key2, i.Key1 });
        }

        [Test]
        public void TestRegisterKeyCompositeFail()
        {
            var db = new MemDb();
            Assert.Throws<InvalidOperationException>(() => db.RegisterKey<TestClass2>(i => new { foo = i.Key1 + 1 }));
        }

        [Test]
        public void TestRegisterKeyGetComposite()
        {
            var db = new MemDb();
            db.RegisterKey<TestClass2>(i => new { i.Key2, i.Key1 });
            db.Add(new TestClass2 { Key1 = 23, Key2 = 45 });
            var fetched = db.Get<TestClass2>(45, 23);
            Assert.AreEqual(23, fetched.Key1);
            Assert.AreEqual(45, fetched.Key2);
        }

        [Test]
        public void TestRegisterKeyGetCompositeFail()
        {
            var db = new MemDb();
            db.RegisterKey<TestClass2>(i => new { i.Key2, i.Key1 });
            db.Add(new TestClass2 { Key1 = 23, Key2 = 45 });
            var fetched = db.Get<TestClass2>(23, 45);
            Assert.IsNull(fetched);
        }

        [Test]
        public void TestGet()
        {
            var db = new MemDb();
            var stored = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(stored);
            var fetched = db.Get<TestClass1>(stored.Id);
            Assert.AreSame(stored, fetched);
        }

        [Test]
        public void TestQueryable()
        {
            var db = new MemDb();
            var stored = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(stored);
            db.Queryable<TestClass1>().Single(s => s.Id == stored.Id);
        }

        [Test]
        public void TestQueryableEmpty()
        {
            var db = new MemDb();
            Assert.AreEqual(0, db.Queryable<TestClass1>().Count());
        }

        [Test]
        public void TestRemove()
        {
            var db = new MemDb();
            var stored = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(stored);
            var remove = new TestClass1 { Id = stored.Id };
            Assert.IsTrue(db.Remove(remove));
            Assert.AreEqual(0, db.Count());
        }

        [Test]
        public void TestRemoveMissing()
        {
            var db = new MemDb();
            Assert.IsFalse(db.Remove(new TestClass1 { Id = Guid.NewGuid() }));
        }

        [Test]
        public void TestAddChildren()
        {
            var db = new MemDb();
            var stored = new TestClass4
            {
                Id = 1,
            };
            stored.List.Add(new TestClass3
            {
                Id = 2
            });
            stored.List2.Add(new TestClass4
            {
                Id = 3
            });

            db.Add(stored);

            Assert.IsNotNull(db.Get<TestClass4>(1));
            Assert.IsNotNull(db.Get<TestClass3>(2));
            Assert.IsNotNull(db.Get<TestClass4>(3));
        }

        [Test]
        public void TestAddChildrenWithSameKey()
        {
            var db = new MemDb();
            var stored = new TestClass4
            {
                Id = 1,
            };
            stored.List.Add(new TestClass3
            {
                Id = 2
            });
            stored.List.Add(new TestClass3
            {
                Id = 2
            });

            db.Add(stored);

            Assert.IsNotNull(db.Get<TestClass4>(1));
            Assert.IsNotNull(db.Get<TestClass3>(2));
            Assert.AreEqual(2, db.Count());
        }

        [Test]
        public void TestAddChildrenTwice()
        {
            var db = new MemDb();
            var stored = new TestClass4
            {
                Id = 1,
            };
            var added = new TestClass3 { Id = 2 };
            stored.List.Add(added);
            stored.List.Add(added);

            db.Add(stored);

            Assert.IsNotNull(db.Get<TestClass4>(1));
            Assert.IsNotNull(db.Get<TestClass3>(2));
            Assert.AreEqual(2, db.Count());
        }

        [Test]
        public void TestAddReference()
        {
            var db = new MemDb();
            var stored = new TestClass4
            {
                Id = 1,
                Ref = new TestClass3
                {
                    Id = 2,
                    Ref2 = new TestClass4
                    {
                        Id = 3
                    }
                }
            };

            db.Add(stored);

            Assert.IsNotNull(db.Get<TestClass4>(1));
            Assert.IsNotNull(db.Get<TestClass3>(2));
            Assert.IsNotNull(db.Get<TestClass4>(3));
            Assert.AreEqual(3, db.Count());
        }

        [Test]
        public void TestAddFilter()
        {
            var db = new MemDb { IncludeFilter = type => type == typeof(TestClass4) };
            var stored = new TestClass4
            {
                Id = 1,
                Ref = new TestClass3
                {
                    Id = 2,
                    Ref2 = new TestClass4
                    {
                        Id = 3
                    }
                }
            };

            db.Add(stored);

            Assert.IsNotNull(db.Get<TestClass4>(1));
            Assert.AreEqual(1, db.Count());
        }
    }
}

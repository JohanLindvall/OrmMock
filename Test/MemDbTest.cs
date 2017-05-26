using System;
using System.Collections.Generic;
using System.Linq;
using DataGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
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

        [TestMethod]
        public void TestAdd()
        {
            var db = new MemDb();
            var obj = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(obj);
            Assert.AreEqual(1, db.Count<TestClass1>());
        }

        [TestMethod]
        public void TestAddTwiceSameKey()
        {
            var db = new MemDb();
            var obj = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(obj);
            obj = new TestClass1 { Id = obj.Id };
            Assert.ThrowsException<InvalidOperationException>(() => db.Add(obj));
            Assert.AreEqual(1, db.Count<TestClass1>());
        }

        [TestMethod]
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

        [TestMethod]
        public void TestAddUnknownKey()
        {
            var db = new MemDb();
            var obj = new TestClass1 { Id = Guid.NewGuid() };
            Assert.ThrowsException<InvalidOperationException>(() => db.Add(new TestClass2()));
            Assert.AreEqual(0, db.Count());
        }

        [TestMethod]
        public void TestRegisterKey()
        {
            var db = new MemDb();
            db.RegisterKey<TestClass2>(i => i.Key1);
        }

        [TestMethod]
        public void TestRegisterKeyFail()
        {
            var db = new MemDb();
            Assert.ThrowsException<InvalidOperationException>(() => db.RegisterKey<TestClass2>(i => i.Key1 + 1));
        }

        [TestMethod]
        public void TestRegisterKeyGet()
        {
            var db = new MemDb();
            db.RegisterKey<TestClass2>(i => i.Key1);
            db.Add(new TestClass2 { Key1 = 23, Key2 = 45 });
            var fetched = db.Get<TestClass2>(23);
            Assert.AreEqual(23, fetched.Key1);
            Assert.AreEqual(45, fetched.Key2);
        }

        [TestMethod]
        public void TestRegisterKeyComposite()
        {
            var db = new MemDb();
            db.RegisterKey<TestClass2>(i => new { i.Key2, i.Key1 });
        }

        [TestMethod]
        public void TestRegisterKeyCompositeFail()
        {
            var db = new MemDb();
            Assert.ThrowsException<InvalidOperationException>(() => db.RegisterKey<TestClass2>(i => new { foo = i.Key1 + 1 }));
        }

        [TestMethod]
        public void TestRegisterKeyGetComposite()
        {
            var db = new MemDb();
            db.RegisterKey<TestClass2>(i => new { i.Key2, i.Key1 });
            db.Add(new TestClass2 { Key1 = 23, Key2 = 45 });
            var fetched = db.Get<TestClass2>(45, 23);
            Assert.AreEqual(23, fetched.Key1);
            Assert.AreEqual(45, fetched.Key2);
        }

        [TestMethod]
        public void TestRegisterKeyGetCompositeFail()
        {
            var db = new MemDb();
            db.RegisterKey<TestClass2>(i => new { i.Key2, i.Key1 });
            db.Add(new TestClass2 { Key1 = 23, Key2 = 45 });
            var fetched = db.Get<TestClass2>(23, 45);
            Assert.IsNull(fetched);
        }

        [TestMethod]
        public void TestGet()
        {
            var db = new MemDb();
            var stored = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(stored);
            var fetched = db.Get<TestClass1>(stored.Id);
            Assert.AreSame(stored, fetched);
        }

        [TestMethod]
        public void TestQueryable()
        {
            var db = new MemDb();
            var stored = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(stored);
            db.Queryable<TestClass1>().Single(s => s.Id == stored.Id);
        }

        [TestMethod]
        public void TestQueryableEmpty()
        {
            var db = new MemDb();
            Assert.AreEqual(0, db.Queryable<TestClass1>().Count());
        }

        [TestMethod]
        public void TestRemove()
        {
            var db = new MemDb();
            var stored = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(stored);
            var remove = new TestClass1 { Id = stored.Id };
            Assert.IsTrue(db.Remove(remove));
            Assert.AreEqual(0, db.Count());
        }

        [TestMethod]
        public void TestRemoveMissing()
        {
            var db = new MemDb();
            Assert.IsFalse(db.Remove(new TestClass1 { Id = Guid.NewGuid() }));
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

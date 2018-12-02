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

namespace Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    using OrmMock.MemDb;
    using OrmMock.Shared;

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

        public class TestClass5
        {
            public long Id { get; set; }

            public int Auto { get; set; }
        }

        private MemDb db;

        [SetUp]
        public void Setup()
        {
            this.db = new MemDb();
            this.db.Relations.RegisterNullForeignKeys<TestClass4, TestClass3>();
            this.db.Relations.RegisterNullForeignKeys<TestClass3, TestClass4>();
            this.db.Relations.RegisterNullForeignKeys<TestClass3, TestClass3>();
            this.db.Relations.RegisterNullForeignKeys<TestClass4, TestClass4>();
        }

        [Test]
        public void TestAdd()
        {
            var obj = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(obj);
            db.Commit();
            Assert.AreEqual(1, db.Count<TestClass1>());
        }

        [Test]
        public void TestAddTwiceSameKey()
        {
            var obj = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(obj);
            obj = new TestClass1 { Id = obj.Id };
            db.Add(obj);
            db.Commit();
        }

        [Test]
        public void TestCount()
        {
            db.Add(new TestClass1 { Id = Guid.NewGuid() });
            db.Add(new TestClass1 { Id = Guid.NewGuid() });
            db.Add(new TestClass1B { Id = Guid.NewGuid() });
            db.Add(new TestClass1B { Id = Guid.NewGuid() });
            db.Add(new TestClass1B { Id = Guid.NewGuid() });

            db.Commit();

            Assert.AreEqual(2, db.Count<TestClass1>());
            Assert.AreEqual(3, db.Count<TestClass1B>());
            Assert.AreEqual(5, db.Count());
        }

        [Test]
        public void TestAddUnknownKey()
        {
            db.Add(new TestClass2());
            db.Commit();
            Assert.AreEqual(1, db.Count());
        }

        [Test]
        public void TestRegisterKey()
        {
            db.Relations.RegisterPrimaryKey<TestClass2>(i => i.Key1);
        }

        [Test]
        public void TestRegisterKeyFail()
        {
            Assert.Throws<InvalidOperationException>(() => db.Relations.RegisterPrimaryKey<TestClass2>(i => i.Key1 + 1));
        }

        [Test]
        public void TestRegisterKeyGet()
        {
            db.Relations.RegisterPrimaryKey<TestClass2>(i => i.Key1);
            db.Add(new TestClass2 { Key1 = 23, Key2 = 45 });
            db.Commit();
            var fetched = db.Get<TestClass2>(new KeyHolder(23));
            Assert.AreEqual(23, fetched.Key1);
            Assert.AreEqual(45, fetched.Key2);
        }

        [Test]
        public void TestRegisterKeyComposite()
        {
            db.Relations.RegisterPrimaryKey<TestClass2>(i => new { i.Key2, i.Key1 });
        }

        [Test]
        public void TestRegisterKeyCompositeFail()
        {
            Assert.Throws<InvalidOperationException>(() => db.Relations.RegisterPrimaryKey<TestClass2>(i => new { foo = i.Key1 + 1 }));
        }

        [Test]
        public void TestRegisterKeyGetComposite()
        {
            db.Relations.RegisterPrimaryKey<TestClass2>(i => new { i.Key2, i.Key1 });
            db.Add(new TestClass2 { Key1 = 23, Key2 = 45 });
            db.Commit();
            var fetched = db.Get<TestClass2>(new KeyHolder(45, 23));
            Assert.AreEqual(23, fetched.Key1);
            Assert.AreEqual(45, fetched.Key2);
        }

        [Test]
        public void TestRegisterKeyGetCompositeFail()
        {
            db.Relations.RegisterPrimaryKey<TestClass2>(i => new { i.Key2, i.Key1 });
            db.Add(new TestClass2 { Key1 = 23, Key2 = 45 });
            db.Commit();
            var fetched = db.Get<TestClass2>(new KeyHolder(23, 45));
            Assert.IsNull(fetched);
        }

        [Test]
        public void TestGet()
        {
            var stored = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(stored);
            db.Commit();
            var fetched = db.Get<TestClass1>(new KeyHolder(stored.Id));
            Assert.AreSame(stored, fetched);
        }

        [Test]
        public void TestGetEnumerable()
        {
            var stored = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(stored);
            db.Commit();
            db.Get<TestClass1>().Single(s => s.Id == stored.Id);
        }

        [Test]
        public void TestGetEnumerableEmpty()
        {
            Assert.AreEqual(0, db.Get<TestClass1>().Count());
        }

        [Test]
        public void TestRemove()
        {
            var stored = new TestClass1 { Id = Guid.NewGuid() };
            db.Add(stored);
            db.Commit();
            var remove = new TestClass1 { Id = stored.Id };
            Assert.IsTrue(db.Remove(remove));
            Assert.AreEqual(0, db.Count());
        }

        [Test]
        public void TestRemoveMissing()
        {
            Assert.IsFalse(db.Remove(new TestClass1 { Id = Guid.NewGuid() }));
        }

        [Test]
        public void TestAddChildren()
        {
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
            db.Commit();

            Assert.IsNotNull(db.Get<TestClass4>(new KeyHolder(1)));
            Assert.IsNotNull(db.Get<TestClass3>(new KeyHolder(2)));
            Assert.IsNotNull(db.Get<TestClass4>(new KeyHolder(3)));
        }

        [Test]
        public void TestAddChildrenTwice()
        {
            var stored = new TestClass4
            {
                Id = 1,
            };
            var added = new TestClass3 { Id = 2 };
            stored.List.Add(added);
            stored.List.Add(added);

            db.Add(stored);
            db.Commit();

            Assert.IsNotNull(db.Get<TestClass4>(new KeyHolder(1)));
            Assert.IsNotNull(db.Get<TestClass3>(new KeyHolder(2)));
            Assert.AreEqual(2, db.Count());
        }

        [Test]
        public void TestAddReference()
        {
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
            db.Commit();

            Assert.IsNotNull(db.Get<TestClass4>(new KeyHolder(1)));
            Assert.IsNotNull(db.Get<TestClass3>(new KeyHolder(2)));
            Assert.IsNotNull(db.Get<TestClass4>(new KeyHolder(3)));
            Assert.AreEqual(3, db.Count());
        }

        [Test]
        public void TestAutoIncrement()
        {
            db.RegisterAutoIncrement<TestClass5>(i => i.Auto);
            db.Add(new TestClass5
            {
                Id = 123
            });
            db.Add(new TestClass5
            {
                Id = 1234
            });
            db.Commit();

            var s1 = db.Get<TestClass5>(new KeyHolder((long)123));
            var s2 = db.Get<TestClass5>(new KeyHolder((long)1234));

            Assert.AreEqual(1, s1.Auto);
            Assert.AreEqual(2, s2.Auto);
        }
    }
}

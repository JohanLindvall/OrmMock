using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using OrmMock.DataGenerator;
using OrmMock.MemDb;

namespace Test
{
    [TestFixture]
    class MemDbPerf
    {

        private MemDb memDb;

        private object root;

        [SetUp]
        public void Setup()
        {
            var dg = new DataGenerator();
            dg.For<Class1>().Include(c => c.Class2);
            dg.For<Class2>().Include(c => c.Class3);
            dg.For<Class3>().Include(c => c.Class4);
            dg.For<Class4>().Include(c => c.Class5);
            dg.For<Class5>().Include(c => c.Class6);
            dg.For<Class6>().Include(c => c.Class7);
            dg.For<Class7>().Include(c => c.Class8);
            dg.ObjectLimit = 100000;
            this.root = dg.Create<Class1>();

            this.memDb = new MemDb();
        }

        // [Test]
        public void Perf()
        {
            for (var i = 0; i < 1000; ++i)
            {
                this.memDb.Reset();

                this.memDb.Add(this.root);

                this.memDb.Commit();
            }
        }

        public class Class1
        {
            public Guid Id { get; set; }

            public ICollection<Class2> Class2 { get; set; }
        }

        public class Class2
        {
            public Guid Id { get; set; }

            public Guid Class1Id { get; set; }

            public Class1 Class1 { get; set; }

            public ICollection<Class3> Class3 { get; set; }
        }

        public class Class3
        {
            public Guid Id { get; set; }

            public Guid Class2Id { get; set; }

            public Class2 Class2 { get; set; }

            public ICollection<Class4> Class4 { get; set; }
        }


        public class Class4
        {
            public Guid Id { get; set; }

            public Guid Class3Id { get; set; }

            public Class3 Class3 { get; set; }

            public ICollection<Class5> Class5 { get; set; }

        }

        public class Class5
        {
            public Guid Id { get; set; }

            public Guid Class4Id { get; set; }

            public Class4 Class4 { get; set; }

            public ICollection<Class6> Class6 { get; set; }
        }

        public class Class6
        {
            public Guid Id { get; set; }

            public Guid Class5Id { get; set; }

            public Class5 Class5 { get; set; }

            public ICollection<Class7> Class7 { get; set; }
        }

        public class Class7
        {
            public Guid Id { get; set; }


            public Guid Class6Id { get; set; }

            public Class6 Class6 { get; set; }

            public ICollection<Class8> Class8 { get; set; }
        }

        public class Class8
        {
            public Guid Id { get; set; }

            public Guid Class7Id { get; set; }

            public Class7 Class7 { get; set; }
        }
    }
}

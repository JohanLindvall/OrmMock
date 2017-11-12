using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class GeneratorTest
    {
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

        [TestMethod]
        public void Test()
        {
            var gen = new Generator();
            var ctx = gen.CreateContext();

            var result = ctx.Create<TestClass1>();

            var result2 = ctx.Create<TestClass1>();
        }
    }
}

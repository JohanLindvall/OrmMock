using System;
using System.Linq;
using DataGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class ExpressionUtilityTest
    {
        public class TestClass
        {
            public int Id { get; set; }

            public string Str { get; set; }
        }

        [TestMethod]
        public void TestMemberExpression()
        {
            var ids = ExpressionUtility.GetPropertyInfo<TestClass, int>(tc => tc.Id);
            Assert.AreSame(typeof(TestClass).GetProperty(nameof(TestClass.Id)), ids.Single());
        }

        [TestMethod]
        public void TestNewExpression()
        {
            var ids = ExpressionUtility.GetPropertyInfo<TestClass, object>(tc => new { tc.Id, tc.Str });
            Assert.AreSame(typeof(TestClass).GetProperty(nameof(TestClass.Id)), ids[0]);
            Assert.AreSame(typeof(TestClass).GetProperty(nameof(TestClass.Str)), ids[1]);
            Assert.AreEqual(2, ids.Length);
        }

        [TestMethod]
        public void TestUnaryExpression()
        {
            var ids = ExpressionUtility.GetPropertyInfo<TestClass, object>(tc => (object)tc.Id);
            Assert.AreSame(typeof(TestClass).GetProperty(nameof(TestClass.Id)), ids.Single());
        }

        [TestMethod]
        public void TestUnsupported()
        {
            Assert.ThrowsException<InvalidOperationException>(() => ExpressionUtility.GetPropertyInfo<TestClass, object>(tc => "foo"));
        }
    }
}

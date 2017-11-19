using System;
using System.Linq;
using NUnit.Framework;
using OrmMock;

namespace Test
{
    [TestFixture]
    public class ExpressionUtilityTest
    {
        public class TestClass
        {
            public int Id { get; set; }

            public string Str { get; set; }
        }

        [Test]
        public void TestMemberExpression()
        {
            var ids = ExpressionUtility.GetPropertyInfo<TestClass, int>(tc => tc.Id);
            Assert.AreSame(typeof(TestClass).GetProperty(nameof(TestClass.Id)), ids.Single());
        }

        [Test]
        public void TestNewExpression()
        {
            var ids = ExpressionUtility.GetPropertyInfo<TestClass, object>(tc => new { tc.Id, tc.Str });
            Assert.AreSame(typeof(TestClass).GetProperty(nameof(TestClass.Id)), ids[0]);
            Assert.AreSame(typeof(TestClass).GetProperty(nameof(TestClass.Str)), ids[1]);
            Assert.AreEqual(2, ids.Length);
        }

        [Test]
        public void TestUnaryExpression()
        {
            var ids = ExpressionUtility.GetPropertyInfo<TestClass, object>(tc => (object)tc.Id);
            Assert.AreSame(typeof(TestClass).GetProperty(nameof(TestClass.Id)), ids.Single());
        }

        [Test]
        public void TestUnsupported()
        {
            Assert.Throws<InvalidOperationException>(() => ExpressionUtility.GetPropertyInfo<TestClass, object>(tc => "foo"));
        }
    }
}

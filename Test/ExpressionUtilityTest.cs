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
    using System.Linq;
    using NUnit.Framework;
    using OrmMock;

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

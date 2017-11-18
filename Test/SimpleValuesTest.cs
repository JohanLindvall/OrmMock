using System;
using DataGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class SimpleValuesTest
    {
        private ObjectContext generator;

        [TestInitialize]
        public void Setup()
        {
            this.generator = new Generator().CreateContext();
        }

        public T GetValue<T>()
        {
            return this.generator.Create<T>();
        }

        [TestMethod]
        public void TestBool()
        {
            var val = this.GetValue<bool>();
            Assert.IsTrue(val || !val, $@"Bad value {val}");
        }

        [TestMethod]
        public void TestByte()
        {
            var val = this.GetValue<byte>();
            Assert.IsTrue((int)val >= byte.MinValue && (int)val <= byte.MaxValue, $@"Bad value {val}");
        }

        [TestMethod]
        public void TestShort()
        {
            var val = this.GetValue<short>();
            Assert.IsTrue((int)val >= short.MinValue && (int)val <= short.MaxValue, $@"Bad value {val}");
        }

        [TestMethod]
        public void TestUShort()
        {
            var val = this.GetValue<ushort>();
            Assert.IsTrue((int)val >= ushort.MinValue && (int)val <= ushort.MaxValue, $@"Bad value {val}");
        }

        [TestMethod]
        public void TestInt()
        {
            var val = this.GetValue<int>();
            Assert.IsTrue((long)val >= int.MinValue && (long)val <= int.MaxValue, $@"Bad value {val}");
        }

        [TestMethod]
        public void TestUInt()
        {
            var val = this.GetValue<uint>();
            Assert.IsTrue((long)val >= uint.MinValue && (long)val <= uint.MaxValue, $@"Bad value {val}");
        }

        [TestMethod]
        public void TestLong()
        {
            var val = this.GetValue<long>();
            Assert.IsTrue((decimal)val >= long.MinValue && (decimal)val <= long.MaxValue, $@"Bad value {val}");
        }

        [TestMethod]
        public void TestULong()
        {
            var val = this.GetValue<ulong>();
            Assert.IsTrue((decimal)val >= ulong.MinValue && (decimal)val <= ulong.MaxValue, $@"Bad value {val}");
        }

        [TestMethod]
        public void TestFloat()
        {
            var val = this.GetValue<float>();
            Assert.IsTrue((double)val >= float.MinValue && (double)val <= float.MaxValue, $@"Bad value {val}");
        }

        [TestMethod]
        public void TestDouble()
        {
            var val = this.GetValue<double>();
            Assert.IsTrue(val >= double.MinValue && val <= double.MaxValue, $@"Bad value {val}");
        }

        [TestMethod]
        public void TestDecimal()
        {
            var val = this.GetValue<decimal>();
            Assert.IsTrue(val > decimal.MinValue && val < decimal.MaxValue, $@"Bad value {val}");
        }

        [TestMethod]
        public void TestDateTimeOffset()
        {
            var val = this.GetValue<DateTimeOffset>();

            Assert.IsTrue(val >= DateTimeOffset.MinValue && val <= DateTimeOffset.MaxValue, $@"Bad value {val}");
        }

        public enum MyEnum
        {
            First,
            Second,
            Third
        }

        [TestMethod]
        public void TestEnum()
        {
            var val = this.GetValue<MyEnum>();

            Assert.IsTrue((int)val >= (int)MyEnum.First && (int)val <= (int)MyEnum.Third, $@"Bad value {val}");
        }

        [TestMethod]
        public void TestGuid()
        {
            var val = this.GetValue<Guid>();

            Assert.IsTrue(val != Guid.Empty, $@"Bad value {val}");
        }

        [TestMethod]
        public void TestNullable()
        {
            var val = this.GetValue<short?>();

            Assert.IsTrue(!val.HasValue || (val >= short.MinValue && val <= short.MaxValue), $@"Bad value {val}");
        }


        [TestMethod]
        public void TestString()
        {
            var val = this.GetValue<string>();
            Assert.IsTrue(!string.IsNullOrEmpty(val));
        }
    }
}

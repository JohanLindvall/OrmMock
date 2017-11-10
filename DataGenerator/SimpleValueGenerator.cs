// Copyright(c) 2017 Johan Lindvall
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

namespace DataGenerator
{
    using System;
    using System.Linq;

    /// <summary>
    /// Defines a data generator for simple values (builtins)
    /// </summary>
    public class SimpleValueGenerator
    {
        /// <summary>
        /// Holds the random generator used by this instance.
        /// </summary>
        public Random Random { get; } = new Random();

        /// <summary>
        /// Tries to create a value of type t.
        /// </summary>
        /// <param name="t">The type of the value to create.</param>
        /// <param name="propertyName">The name of the property to set. Currently used as a prefix for string values.</param>
        /// <param name="result">The created value.</param>
        /// <returns>True if a value was created, false otherwise.</returns>
        public bool TryCreateValue(Type t, string propertyName, out object result)
        {
            if (t.IsClass && t != typeof(string))
            {
                result = null;
                return false;
            }

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (this.Random.Next(0, 1) == 0)
                {
                    result = null;
                    return true;
                }
                else
                {
                    t = Nullable.GetUnderlyingType(t);
                }
            }

            if (t.IsEnum)
            {
                var values = Enum.GetValues(t);
                result = values.GetValue(this.Random.Next(0, values.Length - 1));
            }
            else if (t == typeof(string))
            {
                result = propertyName + BitConverter.ToString(Enumerable.Range(0, 20).Select(_ => (byte)this.Random.Next(0, 255)).ToArray()).Replace("-", string.Empty);
            }
            else if (t == typeof(byte))
            {
                result = (byte)this.Random.Next(byte.MinValue, byte.MaxValue);
            }
            else if (t == typeof(short))
            {
                result = (short)this.Random.Next(short.MinValue, short.MaxValue);
            }
            else if (t == typeof(ushort))
            {
                result = (ushort)this.Random.Next(ushort.MinValue, ushort.MaxValue);
            }
            else if (t == typeof(int))
            {
                result = this.Random.Next(int.MinValue, int.MaxValue);
            }
            else if (t == typeof(uint))
            {
                result = (uint)this.Random.Next(int.MinValue, int.MaxValue);
            }
            else if (t == typeof(long))
            {
                result = BitConverter.ToInt64(Enumerable.Range(0, 16).Select(_ => (byte)this.Random.Next(0, 255)).ToArray(), 0);
            }
            else if (t == typeof(ulong))
            {
                result = BitConverter.ToUInt64(Enumerable.Range(0, 16).Select(_ => (byte)this.Random.Next(0, 255)).ToArray(), 0);
            }
            else if (t == typeof(double))
            {
                result = (this.Random.NextDouble() - 0.5) * double.MaxValue;
            }
            else if (t == typeof(float))
            {
                result = (float)(this.Random.NextDouble() - 0.5) * float.MaxValue;
            }
            else if (t == typeof(decimal))
            {
                result = (decimal)(this.Random.NextDouble() - 0.5) * 1e2m; // TODO look into overflows?
            }
            else if (t == typeof(bool))
            {
                result = this.Random.Next(0, 1) == 1;
            }
            else if (t == typeof(Guid))
            {
                result = new Guid(Enumerable.Range(0, 16).Select(_ => (byte)this.Random.Next(0, 255)).ToArray());
            }
            else if (t == typeof(DateTimeOffset))
            {
                result = DateTimeOffset.Now + TimeSpan.FromMilliseconds((this.Random.NextDouble() - 0.5) * 62e9);
            }
            else
            {
                result = null;
                return false;
            }

            return true;
        }
    }
}

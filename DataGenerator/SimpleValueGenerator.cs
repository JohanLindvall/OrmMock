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
            var creator = ValueCreator(t);
            if (creator == null)
            {
                result = null;
                return false;
            }
            else
            {
                result = creator(propertyName);
                return true;
            }
        }

        /// <summary>
        /// Returns a value creating delegate for the given type.
        /// </summary>
        /// <param name="t">The type for which to create values.</param>
        /// <returns></returns>
        public Func<string, object> ValueCreator(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                t = Nullable.GetUnderlyingType(t);
                var inner = ValueCreator(t);
                if (inner == null)
                {
                    return null;
                }

                return s =>
                {
                    if (this.Random.Next(0, 1) == 0)
                    {
                        return null;
                    }
                    else
                    {
                        return inner(s);
                    }
                };
            }

            if (t.IsEnum)
            {
                var values = Enum.GetValues(t);
                return _ => values.GetValue(this.Random.Next(0, values.Length - 1));
            }
            else if (t == typeof(string))
            {
                return s =>
                {
                    var rnd = new byte[21];
                    this.Random.NextBytes(rnd);
                    return s + Convert.ToBase64String(rnd);
                };
            }
            else if (t == typeof(byte))
            {
                return _ => (byte)this.Random.Next(byte.MinValue, byte.MaxValue);
            }
            else if (t == typeof(short))
            {
                return _ => (short)this.Random.Next(short.MinValue, short.MaxValue);
            }
            else if (t == typeof(ushort))
            {
                return _ => (ushort)this.Random.Next(ushort.MinValue, ushort.MaxValue);
            }
            else if (t == typeof(int))
            {
                return _ => this.Random.Next(int.MinValue, int.MaxValue);
            }
            else if (t == typeof(uint))
            {
                return _ => (uint)this.Random.Next(int.MinValue, int.MaxValue);
            }
            else if (t == typeof(long))
            {
                return _ =>
                {
                    var rnd = new byte[8];
                    this.Random.NextBytes(rnd);
                    return BitConverter.ToInt64(rnd, 0);
                };
            }
            else if (t == typeof(ulong))
            {
                return _ =>
                {
                    var rnd = new byte[8];
                    this.Random.NextBytes(rnd);
                    return BitConverter.ToUInt64(rnd, 0);
                };
            }
            else if (t == typeof(double))
            {
                return _ => (this.Random.NextDouble() - 0.5) * double.MaxValue;
            }
            else if (t == typeof(float))
            {
                return _ => (float)(this.Random.NextDouble() - 0.5) * float.MaxValue;
            }
            else if (t == typeof(decimal))
            {
                return _ => (decimal)(this.Random.NextDouble() - 0.5) * 1e2m; // TODO look into overflows?
            }
            else if (t == typeof(bool))
            {
                return _ => this.Random.Next(0, 1) == 1;
            }
            else if (t == typeof(Guid))
            {
                return s =>
                {
                    var rnd = new byte[16];
                    this.Random.NextBytes(rnd);
                    return new Guid(rnd);
                };
            }
            else if (t == typeof(DateTimeOffset))
            {
                return _ => DateTimeOffset.Now + TimeSpan.FromMilliseconds((this.Random.NextDouble() - 0.5) * 62e9);
            }
            return null;
        }
    }
}

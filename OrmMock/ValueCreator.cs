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

namespace OrmMock
{
    using System;

    public class ValueCreator
    {
        /// <summary>
        /// Holds the random generator used by this instance.
        /// </summary>
        private readonly Random random = new Random();

        public Func<string, object> Get(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                t = Nullable.GetUnderlyingType(t);
                var inner = this.Get(t);
                if (inner == null)
                {
                    return null;
                }

                return s =>
                {
                    if (this.random.Next(0, 2) == 0)
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
                return _ => values.GetValue(this.random.Next(0, values.Length));
            }
            else if (t == typeof(string))
            {
                return s => this.CreateString(s, s.Length + 24);
            }
            else if (t == typeof(byte))
            {
                return _ => (byte)this.random.Next(byte.MinValue, byte.MaxValue + 1);
            }
            else if (t == typeof(short))
            {
                return _ => (short)this.random.Next(short.MinValue, short.MaxValue + 1);
            }
            else if (t == typeof(ushort))
            {
                return _ => (ushort)this.random.Next(ushort.MinValue, ushort.MaxValue + 1);
            }
            else if (t == typeof(int))
            {
                return _ => this.random.Next(int.MinValue, int.MaxValue); // TODO capped at maxValue - 1
            }
            else if (t == typeof(uint))
            {
                return _ => (uint)this.random.Next(int.MinValue, int.MaxValue); // TODO capped at maxValue - 1
            }
            else if (t == typeof(long))
            {
                return _ =>
                {
                    var rnd = new byte[8];
                    this.random.NextBytes(rnd);
                    return BitConverter.ToInt64(rnd, 0);
                };
            }
            else if (t == typeof(ulong))
            {
                return _ =>
                {
                    var rnd = new byte[8];
                    this.random.NextBytes(rnd);
                    return BitConverter.ToUInt64(rnd, 0);
                };
            }
            else if (t == typeof(double))
            {
                return _ => (this.random.NextDouble() - 0.5) * double.MaxValue;
            }
            else if (t == typeof(float))
            {
                return _ => (float)(this.random.NextDouble() - 0.5) * float.MaxValue;
            }
            else if (t == typeof(decimal))
            {
                return _ => Math.Round(100 * (decimal)(this.random.NextDouble() - 0.5) * 1e2m) / 100; // TODO look into overflows?
            }
            else if (t == typeof(bool))
            {
                return _ => this.random.Next(0, 2) == 1;
            }
            else if (t == typeof(Guid))
            {
                return _ =>
                {
                    var rnd = new byte[16];
                    this.random.NextBytes(rnd);
                    return new Guid(rnd);
                };
            }
            else if (t == typeof(DateTime))
            {
                return _ => DateTime.Now + TimeSpan.FromMilliseconds((this.random.NextDouble() - 0.5) * 62e9);
            }
            else if (t == typeof(DateTimeOffset))
            {
                return _ => DateTimeOffset.Now + TimeSpan.FromMilliseconds((this.random.NextDouble() - 0.5) * 62e9);
            }

            return null;
        }

        /// <summary>
        /// Creates a string of the given length.
        /// </summary>
        /// <param name="length">The string length.</param>
        /// <returns>A random string.</returns>
        public string CreateString(int length)
        {
            return this.CreateString(string.Empty, length);
        }

        /// <summary>
        /// Creates a random string with the given prefix and maximum length.
        /// </summary>
        /// <param name="prefix">The prefix</param>
        /// <param name="length">The maximum length</param>
        /// <returns>A random string.</returns>
        public string CreateString(string prefix, int length)
        {
            var ba = Math.Max(0, length - prefix.Length);
            ba *= 3;
            if (ba % 4 != 0)
            {
                ba += 4;
            }
            ba /= 4;

            var rnd = new byte[ba];
            this.random.NextBytes(rnd);
            var result = prefix + Convert.ToBase64String(rnd);
            if (result.Length > length)
            {
                result = result.Substring(0, length);
            }

            return result;
        }
    }
}

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

namespace OrmMock.Shared
{
    using System;
    using System.IO;

    /// <summary>
    /// Defines extension methods for a BinaryWriter.
    /// </summary>
    public static class BinaryWriterExtensions
    {
        /// <summary>
        /// Writer the given object with the given type to the binary writer.
        /// </summary>
        /// <param name="bw">The binary writer.</param>
        /// <param name="o">The object to serialize.</param>
        /// <param name="t">The type of the object.</param>
        public static void Serialize(this BinaryWriter bw, object o, Type t)
        {
            if (ReflectionUtility.IsNullableOrString(t))
            {
                bw.Write(o != null);
                if (o == null)
                {
                    return;
                }

                t = Nullable.GetUnderlyingType(t) ?? t;
            }

            if (t == typeof(string))
            {
                bw.Write((string)o);
            }
            else if (t == typeof(Guid))
            {
                bw.Write(((Guid)o).ToByteArray());
            }
            else if (t.IsEnum)
            {
                bw.Write(Convert.ToInt32(o));
            }
            else if (t == typeof(bool))
            {
                bw.Write((bool)o);
            }
            else if (t == typeof(byte))
            {
                bw.Write((byte)o);
            }
            else if (t == typeof(DateTime))
            {
                bw.Write(((DateTime)o).ToBinary());
            }
            else if (t == typeof(DateTimeOffset))
            {
                var dto = (DateTimeOffset)o;
                bw.Write(dto.DateTime.ToBinary());
                bw.Write((short)dto.Offset.TotalMinutes);
            }
            else if (t == typeof(short))
            {
                bw.Write((short)o);
            }
            else if (t == typeof(ushort))
            {
                bw.Write((ushort)o);
            }
            else if (t == typeof(int))
            {
                bw.Write((int)o);
            }
            else if (t == typeof(uint))
            {
                bw.Write((uint)o);
            }
            else if (t == typeof(long))
            {
                bw.Write((long)o);
            }
            else if (t == typeof(ulong))
            {
                bw.Write((ulong)o);
            }
            else if (t == typeof(float))
            {
                bw.Write((float)o);
            }
            else if (t == typeof(double))
            {
                bw.Write((double)o);
            }
            else if (t == typeof(decimal))
            {
                bw.Write((decimal)o);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported type {t.Name}");
            }
        }
    }
}

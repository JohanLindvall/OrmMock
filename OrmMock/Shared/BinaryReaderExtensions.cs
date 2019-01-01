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
    /// Defines extensions methods for BinaryReader.
    /// </summary>
    public static class BinaryReaderExtensions
    {
        /// <summary>
        /// Deserializes an object of the given type.
        /// </summary>
        /// <param name="br">The binary reader instance.</param>
        /// <param name="t">The type of the object to deserialize.</param>
        /// <returns>A deserialized object.</returns>
        public static object Deserialize(this BinaryReader br, Type t)
        {
            if (ReflectionUtility.IsNullableOrString(t))
            {
                if (br.ReadBoolean() == false)
                {
                    return null;
                }

                t = Nullable.GetUnderlyingType(t) ?? t;
            }

            if (t == typeof(string))
            {
                return br.ReadString();
            }
            else if (t == typeof(Guid))
            {
                return new Guid(br.ReadBytes(16));
            }
            else if (t.IsEnum)
            {
                return Enum.ToObject(t, br.ReadInt32());
            }
            else if (t == typeof(bool))
            {
                return br.ReadBoolean();
            }
            else if (t == typeof(byte))
            {
                return br.ReadByte();
            }
            else if (t == typeof(DateTime))
            {
                return DateTime.FromBinary(br.ReadInt64());
            }
            else if (t == typeof(DateTimeOffset))
            {
                var dt = DateTime.FromBinary(br.ReadInt64());
                var offset = br.ReadInt16();
                return new DateTimeOffset(dt, TimeSpan.FromMinutes(offset));
            }
            else if (t == typeof(short))
            {
                return br.ReadInt16();
            }
            else if (t == typeof(ushort))
            {
                return br.ReadUInt16();
            }
            else if (t == typeof(int))
            {
                return br.ReadInt32();
            }
            else if (t == typeof(uint))
            {
                return br.ReadUInt32();
            }
            else if (t == typeof(long))
            {
                return br.ReadInt64();
            }
            else if (t == typeof(ulong))
            {
                return br.ReadUInt64();
            }
            else if (t == typeof(float))
            {
                return br.ReadSingle();
            }
            else if (t == typeof(double))
            {
                return br.ReadDouble();
            }
            else if (t == typeof(decimal))
            {
                return br.ReadDecimal();
            }
            else
            {
                throw new InvalidOperationException($"Unsupported type {t.Name}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrmMock.Shared
{
    public static class SerializationExtensions
    {
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
            else if (t == typeof(int))
            {
                bw.Write((int)o);
            }
            else if (t == typeof(long))
            {
                bw.Write((long)o);
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
            else if (t == typeof(int))
            {
                return br.ReadInt32();
            }
            else if (t == typeof(long))
            {
                return br.ReadInt64();
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

using System;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Landoria.ServerInventory.Serialization
{
    internal sealed class BinaryJson : IDisposable
    {
        internal const int MaximumBytes = 64 * 1024 * 1024;
        private const int MaximumEntries = 100000;
        private readonly MemoryStream stream;
        private readonly BinaryReader reader;
        private readonly BinaryWriter writer;
        internal bool Writing => writer != null;

        internal BinaryJson(byte[] data = null)
        {
            if (data != null && data.Length > MaximumBytes) throw new InvalidDataException("Character data is too large.");
            stream = data == null ? new MemoryStream() : new MemoryStream(data, false);
            var encoding = new UTF8Encoding(false, true);
            if (data == null) writer = new BinaryWriter(stream, encoding, true);
            else reader = new BinaryReader(stream, encoding, true);
        }

        internal byte[] Finish()
        {
            ValidateComplete();
            return stream.ToArray();
        }

        internal void ValidateComplete(int reservedBytes = 0)
        {
            if (!Writing && stream.Position != stream.Length) throw new InvalidDataException("Trailing character data.");
            if (stream.Length > MaximumBytes - reservedBytes) throw new InvalidDataException("Character data is too large.");
        }

        private T Field<T>(JObject obj, string name, Func<T> read, Action<T> write, Func<JToken, T> parse)
        {
            if (!Writing) { T value = read(); obj[name] = JToken.FromObject(value); return value; }
            T result = parse(Required(obj, name));
            write(result);
            if (stream.Length > MaximumBytes) throw new InvalidDataException("Character data is too large.");
            return result;
        }

        internal static JToken Required(JObject obj, string name)
            => obj[name] ?? throw new InvalidDataException("Missing JSON field: " + name);
        internal static long Integer(JToken token)
        {
            if (token.Type != JTokenType.Integer) throw new InvalidDataException("Expected an integer.");
            return token.Value<long>();
        }
        internal static bool Boolean(JToken token)
        {
            if (token.Type != JTokenType.Boolean) throw new InvalidDataException("Expected a boolean.");
            return token.Value<bool>();
        }
        internal static string Text(JToken token)
        {
            if (token.Type != JTokenType.String) throw new InvalidDataException("Expected a string.");
            return token.Value<string>();
        }

        internal int Int(JObject obj, string name) => Field(obj, name, reader == null ? null : reader.ReadInt32,
            writer == null ? null : new Action<int>(writer.Write), t => checked((int)Integer(t)));
        internal byte Byte(JObject obj, string name) => Field(obj, name, reader == null ? null : reader.ReadByte,
            writer == null ? null : new Action<byte>(writer.Write), t => checked((byte)Integer(t)));
        internal ushort UShort(JObject obj, string name) => Field(obj, name, reader == null ? null : reader.ReadUInt16,
            writer == null ? null : new Action<ushort>(writer.Write), t => checked((ushort)Integer(t)));
        internal bool Bool(JObject obj, string name) => Field(obj, name, reader == null ? null : reader.ReadBoolean,
            writer == null ? null : new Action<bool>(writer.Write), Boolean);
        internal string String(JObject obj, string name) => Field(obj, name, reader == null ? null : reader.ReadString,
            writer == null ? null : new Action<string>(writer.Write), Text);

        internal void Long(JObject obj, string name)
        {
            if (!Writing) obj[name] = reader.ReadInt64().ToString(CultureInfo.InvariantCulture);
            else writer.Write(long.Parse(Text(Required(obj, name)), CultureInfo.InvariantCulture));
        }

        internal void Float(JObject obj, string name)
        {
            if (Writing) WriteFloat(Required(obj, name));
            else obj[name] = ReadFloat();
        }

        private JToken ReadFloat()
        {
            int bits = reader.ReadInt32();
            float value = BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
            return float.IsNaN(value) || float.IsInfinity(value) || bits == int.MinValue
                ? (JToken)new JObject { ["floatBits"] = unchecked((uint)bits).ToString("X8", CultureInfo.InvariantCulture) }
                : new JValue((double)value);
        }

        private void WriteFloat(JToken token)
        {
            if (token is JObject bits)
            {
                writer.Write(uint.Parse(Text(Required(bits, "floatBits")), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                return;
            }
            if (token.Type != JTokenType.Float && token.Type != JTokenType.Integer)
                throw new InvalidDataException("Expected a number or floatBits.");
            float value = token.Value<float>();
            if (float.IsInfinity(value) || float.IsNaN(value)) throw new InvalidDataException("Non-finite float requires floatBits.");
            writer.Write(value);
        }
        internal void Version(JObject obj, int expected)
        {
            if (Int(obj, "version") != expected) throw new InvalidDataException("Unsupported format version; expected " + expected);
        }

        internal void Object(JObject obj, string name, Action<JObject> visit)
        {
            var child = Writing ? Required(obj, name) as JObject : new JObject();
            if (child == null) throw new InvalidDataException("Expected object: " + name);
            visit(child);
            if (!Writing) obj[name] = child;
        }

        internal void List(JObject obj, string name, Action<JObject> visit, bool shortCount = false, int fixedCount = -1)
        {
            var array = Writing ? Required(obj, name) as JArray : new JArray();
            if (array == null) throw new InvalidDataException("Expected array: " + name);
            int count = Writing ? array.Count : fixedCount >= 0 ? fixedCount : shortCount ? reader.ReadUInt16() : reader.ReadInt32();
            if (count < 0 || count > MaximumEntries) throw new InvalidDataException("Invalid array size.");
            if (Writing && fixedCount >= 0 && count != fixedCount) throw new InvalidDataException("Invalid fixed array size.");
            if (Writing && fixedCount < 0)
            {
                if (shortCount) writer.Write(checked((ushort)count));
                else writer.Write(count);
            }
            for (int i = 0; i < count; i++)
            {
                var item = Writing ? array[i] as JObject : new JObject();
                if (item == null) throw new InvalidDataException("Expected array entry object.");
                visit(item);
                if (!Writing) array.Add(item);
            }
            if (!Writing) obj[name] = array;
        }

        internal void Vector(JObject obj, string name)
            => Object(obj, name, v => { Float(v, "x"); Float(v, "y"); Float(v, "z"); });

        internal void Blob(JObject obj, string name, Action<BinaryJson, JObject> visit = null)
        {
            if (!Writing)
            {
                int count = reader.ReadInt32();
                if (count < 0 || count > MaximumBytes || count > stream.Length - stream.Position)
                    throw new InvalidDataException("Invalid binary block size.");
                byte[] data = reader.ReadBytes(count);
                if (visit == null) obj[name] = Convert.ToBase64String(data);
                else
                {
                    var child = new JObject();
                    using (var nested = new BinaryJson(data)) { visit(nested, child); nested.Finish(); }
                    obj[name] = child;
                }
                return;
            }
            byte[] bytes;
            if (visit == null) bytes = Convert.FromBase64String(Text(Required(obj, name)));
            else
            {
                var child = Required(obj, name) as JObject ?? throw new InvalidDataException("Expected package object.");
                using (var nested = new BinaryJson()) { visit(nested, child); bytes = nested.Finish(); }
            }
            if (bytes.Length > MaximumBytes) throw new InvalidDataException("Binary block is too large.");
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        internal int CompactCount(JObject obj, string name)
        {
            if (!Writing)
            {
                int value = reader.ReadByte();
                if ((value & 128) != 0) value = ((value & 127) << 8) | reader.ReadByte();
                obj[name] = value;
                return value;
            }
            int count = checked((int)Integer(Required(obj, name)));
            if (count < 0 || count > 32767) throw new InvalidDataException("Invalid compact count.");
            if (count >= 128) writer.Write((byte)((count >> 8) | 128));
            writer.Write((byte)(count & 255));
            return count;
        }

        public void Dispose()
        {
            reader?.Dispose();
            writer?.Dispose();
            stream.Dispose();
        }
    }
}

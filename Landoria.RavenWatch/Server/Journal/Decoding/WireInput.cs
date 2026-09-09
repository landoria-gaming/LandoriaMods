using System;
using System.IO;
using System.Text;

namespace Landoria.RavenWatch.Server.Journal.Decoding
{
    internal sealed class WireInput : IDisposable
    {
        internal const int MaximumBytes = 32 * 1024 * 1024;
        private readonly MemoryStream stream;
        private readonly BinaryReader reader;
        internal long Remaining => stream.Length - stream.Position;
        internal long Position => stream.Position;

        internal WireInput(byte[] bytes)
        {
            if (bytes.Length > MaximumBytes) throw new InvalidDataException("RPC exceeds decoder byte limit.");
            stream = new MemoryStream(bytes, false);
            reader = new BinaryReader(stream, new UTF8Encoding(false, true));
        }

        internal object Primitive(string type)
        {
            switch (type)
            {
                case "int16": return reader.ReadInt16();
                case "uint16": return reader.ReadUInt16();
                case "int32": return reader.ReadInt32();
                case "uint32": return reader.ReadUInt32();
                case "int64": return reader.ReadInt64().ToString(System.Globalization.CultureInfo.InvariantCulture);
                case "uint8": return reader.ReadByte();
                case "numItems": return ReadNumItems();
                case "float32": return reader.ReadSingle();
                case "float64": return reader.ReadDouble();
                case "boolean": return reader.ReadBoolean();
                case "char": return unchecked((short)reader.ReadChar());
                case "string": return ReadString();
                default: throw new InvalidDataException("Unknown primitive type: " + type);
            }
        }

        private int ReadNumItems()
        {
            int first = reader.ReadByte();
            return (first & 128) == 0 ? first : ((first & 127) << 8) | reader.ReadByte();
        }

        private string ReadString()
        {
            uint length = 0;
            for (int shift = 0; shift < 35; shift += 7)
            {
                byte part = reader.ReadByte();
                if (shift == 28 && part > 7) throw new InvalidDataException("Invalid string length prefix.");
                length |= (uint)(part & 127) << shift;
                if ((part & 128) == 0) return new UTF8Encoding(false, true).GetString(Bytes((int)length));
            }
            throw new InvalidDataException("Invalid string length prefix.");
        }

        internal byte[] Bytes(int count)
        {
            if (count < 0 || count > Remaining || count > MaximumBytes)
                throw new InvalidDataException("Invalid RPC byte length.");
            return reader.ReadBytes(count);
        }

        internal void RequireEnd()
        {
            if (Remaining != 0) throw new InvalidDataException("Undecoded trailing bytes: " + Remaining);
        }

        public void Dispose() { reader.Dispose(); stream.Dispose(); }
    }
}

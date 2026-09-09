using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server.Journal
{
    internal sealed class RpcJournal : IDisposable
    {
        private readonly FileStream stream;
        private bool hasEntries;
        private long appendPosition;
        internal RpcJournal(string directory, string prefix = "rpc")
            : this(Path.Combine(directory, prefix + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
                + "-" + Guid.NewGuid().ToString("N") + ".json"), false) { }

        internal static RpcJournal OpenPersistent(string path) => new RpcJournal(path, true);

        private RpcJournal(string path, bool persistent)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            stream = new FileStream(path, persistent ? FileMode.OpenOrCreate : FileMode.CreateNew,
                FileAccess.ReadWrite, FileShare.Read);
            try
            {
                if (stream.Length == 0)
                {
                    byte[] empty = Encoding.UTF8.GetBytes("[\n]\n");
                    stream.Write(empty, 0, empty.Length);
                    stream.Flush();
                    appendPosition = 2;
                }
                else ReadExisting();
            }
            catch (Exception error)
            {
                RpcCapture.Log.LogError(error);
                stream.Dispose();
                throw;
            }
        }

        private void ReadExisting()
        {
            // Validate once on opening, without loading the history into memory or rewriting it.
            using (var text = new StreamReader(stream, Encoding.UTF8, true, 4096, true))
            using (var reader = new JsonTextReader(text))
            {
                if (!reader.Read() || reader.TokenType != JsonToken.StartArray)
                    throw new InvalidDataException("Inventory journal must be a JSON array.");
                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                {
                    if (reader.TokenType != JsonToken.StartObject)
                        throw new InvalidDataException("Inventory journal entries must be objects.");
                    reader.Skip();
                    hasEntries = true;
                }
                if (reader.TokenType != JsonToken.EndArray || reader.Read())
                    throw new InvalidDataException("Incomplete inventory journal.");
            }
            long position = stream.Length;
            int value;
            do { stream.Position = --position; value = stream.ReadByte(); }
            while (value == ' ' || value == '\r' || value == '\n' || value == '\t');
            if (value != ']') throw new InvalidDataException("Missing journal closing bracket.");
            appendPosition = position;
        }

        internal void Append(JObject entry)
        {
            lock (stream)
            {
                byte[] bytes = Encoding.UTF8.GetBytes((hasEntries ? ",\n" : "")
                    + entry.ToString(Formatting.None) + "\n]\n");
                stream.Position = appendPosition;
                stream.Write(bytes, 0, bytes.Length);
                appendPosition = stream.Position - 3;
                stream.SetLength(stream.Position);
                stream.Flush();
                hasEntries = true;
            }
        }

        public void Dispose() { stream.Dispose(); }
    }
}

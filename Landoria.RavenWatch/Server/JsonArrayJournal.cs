using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server
{
    internal sealed class JsonArrayJournal : IDisposable
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
        private readonly FileStream stream;
        private bool hasEntries;

        internal JsonArrayJournal(string path)
        {
            stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.Read);
            Write(Utf8.GetBytes("[]"));
            stream.Flush();
        }

        internal void Append(JObject entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            string separator = hasEntries ? "," : string.Empty;
            byte[] bytes = Utf8.GetBytes(separator + entry.ToString(Formatting.None) + "]");
            stream.Position = stream.Length - 1;
            Write(bytes);
            stream.Flush();
            hasEntries = true;
        }

        private void Write(byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        public void Dispose()
        {
            stream.Dispose();
        }
    }
}

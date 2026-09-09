using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Landoria.RavenWatch.Server
{
    internal sealed class RpcJournal : IDisposable
    {
        private readonly FileStream stream;
        private bool hasEntries;
        internal RpcJournal(string directory, string prefix = "rpc")
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, prefix + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
                + "-" + Guid.NewGuid().ToString("N") + ".json");
            stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            byte[] empty = Encoding.UTF8.GetBytes("[\n]\n");
            stream.Write(empty, 0, empty.Length);
            stream.Flush();
        }

        internal void Append(JObject entry)
        {
            lock (stream)
            {
                byte[] bytes = Encoding.UTF8.GetBytes((hasEntries ? ",\n" : "")
                    + entry.ToString(Formatting.None) + "\n]\n");
                stream.Seek(hasEntries ? -3 : -2, SeekOrigin.End);
                stream.Write(bytes, 0, bytes.Length);
                stream.SetLength(stream.Position);
                stream.Flush();
                hasEntries = true;
            }
        }

        public void Dispose() { stream.Dispose(); }
    }
}

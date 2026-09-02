using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Landoria.CharacterVault
{
    internal sealed class VaultStorage : ICharacterProfileCatalog
    {
        private const string BackupDirectory = "backups";

        bool ICharacterProfileCatalog.HasProfile(string accountId) => HasProfile(accountId);

        internal bool TryRead(string accountId, string name, out byte[] data)
        {
            string path = ProfilePath(accountId, name);
            if (!File.Exists(path))
            {
                path = FindProfilePath(accountId, name);
            }
            data = File.Exists(path) ? File.ReadAllBytes(path) : null;
            return data != null;
        }

        private static string FindProfilePath(string accountId, string name)
        {
            string prefix = SafeSegment(accountId) + "_";
            string root = StorageRoot();
            if (!Directory.Exists(root)) return string.Empty;
            return Directory.GetFiles(root, prefix + "*.fch", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(path => string.Equals(
                    Path.GetFileNameWithoutExtension(path).Substring(prefix.Length),
                    name, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        }

        internal bool HasProfile(string accountId)
        {
            return GetProfileNames(accountId).Count > 0;
        }

        internal IReadOnlyList<string> GetProfileNames(string accountId)
        {
            string prefix = SafeSegment(accountId) + "_";
            string root = StorageRoot();
            if (!Directory.Exists(root))
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(root, prefix + "*.fch", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(fileName => fileName.StartsWith(prefix, StringComparison.Ordinal))
                .Select(fileName => fileName.Substring(prefix.Length))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
        }

        internal void Commit(string accountId, string name, byte[] data)
        {
            Directory.CreateDirectory(StorageRoot());
            string current = ProfilePath(accountId, name);
            string next = current + ".new";
            WriteDurably(next, data);
            PreserveBackup(data, Path.GetFileNameWithoutExtension(current));
            Replace(next, current);
        }

        private void PreserveBackup(byte[] data, string profileName)
        {
            string directory = Path.Combine(StorageRoot(), BackupDirectory);
            Directory.CreateDirectory(directory);
            string timestamp = DateTime.UtcNow.ToString(
                "yyyyMMdd'T'HHmmssfffffff'Z'", CultureInfo.InvariantCulture);
            WriteDurably(Path.Combine(directory, $"{profileName}_{timestamp}.fch"), data);
            foreach (string deleted in BackupRetention.Apply(directory, profileName))
            {
                CharacterVaultPlugin.Log.LogInfo(
                    $"Deleted expired character backup {deleted} for profile {profileName}.");
            }
        }

        private static string ProfileFileName(string accountId, string name)
        {
            return $"{SafeSegment(accountId)}_{SafeSegment(name)}.fch";
        }

        internal static string SafeSegment(string value)
        {
            const string invalid = "<>:\"/\\|?*";
            return new string(value
                .Select(character => char.IsControl(character) || invalid.Contains(character)
                    ? '_' : character)
                .ToArray());
        }

        private string ProfilePath(string accountId, string name)
        {
            return Path.Combine(StorageRoot(), ProfileFileName(accountId, name));
        }

        private static string StorageRoot() => Path.Combine(
            Utils.GetSaveDataPath(FileHelpers.FileSource.Local), "characters_local");

        private static void WriteDurably(string path, byte[] data)
        {
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write,
                FileShare.None, 65536, FileOptions.WriteThrough))
            {
                stream.Write(data, 0, data.Length);
                stream.Flush(true);
            }
        }

        private static void Replace(string source, string destination)
        {
            if (File.Exists(destination))
            {
                File.Replace(source, destination, null);
            }
            else
            {
                File.Move(source, destination);
            }
        }

        internal static string Hash(byte[] data)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                return BitConverter.ToString(algorithm.ComputeHash(data)).Replace("-", string.Empty);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Mono.Cecil;

namespace Landoria.ModSentry
{
    internal static class PluginPolicyLoader
    {
        internal static PluginPolicy Load()
        {
            return new PluginPolicy(
                LoadDirectory(Path.Combine(Paths.ConfigPath, "ModSentry_Required")),
                LoadDirectory(Path.Combine(Paths.ConfigPath, "ModSentry_Optional")));
        }

        private static IReadOnlyList<PluginDescriptor> LoadDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(
                    $"ModSentry policy directory is missing: {directory}");
            }

            return Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly)
                .Select(ReadDescriptor)
                .OrderBy(plugin => plugin.Guid, StringComparer.Ordinal)
                .ToList();
        }

        internal static PluginDescriptor ReadDescriptor(string path)
        {
            try
            {
                using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path))
                {
                    CustomAttribute attribute = assembly.MainModule.Types
                        .SelectMany(type => type.CustomAttributes)
                        .SingleOrDefault(item =>
                            item.AttributeType.FullName == typeof(BepInPlugin).FullName);

                    if (attribute == null || attribute.ConstructorArguments.Count < 3)
                    {
                        return CreateFallbackDescriptor(assembly, path);
                    }

                    return CreateDescriptor(path, attribute);
                }
            }
            catch (BadImageFormatException exception)
            {
                ModSentryPlugin.Log.LogDebug(
                    $"Using a fallback descriptor for {path}: {exception}");
                return CreateFallbackDescriptor(null, path);
            }
        }

        private static PluginDescriptor CreateFallbackDescriptor(AssemblyDefinition assembly, string path)
        {
            string name = assembly?.Name?.Name ?? Path.GetFileNameWithoutExtension(path);
            string guid = $"Landoria.NonBepInPlugin.{name}";
            string version = assembly?.Name?.Version?.ToString() ?? "0.0.0";

            return new PluginDescriptor(guid, name, version, PluginInventory.Sha256(path), false);
        }

        private static PluginDescriptor CreateDescriptor(string path, CustomAttribute attribute)
        {
            string guid = (string)attribute.ConstructorArguments[0].Value;
            string name = (string)attribute.ConstructorArguments[1].Value;
            string version = (string)attribute.ConstructorArguments[2].Value;
            return new PluginDescriptor(guid, name, version, PluginInventory.Sha256(path));
        }
    }
}

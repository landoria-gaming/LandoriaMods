using System;
using System.Collections.Concurrent;
using System.Reflection;
using UnityEngine;

namespace Landoria.ModSentry
{
    internal static class ManagedCheatDetector
    {
        private const int AssembliesPerFrame = 2;
        private static readonly ConcurrentQueue<Assembly> Pending =
            new ConcurrentQueue<Assembly>();
        private static ZRpc _serverRpc;
        private static Detection _detection;
        private static bool _serverReady;
        private static bool _reported;
        private static bool _initialized;

        internal static void Enable(ZRpc serverRpc)
        {
            if (Application.isBatchMode)
            {
                return;
            }
            Connect(serverRpc);
            _serverReady = true;
            if (!_initialized)
            {
                _initialized = true;
                AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
                foreach (Assembly assembly in
                    AppDomain.CurrentDomain.GetAssemblies())
                {
                    Pending.Enqueue(assembly);
                }
            }
            ModSentryPlugin.Log.LogDebug(
                "Enabled managed cheat assembly inspection for this server.");
        }

        private static void Connect(ZRpc serverRpc)
        {
            _serverRpc = serverRpc;
            _serverReady = false;
            _reported = false;
        }

        internal static void Disconnect()
        {
            _serverRpc = null;
            _serverReady = false;
            _reported = false;
        }

        internal static void Update()
        {
            if (!_initialized)
            {
                return;
            }
            for (int index = 0; index < AssembliesPerFrame &&
                Pending.TryDequeue(out Assembly assembly); index++)
            {
                Inspect(assembly);
            }
            ReportIfNeeded();
        }

        internal static void Shutdown()
        {
            if (_initialized)
            {
                AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
            }
            while (Pending.TryDequeue(out _))
            {
            }
            Disconnect();
            _detection = null;
            _initialized = false;
        }

        private static void OnAssemblyLoad(object sender,
            AssemblyLoadEventArgs arguments)
        {
            if (arguments?.LoadedAssembly != null)
            {
                Pending.Enqueue(arguments.LoadedAssembly);
            }
        }

        private static void Inspect(Assembly assembly)
        {
            if (_detection != null || assembly == null || assembly.IsDynamic)
            {
                return;
            }
            try
            {
                string name = assembly.GetName().Name ?? string.Empty;
                if (KnownCheatCatalog.TryMatchAssembly(name, out string tool))
                {
                    Record(tool, "assembly_name", name);
                    return;
                }
                InspectNamespaces(assembly);
            }
            catch (Exception exception)
            {
                ModSentryPlugin.Log.LogDebug(
                    "Managed assembly inspection skipped one assembly: " + exception);
            }
        }

        private static void InspectNamespaces(Assembly assembly)
        {
            foreach (Type type in LoadableTypes(assembly))
            {
                string value = type?.Namespace;
                if (KnownCheatCatalog.TryMatchNamespace(value,
                    out string tool))
                {
                    Record(tool, "type_namespace", value);
                    return;
                }
            }
        }

        private static Type[] LoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                ModSentryPlugin.Log.LogDebug(
                    "Loaded the available types from a partially loadable assembly: " + exception);
                return exception.Types ?? Array.Empty<Type>();
            }
        }

        private static void Record(string tool, string vector,
            string indicator)
        {
            _detection = new Detection(tool, vector, indicator);
            ModSentryPlugin.Log.LogWarning(
                $"Known managed cheat tool detected: {tool} " +
                $"({vector}: {indicator}).");
        }

        private static void ReportIfNeeded()
        {
            if (_detection == null || _reported || !_serverReady ||
                _serverRpc == null)
            {
                return;
            }
            _reported = true;
            KnownCheatReport.Send(_serverRpc, _detection.Tool,
                _detection.Vector, _detection.Indicator);
        }

        private sealed class Detection
        {
            internal Detection(string tool, string vector, string indicator)
            {
                Tool = tool;
                Vector = vector;
                Indicator = indicator;
            }

            internal string Tool { get; }
            internal string Vector { get; }
            internal string Indicator { get; }
        }
    }
}

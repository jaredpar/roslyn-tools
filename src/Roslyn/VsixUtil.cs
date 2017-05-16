using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn
{
    /// <summary>
    /// Contains the VS extension API wrappers and helps with our need to delay the
    /// loading of the extension manager types.
    ///
    /// No Roslyn specific knowledge here. 
    /// </summary>
    internal sealed class VsixUtil : IDisposable
    {
        private const string ExtensionManagerCollectionPath = "ExtensionManager";

        internal string VsInstallDir { get; }
        internal string RootSuffix { get; }
        internal ExternalSettingsManager SettingsManager { get; }

        internal VsixUtil(string vsInstallDir, string rootSuffix)
        {
            var devenvFilePath = Path.Combine(vsInstallDir, @"Common7\IDE\DevEnv.exe");
            VsInstallDir = vsInstallDir;
            RootSuffix = rootSuffix;
            SettingsManager = ExternalSettingsManager.CreateForApplication(devenvFilePath, rootSuffix);
        }

        public void Dispose()
        {
            SettingsManager.Dispose();
        }

        internal bool IsInstalledGlobally(IInstalledExtension extension) => extension.InstallPath.StartsWith(VsInstallDir, StringComparison.OrdinalIgnoreCase);

        internal ExtensionManagerService CreateExtensionManagerService() => new ExtensionManagerService(SettingsManager);

        internal void WithExtensionManager(Action<IVsExtensionManager> action)
        {
            using (var service = CreateExtensionManagerService())
            {
                var extensionManager = (IVsExtensionManager)service;
                action(extensionManager);
            }
        }

        /// <summary>
        /// This allows us to modify globally installing extensions like Roslyn
        /// </summary>
        private static void EnableLoadingAllExtensions(WritableSettingsStore settingsStore)
        {
            const string EnableAdminExtensionsProperty = "EnableAdminExtensions";

            if (!settingsStore.CollectionExists(ExtensionManagerCollectionPath))
            {
                settingsStore.CreateCollection(ExtensionManagerCollectionPath);
            }

            if (!settingsStore.GetBoolean(ExtensionManagerCollectionPath, EnableAdminExtensionsProperty, defaultValue: false))
            {
                settingsStore.SetBoolean(ExtensionManagerCollectionPath, EnableAdminExtensionsProperty, value: true);
            }
        }

        /// <summary>
        /// If an extension is both pending delete and pending install things can break.  
        /// </summary>
        private static void RemoveExtensionFromPendingDeletions(WritableSettingsStore settingsStore, IExtensionHeader vsixToInstallHeader)
        {
            const string PendingDeletionsCollectionPath = ExtensionManagerCollectionPath + @"\PendingDeletions";
            var vsixToDeleteProperty = $"{vsixToInstallHeader.Identifier},{vsixToInstallHeader.Version}";

            if (settingsStore.CollectionExists(PendingDeletionsCollectionPath) &&
                settingsStore.PropertyExists(PendingDeletionsCollectionPath, vsixToDeleteProperty))
            {
                settingsStore.DeleteProperty(PendingDeletionsCollectionPath, vsixToDeleteProperty);
            }
        }

        /// <summary>
        /// Note that extensions have changed so the next restart of Visual Studio will properly rebuild all
        /// of the components.
        /// </summary>
        private static void UpdateLastExtensionsChange(WritableSettingsStore settingsStore)
        {
            const string ExtensionsChangedProperty = "ExtensionsChanged";

            if (!settingsStore.CollectionExists(ExtensionManagerCollectionPath))
            {
                settingsStore.CreateCollection(ExtensionManagerCollectionPath);
            }

            settingsStore.SetInt64(ExtensionManagerCollectionPath, ExtensionsChangedProperty, value: DateTime.UtcNow.ToFileTimeUtc());
        }
    }

    internal sealed class VsixUtilFactory
    {
        internal static VsixUtilFactory s_instance;

        internal string VsInstallDir { get; }

        private VsixUtilFactory(string vsInstallDir)
        {
            VsInstallDir = vsInstallDir;
            HookResolve(vsInstallDir);
        }

        internal VsixUtil CreateVsixUtil(string rootSuffix = "")
        {
            return new VsixUtil(VsInstallDir, rootSuffix);
        }

        internal static VsixUtilFactory GetOrCreate(string vsInstallDir)
        {
            if (s_instance == null)
            {
                s_instance = new VsixUtilFactory(vsInstallDir);
            }

            if (!StringComparer.OrdinalIgnoreCase.Equals(s_instance.VsInstallDir, vsInstallDir))
            {
                throw new Exception($"{nameof(VsixUtilFactory)} already created for a different install path {vsInstallDir}");
            }

            return s_instance;
        }

        /// <summary>
        /// Hook all assembly resolve failures to look into the VS installation path.  This allows us to be fairly
        /// flexible to the assembly version of VS that we are targeting. 
        /// </summary>
        /// <param name="vsInstallDir"></param>
        private static void HookResolve(string vsInstallDir)
        {
            var assemblyResolutionPaths = new string[] {
                    Path.Combine(vsInstallDir, @"Common7\IDE"),
                    Path.Combine(vsInstallDir, @"Common7\IDE\PrivateAssemblies"),
                    Path.Combine(vsInstallDir, @"Common7\IDE\PublicAssemblies")
                };

            AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs eventArgs) =>
            {
                var assemblyFileName = $"{eventArgs.Name.Split(',')[0]}.dll";

                foreach (var assemblyResolutionPath in assemblyResolutionPaths)
                {
                    var assemblyFilePath = Path.Combine(assemblyResolutionPath, assemblyFileName);

                    if (File.Exists(assemblyFilePath))
                    {
                        return Assembly.LoadFrom(assemblyFilePath);
                    }
                }

                return null;
            };

        }

    }
}

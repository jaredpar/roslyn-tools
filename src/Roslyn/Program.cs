using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.Settings;
using System.Runtime.CompilerServices;

namespace Roslyn
{
    internal static class Program
    {
        internal static int Main(string[] args)
        {
            if (!Options.TryParse(args, out var options))
            {
                return 1;
            }

            try
            {
                var factory = VsixUtilFactory.GetOrCreate(options.VsInstallDir);
                return Go(factory, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }
        }

        /// <summary>
        /// Don't inline this method because we need to make sure all of the type loading happens after we
        /// have properly hooked load via the factory
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Go(VsixUtilFactory factory, Options options)
        {
            using (var vsixUtil = factory.CreateVsixUtil(options.RootSuffix))
            {
                var runner = new Runner(vsixUtil, options);
                return runner.Go();
            }
        }

        /*
        // Move all of this into a local method so that it only causes the assembly loads after the resolver has been hooked up
        void RunProgram()
        {
            using (var settingsManager = ExternalSettingsManager.CreateForApplication(devenvPath, rootSuffix))
            {
                ExtensionManagerService extensionManagerService = null;

                try
                {
                    extensionManagerService = new ExtensionManagerService(settingsManager);

                    if (uninstallAll)
                    {
                        Console.WriteLine("Uninstalling all... ");
                        UninstallAll(extensionManagerService);
                    }
                    else
                    {
                        var extensionManager = (IVsExtensionManager)(extensionManagerService);
                        var vsixToInstall = extensionManager.CreateInstallableExtension(vsixPath);
                        var vsixToInstallHeader = vsixToInstall.Header;

                        var foundBefore = extensionManagerService.TryGetInstalledExtension(vsixToInstallHeader.Identifier, out var installedVsixBefore);
                        var installedGlopublicballyBefore = foundBefore && installedVsixBefore.InstallPath.StartsWith(vsInstallDir, StringComparison.OrdinalIgnoreCase);

                        if (uninstall)
                        {
                            if (foundBefore && !installedGloballyBefore)
                            {
                                Console.WriteLine("Uninstalling {0}... ", vsixPath);
                                extensionManagerService.Uninstall(installedVsixBefore);
                            }
                            else
                            {
                                Console.WriteLine("Nothing to uninstall... ");
                            }
                        }
                        else
                        {
                            if (foundBefore && installedGloballyBefore && (vsixToInstallHeader.Version < installedVsixBefore.Header.Version))
                            {
                                throw new Exception($"The version you are attempting to install ({vsixToInstallHeader.Version}) has a version that is less than the one installed globally ({installedVsixBefore.Header.Version}).");
                            }
                            else if (foundBefore && !installedGloballyBefore)
                            {
                                Console.WriteLine("Updating {0}... ", vsixPath);
                                extensionManagerService.Uninstall(installedVsixBefore);
                            } 
                            else
                            {
                                Console.WriteLine("Installing {0}... ", vsixPath);
                            }

                            extensionManagerService.Install(vsixToInstall, perMachine: false);
                            var settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

                            EnableLoadingAllExtensions(settingsStore);
                            RemoveExtensionFromPendingDeletions(settingsStore, vsixToInstallHeader);
                            UpdateLastExtensionsChange(settingsStore);

                            // Recreate the extensionManagerService to force the extension cache to recreate
                            extensionManagerService?.Close();
                            extensionManagerService = new ExtensionManagerService(settingsManager);

                            var foundAfter = extensionManagerService.TryGetInstalledExtension(vsixToInstallHeader.Identifier, out var installedVsixAfter);
                            var installedGloballyAfter = foundAfter && installedVsixAfter.InstallPath.StartsWith(vsInstallDir, StringComparison.OrdinalIgnoreCase);

                            if (uninstall && foundAfter)
                            {
                                if (installedGloballyBefore && installedGloballyAfter)
                                {
                                    throw new Exception($"The extension failed to uninstall. It is still installed globally.");
                                }
                                else if (!installedGloballyBefore && installedGloballyAfter)
                                {
                                    Console.WriteLine("The local extension was succesfully uninstalled. However, the global extension is still installed.");
                                }
                                else
                                {
                                    Console.WriteLine("The extension was succesfully uninstalled.");
                                }
                            }
                            else if (!uninstall)
                            {
                                if (!foundAfter)
                                {
                                    throw new Exception($"The extension failed to install. It could not be located.");
                                }
                                else if (installedVsixAfter.Header.Version != vsixToInstallHeader.Version)
                                {
                                    throw new Exception("The extension failed to install. The located version does not match the expected version.");
                                }
                                else
                                {
                                    Console.WriteLine("The extension was succesfully installed.");
                                }
                            }
                        }
                    }
                }
                finally
                {
                    extensionManagerService?.Close();
                    extensionManagerService = null;
                }

                Console.WriteLine("Done!");
            }
            */
    }
}

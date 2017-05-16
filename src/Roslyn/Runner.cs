using Microsoft.VisualStudio.ExtensionManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn
{
    internal sealed class Runner
    {
        /// <summary>
        /// Identifiers of the known Roslyn VSIX
        /// </summary>
        internal static string[] KnownRoslynVsixIdentifiers => new[]
            {
                "21BAC26D-2935-4D0D-A282-AD647E2592B5",
                "7922692f-f018-45e7-8f3f-d3b7c0262841",
                "49e24138-9ee3-49e0-8ede-6b39f49303bf",
                "500fff63-afcf-4195-8db4-3fa8a5180e79",
                "58293943-56F1-4734-82FC-0411DCF49DE1",
                "0b5e8ddb-f12d-4131-a71d-77acc26a798f",
            };

        internal VsixUtil VsixUtil { get; }
        internal Options Options { get; }

        internal Runner(VsixUtil vsixUtil, Options options)
        {
            VsixUtil = vsixUtil;
            Options = options;
        }

        internal int Go()
        {
            switch (Options.Command)
            {
                case Command.Clear: return RunClear();
                default: throw new Exception($"Bad command {Options.Command}");

            }
        }

        private int RunClear()
        {
            Console.WriteLine("Clearing Roslyn Extensions");
            VsixUtil.WithExtensionManager(extensionManager =>
            {
                foreach (var identifier in KnownRoslynVsixIdentifiers)
                { 
                    if (extensionManager.TryGetInstalledExtension(identifier, out var extension) &&
                        !IsInstalledGlobally(extension))
                    {
                        Console.WriteLine($"\tUninstalling {extension.Header.Name}");
                        extensionManager.Uninstall(extension);
                    }
                }
            });

            return 0;
        }

        internal bool IsRoslynExtension(IExtensionHeader header) => KnownRoslynVsixIdentifiers.Contains(header.Identifier, StringComparer.OrdinalIgnoreCase);

        internal bool IsInstalledGlobally(IInstalledExtension extension) => VsixUtil.IsInstalledGlobally(extension);
    }
}

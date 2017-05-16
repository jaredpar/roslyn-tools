using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn
{
    internal enum Command
    {
        Clear
    }

    internal sealed class Options
    {
        internal Command Command { get; }
        internal string VsInstallDir { get; }
        internal string RootSuffix { get; }

        internal Options(
            Command command,
            string vsInstallDir,
            string rootSuffix)
        {
            Command = command;
            VsInstallDir = vsInstallDir;
            RootSuffix = rootSuffix;
        }

        internal static bool TryParse(string[] args, out Options options)
        {
            var vsInstallDir = @"C:\Program Files (x86)\Microsoft Visual Studio\Preview\Enterprise";
            var rootSuffix = "";

            if (args.Length != 1)
            {
                options = null;
                return false;

            }

            Command command;
            switch (args[0].ToLower())
            {
                case "clear":
                    command = Command.Clear;
                    break;
                default:
                    options = null;
                    return false;
            }

            options = new Options(command, vsInstallDir, rootSuffix);
            return true;
        }
    }
}

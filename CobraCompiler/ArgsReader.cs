using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler
{
    static class ArgsReader
    {
        private static readonly Dictionary<string, CompilerFlags> FlagSymbols = new Dictionary<string, CompilerFlags>()
        {
            {"-ps", CompilerFlags.PrintScan},
            {"-pp", CompilerFlags.PrintParse },
            {"-he", CompilerFlags.HideErrors },
            {"-hw", CompilerFlags.HideWarnings },
            {"-debug", CompilerFlags.Debug}
        };

        public static CompilationOptions ReadArgs(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Provide filepath");
                throw new ArgumentException();
            }

            string filePath = args[0];

            Dictionary<string, string> flagArgs = new Dictionary<string, string>();
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i][0] != '-')
                {
                    Console.WriteLine($"Invalid argument: \"{args[i]}\"");
                    throw new ArgumentException();
                }

                if (i + 1 < args.Length && args[i + 1][0] != '-')
                {
                    flagArgs[args[i]] = args[i + 1];
                    i++;
                    continue;
                }

                flagArgs[args[i]] = "";
            }

            CompilerFlags flags = CompilerFlags.None;

            foreach (string flag in flagArgs.Keys)
            {
                if (FlagSymbols.ContainsKey(flag))
                    flags |= FlagSymbols[flag];
                else
                {
                    Console.WriteLine($"Invalid flag: \"{flag}\"");
                }
            }

            return new CompilationOptions(filePath, flags);
        }
    }
}

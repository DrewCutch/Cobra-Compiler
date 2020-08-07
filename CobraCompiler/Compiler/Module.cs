using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.Compiler
{
    public class Module
    {
        public readonly string Name;
        public readonly string SystemPath;
        public string FullName => SystemPath + System.SystemPathDelimiter + Name;
        public readonly FileReader File;

        public Module(string name, string systemPath, FileReader file)
        {
            Name = name;
            SystemPath = systemPath;
            File = file;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Compiler
{
    class System
    {
        public const string SystemPathDelimiter = ".";

        public readonly string Name;
        public readonly string SystemPath;
        public string FullName => SystemPath + SystemPathDelimiter + Name;
        public readonly IReadOnlyList<System> SubSystems;
        public readonly IReadOnlyList<Module> Modules;

        public System(string name, string systemPath, IEnumerable<System> subSystems, IEnumerable<Module> modules)
        {
            Name = name;
            SystemPath = systemPath;
            SubSystems = new List<System>(subSystems);
            Modules = new List<Module>(modules);
        }

        public System(string name, string systemPath, IEnumerable<Module> modules)
        {
            Name = name;
            SystemPath = systemPath;
            Modules = new List<Module>(modules);
            SubSystems = new List<System>();
        }

        public System(string name, string systemPath)
        {
            Name = name;
            SystemPath = systemPath;
            Modules = new List<Module>();
            SubSystems = new List<System>();
        }
    }
}

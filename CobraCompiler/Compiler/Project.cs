using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Compiler
{
    public class Project
    {
        public readonly string Name;
        public readonly IReadOnlyList<System> Systems;

        public Project(string name, IEnumerable<System> systems)
        {
            Name = name;
            Systems = new List<System>(systems);
        }
    }
}

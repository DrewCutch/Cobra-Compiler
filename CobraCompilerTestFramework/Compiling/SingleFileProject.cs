using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Compiler;
using CobraCompiler.Scanning;
using System = CobraCompiler.Compiler.System;

namespace CobraCompilerTestFramework.Compiling
{
    class SingleFileProject: Project
    {
        public SingleFileProject(string name, FileReader file) : base(name, SingleFileSystem(name, file))
        {

        }

        private static CobraCompiler.Compiler.System[] SingleFileSystem(string name, FileReader file)
        {
            Module module = new Module(name, file.Path, file);
            return new[] {new CobraCompiler.Compiler.System(name, file.Path, new[] {module})};
        }
    }
}

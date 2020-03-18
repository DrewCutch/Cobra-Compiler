using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Scopes;

namespace CobraCompiler.Compiler
{
    class CheckedProject: Project
    {
        public readonly GlobalScope Scope;

        public CheckedProject(Project proj, GlobalScope scope) : base(proj.Name, proj.Systems)
        {
            Scope = scope;
        }
    }
}

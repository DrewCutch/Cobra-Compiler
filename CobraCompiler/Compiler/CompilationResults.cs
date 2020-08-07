using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Compiler
{
    public class CompilationResults
    {
        public readonly double ScanDuration;
        public readonly double ParseDuration;
        public readonly double TypeCheckDuration;
        public readonly double AssemblyDuration;
        public readonly double TotalDuration;

        public CompilationResults(double scanDuration, double parseDuration, double typeCheckDuration,
            double assemblyDuration, double totalDuration)
        {
            ScanDuration = scanDuration;
            ParseDuration = parseDuration;
            TypeCheckDuration = typeCheckDuration;
            AssemblyDuration = assemblyDuration;
            TotalDuration = totalDuration;
        }
    }
}

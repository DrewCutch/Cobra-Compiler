using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Assemble;
using CobraCompiler.Compiler;
using CobraCompiler.ErrorLogging;
using CobraCompiler.Parse;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.PrettyPrint;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Scanning;

namespace CobraCompiler
{
    static class Program
    {
        static void Main(string[] args)
        {
            CompilationOptions options;

            try
            {
                options = ArgsReader.ReadArgs(args);
            }
            catch (ArgumentException)
            {
                return;
            }

            
            ProjectReader projectReader = new ProjectReader(options.FilePath);
            Project project = projectReader.ReadProject();
            
            ErrorLogger errorLogger = new ErrorLogger();

            Compiler.Compiler compiler = new Compiler.Compiler(options, errorLogger);

            try
            {
                compiler.Compile(project);
            }
            catch (CompilerException ce)
            {
                if (!options.Flags.HasFlag(CompilerFlags.HideErrors))
                    errorLogger.DisplayErrors();
                if(!options.Flags.HasFlag(CompilerFlags.HideWarnings))
                    errorLogger.DisplayWarnings();
            }
            
            Console.ReadKey();
        }
    }
}

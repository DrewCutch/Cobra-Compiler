using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Assemble;
using CobraCompiler.ErrorLogging;
using CobraCompiler.Parse;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.PrettyPrint;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Parse.TypeCheck;
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

            ErrorLogger errorLogger = new ErrorLogger();

            Compiler compiler = new Compiler(options, errorLogger);

            compiler.Run();

            if (!options.Flags.HasFlag(CompilerFlags.HideErrors))
                errorLogger.DisplayErrors();
            
            Console.ReadKey();
        }
    }
}

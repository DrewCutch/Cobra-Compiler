using System;
using System.Collections.Generic;
using CobraCompiler.Assemble;
using CobraCompiler.ErrorLogging;
using CobraCompiler.Parse;
using CobraCompiler.Parse.PrettyPrint;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Parse.TypeCheck;
using CobraCompiler.Scanning;

namespace CobraCompiler.Compiler
{
    class Compiler
    {
        public readonly CompilationOptions Options;
        public readonly ErrorLogger ErrorLogger;

        public Compiler(CompilationOptions options, ErrorLogger errorLogger)
        {
            Options = options;
            ErrorLogger = errorLogger;
        }

        public void Run()
        {
            FileReader fileReader = new FileReader(Options.FilePath);
            Scanner scanner = new Scanner(fileReader, ErrorLogger);

            IReadOnlyList<Token> tokens = scanner.GetTokens();

            if (Options.Flags.HasFlag(CompilerFlags.PrintScan))
            {
                foreach (Token token in tokens)
                {
                    if (token.Type == TokenType.NewLine)
                        Console.WriteLine(token);
                    else
                        Console.Write(token);
                }
                Console.WriteLine();
            }

            if (ErrorLogger.HasErrors)
                return;

            Parser parser = new Parser(tokens, ErrorLogger);
            List<Statement> statements = parser.Parse();

            if (Options.Flags.HasFlag(CompilerFlags.PrintParse))
            {
                AstPrinter astPrinter = new AstPrinter();
                astPrinter.PrintStatements(statements);
            }

            if (ErrorLogger.HasErrors)
                return;

            TypeChecker typeChecker = new TypeChecker(ErrorLogger);

            ModuleScope module = typeChecker.CreateModule(statements, "supportedcode");

            if (ErrorLogger.HasErrors)
                return;

            Assembler assembler = new Assembler("CobraTest");
            assembler.AssembleModule(module);

            if (ErrorLogger.HasErrors)
                return;

            assembler.SaveAssembly();
        }
    }
}

// "C:\\Users\\DrewC\\source\\repos\\CobraCompiler\\CobraCompiler\\supportedcode.txt"
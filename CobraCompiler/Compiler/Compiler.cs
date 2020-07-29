﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using CobraCompiler.Assemble;
using CobraCompiler.ErrorLogging;
using CobraCompiler.Parse;
using CobraCompiler.Parse.PrettyPrint;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Scanning;
using CobraCompiler.TypeCheck;

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

        public void Compile(Project project)
        {
            Stopwatch totalTimer = new Stopwatch();
            
            Stopwatch timer = new Stopwatch();

            totalTimer.Start();
            timer.Start();
            IReadOnlyList<ScannedModule> scannedModules = Scan(project);
            ErrorLogger.WriteWithColor($"Scanned {project.Name} in {timer.Elapsed.TotalSeconds} seconds\n", ConsoleColor.Green);
            
            timer.Restart();
            IReadOnlyList<ParsedModule> parsedModules = Parse(scannedModules);
            ErrorLogger.WriteWithColor($"Parsed {project.Name} in {timer.Elapsed.TotalSeconds} seconds\n", ConsoleColor.Green);

            timer.Restart();
            CheckedProject checkedProject = Check(project, parsedModules);
            ErrorLogger.WriteWithColor($"Checked {project.Name} in {timer.Elapsed.TotalSeconds} seconds\n", ConsoleColor.Green);

            timer.Restart();
            Assemble(checkedProject);
            ErrorLogger.WriteWithColor($"Assembled {project.Name} in {timer.Elapsed.TotalSeconds} seconds\n", ConsoleColor.Green);
            timer.Stop();

            ErrorLogger.WriteWithColor($"Compiled {project.Name} in {totalTimer.Elapsed.TotalSeconds} seconds", ConsoleColor.Green);
            totalTimer.Stop();
        }

        private List<ScannedModule> Scan(Project project)
        {
            Queue<System> systems = new Queue<System>(project.Systems);
            List<ScannedModule> scannedModules = new List<ScannedModule>();

            do
            {
                System next = systems.Dequeue();

                foreach (Module module in next.Modules)
                    scannedModules.Add(Scan(module));

                foreach (System subSystem in next.SubSystems)
                    systems.Enqueue(subSystem);
            } while (systems.Count > 0);

            return scannedModules;
        }

        private ScannedModule Scan(Module module)
        {
            Scanner scanner = new Scanner(module.File, ErrorLogger);
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
                throw new CompilerException("Error scanning module!");


            return new ScannedModule(module, tokens);
        }

        private List<ParsedModule> Parse(IEnumerable<ScannedModule> modules)
        {
            List<ParsedModule> parsedModules = new List<ParsedModule>();

            foreach (ScannedModule module in modules)
                parsedModules.Add(Parse(module));

            return parsedModules;
        }

        private ParsedModule Parse(ScannedModule module)
        {
            Parser parser = new Parser(module.Tokens, ErrorLogger);
            List<Statement> statements = parser.Parse();

            if (Options.Flags.HasFlag(CompilerFlags.PrintParse))
            {
                AstPrinter astPrinter = new AstPrinter();
                astPrinter.PrintStatements(statements);
            }

            if (ErrorLogger.HasErrors)
                throw new CompilerException("Error parsing module!");

            return new ParsedModule(module, statements);
        }

        private CheckedProject Check(Project project, IEnumerable<ParsedModule> parsedModules)
        {
            TypeChecker typeChecker = new TypeChecker(ErrorLogger);
            typeChecker.DefineNamespaces(project);
            GlobalScope globalScope = typeChecker.Check(parsedModules);

            if (ErrorLogger.HasErrors)
                throw new CompilerException("Error checking project!");

            return new CheckedProject(project, globalScope);
        }

        private void Assemble(CheckedProject project)
        {
            Assembler assembler = new Assembler(project.Name);
            assembler.AssembleProject(project);

            if (ErrorLogger.HasErrors)
                throw new CompilerException("Error assembling!");

            assembler.SaveAssembly();
        }
    }
}

// "C:\\Users\\DrewC\\source\\repos\\CobraCompiler\\CobraCompiler\\supportedcode.txt"
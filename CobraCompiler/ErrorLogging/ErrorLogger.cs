using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.ErrorLogging
{
    public class ErrorLogger
    {
        private readonly List<CompilingException> _errors;
        public Boolean HasErrors => _errors.Count > 0;

        public ErrorLogger()
        {
            _errors = new List<CompilingException>();
        }

        public void Log(CompilingException error)
        {
            _errors.Add(error);
        }

        public void DisplayErrors()
        {
            foreach (CompilingException error in _errors)
            {
                if (!error.isWarning)
                {
                    WriteWithColor("Error:\n", ConsoleColor.Red);
                    Console.WriteLine($"{error.FirstToken.SourceLocation}:");
                    Console.WriteLine(error.Message);
                    PrintErrorLine(error);

                    Console.WriteLine();
                }
            }
        }

        public void DisplayWarnings()
        {
            foreach (CompilingException error in _errors)
            {
                if (error.isWarning)
                {
                    WriteWithColor("Warning:\n", ConsoleColor.Yellow);
                    Console.WriteLine($"{error.FirstToken.SourceLocation}:");
                    Console.WriteLine(error.Message);
                    PrintLine(error.FirstToken);

                    Console.WriteLine();
                }
            }
        }

        private void PrintErrorLine(CompilingException error)
        {
            string line = Token.GetWholeLine(error.FirstToken);

            int errTextLen = error.LastToken.SourceLocation.CharIndex + error.LastToken.Lexeme.Length -
                             error.FirstToken.SourceLocation.CharIndex;

            string before = line.Substring(0, error.FirstToken.SourceLocation.CharIndex);
            string errText = line.Substring(error.FirstToken.SourceLocation.CharIndex, errTextLen);
            string after = line.Substring(error.FirstToken.SourceLocation.CharIndex + errTextLen);

            Console.Write($"{error.FirstToken.SourceLocation.Line}|");
            Console.Write(before);
            WriteWithColor(errText, ConsoleColor.Red);
            Console.WriteLine(after);
        }

        private void PrintLine(Token token)
        {
            Console.WriteLine($"{token.SourceLocation.Line}|{Token.GetWholeLine(token)}");
        }

        public static void WriteWithColor(String message, ConsoleColor color)
        {
            ConsoleColor old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ForegroundColor = old;
        }
    }
}

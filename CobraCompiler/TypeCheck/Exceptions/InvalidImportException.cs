using CobraCompiler.Parse.Statements;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class InvalidImportException : TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }

        public override bool isWarning => false;
        public InvalidImportException(ImportStatement import) : base($"Cannot import {import.Import.Type} because it is not a namespace")
        {
            FirstToken = import.Keyword;
            LastToken = import.Import.LastToken;
        }
    }
}

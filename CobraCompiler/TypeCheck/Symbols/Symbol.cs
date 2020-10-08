using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Statements;
using CobraCompiler.Scanning;
using CobraCompiler.TypeCheck.Symbols;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.TypeCheck
{
    class Symbol
    {
        public readonly string Lexeme;
        public readonly CobraType Type;
        public readonly Mutability Mutability;
        public readonly Statement Declaration;
        public IReadOnlyList<Expression> References => _references;

        private readonly List<Expression> _references;

        public Symbol(Statement declaration, CobraType type, Mutability mutability, string lexeme)
        {
            Declaration = declaration;
            Type = type;
            Mutability = mutability;
            Lexeme = lexeme;
            _references = new List<Expression>();
        }

        public void AddReference(Expression expression)
        {
            _references.Add(expression);
        }
    }
}

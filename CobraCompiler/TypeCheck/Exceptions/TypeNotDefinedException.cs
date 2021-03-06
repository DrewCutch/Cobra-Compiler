﻿using CobraCompiler.Parse.Expressions;
using CobraCompiler.Scanning;

namespace CobraCompiler.TypeCheck.Exceptions
{
    class TypeNotDefinedException: TypingException
    {
        public override Token FirstToken { get; }
        public override Token LastToken { get; }

        public override bool isWarning => false;
        public TypeNotDefinedException(TypeInitExpression typeInit) : base($"Type {typeInit.IdentifierStr} is not defined")
        {
            FirstToken = typeInit.FirstToken;
            LastToken = typeInit.LastToken;
        }
    }
}

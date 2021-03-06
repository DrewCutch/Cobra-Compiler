﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler
{
    public enum TokenType
    {
        // Single-character tokens. 
        Ampersand, Bar,
        LeftParen, RightParen, LeftBrace, RightBrace,
        LeftBracket, RightBracket, Comma, Dot, Minus,
        Plus, Slash, Star, QuestionMark,

        // One or two character tokens.                  
        Bang, BangEqual, BangColon,
        Colon, ColonColon,
        Equal, EqualEqual,
        Greater, GreaterEqual,
        Less, LessEqual,

        // Literals.                                     
        Identifier, String, Integer, Decimal,

        // Keywords.                                     
        And, Class, Else, False, Func, For, Guard, If, Import,
        In, Init, Null, Or, Op, Panic, Return, Super, True,
        Type, Var, Val, While,

        //Invalid
        Invalid,

        // File.
        NewLine, Eof
    }
}

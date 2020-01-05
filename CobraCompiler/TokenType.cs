﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler
{
    enum TokenType
    {
        // Single-character tokens.                      
        LeftParen, RightParen, LeftBrace, RightBrace,
        LeftBracket, RightBracket, Comma, Dot, Minus,
        Plus, Colon, Slash, Star,

        // One or two character tokens.                  
        Bang, BangEqual,
        Equal, EqualEqual,
        Greater, GreaterEqual,
        Less, LessEqual,

        // Literals.                                     
        Identifier, String, Integer, Decimal,

        // Keywords.                                     
        And, Class, Else, False, Func, For, If, In, Null, Or, Op,
        Return, Super, This, True, Var, While,

        //Invalid
        Invalid,

        // TypeName.
        Type,

        // File.
        NewLine, Eof
    }
}

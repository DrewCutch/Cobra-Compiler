﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.TypeCheck
{
    class InvalidOperationException: TypingException
    {
        public override bool isWarning => false;
        public InvalidOperationException(int lineNumber) : base("Cannot perform operation", lineNumber)
        {

        }
    }
}

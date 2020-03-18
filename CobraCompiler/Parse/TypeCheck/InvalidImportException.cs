using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.TypeCheck
{
    class InvalidImportException : TypingException
    {
        public override bool isWarning => false;
        public InvalidImportException(string type, int lineNumber) : base($"Cannot import {type}", lineNumber)
        {

        }
    }
}

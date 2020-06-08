using System;

namespace CobraCompiler.TypeCheck.Types
{
    abstract class CobraTypeBase
    {
        public readonly String Identifier;

        protected CobraTypeBase(string identifier)
        {
            Identifier = identifier;
        }
    }
}

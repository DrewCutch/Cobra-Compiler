using System;

namespace CobraCompiler.Scanning
{
    class StringNibbler
    {
        public int Pos { get; private set; }
        public readonly string Str;

        public StringNibbler(string str)
        {
            Pos = 0;
            Str = str;
        }

        /// <summary>
        /// Returns the next character in the string and advances the position in the string
        /// </summary>
        /// <returns>The next char</returns>
        public char Pop()
        {
            if (HasNext())
                return Str[Pos++];
            return '\0';
        }

        /// <summary>
        /// Returns the next character in the string
        /// </summary>
        public char Peek()
        {
            if(Pos < Str.Length)
                return Str[Pos];
            return '\0';
        }


        public Boolean HasNext()
        {
            return Pos < Str.Length;
        }
    }
}

using System;

namespace CobraCompiler.Scanning
{
    class StringNibbler
    {
        private int _pos;
        public readonly string Str;

        public StringNibbler(string str)
        {
            _pos = 0;
            Str = str;
        }

        /// <summary>
        /// Returns the next character in the string and advances the position in the string
        /// </summary>
        /// <returns>The next char</returns>
        public char Pop()
        {
            if (HasNext())
                return Str[_pos++];
            return '\0';
        }

        /// <summary>
        /// Returns the next character in the string
        /// </summary>
        public char Peek()
        {
            if(_pos < Str.Length)
                return Str[_pos];
            return '\0';
        }


        public Boolean HasNext()
        {
            return _pos < Str.Length;
        }
    }
}

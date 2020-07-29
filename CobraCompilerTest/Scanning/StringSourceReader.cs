using System;
using System.Collections.Generic;
using System.Text;
using CobraCompiler.Scanning;

namespace CobraCompilerTest.Scanning
{
    class StringSourceReader: SourceReader
    {
        public string Path => "TEST_SOURCE";

        private readonly string[] _lines;

        public StringSourceReader(string source)
        {
            _lines = new[] {source};
        }

        public StringSourceReader(string[] lines)
        {
            _lines = lines;
        }

        public IEnumerable<string> Lines()
        {
            return _lines;
        }
    }
}

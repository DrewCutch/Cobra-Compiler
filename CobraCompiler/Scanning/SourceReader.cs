using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Scanning
{
    public interface SourceReader
    {
        string Path { get; }
        IEnumerable<string> Lines();
    }
}

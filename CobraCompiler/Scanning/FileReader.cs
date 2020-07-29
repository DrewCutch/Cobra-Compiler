using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Scanning
{
    public class FileReader: SourceReader
    {
        public string Path { get; }

        public FileReader(string filePath)
        {
            Path = filePath;
        }

        public IEnumerable<string> Lines()
        {
            string line = String.Empty;
            using (StreamReader sr = File.OpenText(Path))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }
}

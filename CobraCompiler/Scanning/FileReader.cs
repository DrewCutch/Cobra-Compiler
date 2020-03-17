using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Scanning
{
    class FileReader
    {
        private string Path;

        public FileReader(string filePath)
        {
            Path = filePath;
        }

        public string GetLine(int n)
        {
            using (StreamReader sr = File.OpenText(Path))
            {
                for (int i = 0; i < n && !sr.EndOfStream; i++)
                {
                    sr.ReadLine();
                }

                return sr.ReadLine();
            }
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

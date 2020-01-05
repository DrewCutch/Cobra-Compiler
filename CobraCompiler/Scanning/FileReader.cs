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
        private StreamReader sr;

        public FileReader(string filePath)
        {
            sr = File.OpenText(filePath);
        }

        public string NextLine()
        {
            string line = sr.ReadLine();

            if (line != null)
                return line;

            sr.Close();

            return null;
        }

        public IEnumerable<string> Lines()
        {
            string line = String.Empty;
            while ((line = NextLine()) != null)
            {
                yield return line;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompilerTestFramework.Util
{
    public class OutputCapture : TextWriter
    {
        private readonly TextWriter _stdOutWriter;
        private readonly StringWriter _stringWriter;
        private readonly bool _blockPrint;

        public override Encoding Encoding => _stdOutWriter.Encoding;

        public String Captured => _stringWriter.ToString();

        public OutputCapture(bool blockPrint = false)
        {
            _blockPrint = blockPrint;
            _stdOutWriter = Console.Out;
            Console.SetOut(this);
            _stringWriter = new StringWriter();
        }

        public override void Write(string output)
        {
            _stringWriter.Write(output);
            if(!_blockPrint)
                _stdOutWriter.Write(output);
        }

        public override void WriteLine(string output)
        {
            _stringWriter.WriteLine(output);
            if(!_blockPrint)
                _stdOutWriter.WriteLine(output);
        }
    }

}

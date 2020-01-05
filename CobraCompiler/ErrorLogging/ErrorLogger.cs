using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.ErrorLogging
{
    class ErrorLogger
    {
        private readonly List<CompilingException> _errors;
        public Boolean HasErrors => _errors.Count > 0;

        public ErrorLogger()
        {
            _errors = new List<CompilingException>();
        }

        public void Log(CompilingException error)
        {
            _errors.Add(error);
        }

        public void DisplayErrors()
        {
            foreach (CompilingException error in _errors)
            {
                Console.WriteLine($"Line {error.LineNumber}: {error.Message}");
            }
        }
    }
}

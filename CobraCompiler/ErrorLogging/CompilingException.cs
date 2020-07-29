using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Scanning;

namespace CobraCompiler.ErrorLogging
{
    public abstract class CompilingException : Exception
    {
        public abstract Token FirstToken { get; }
        public abstract Token LastToken { get; }

        public abstract bool isWarning { get; }

        protected CompilingException(string lexeme, SourceLocation location)
        {

        }

        protected CompilingException()
        {
        }

        protected CompilingException(string message) : base(message)
        {
        }

        protected CompilingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CompilingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

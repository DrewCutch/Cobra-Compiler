using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.ErrorLogging
{
    abstract class CompilingException : Exception
    {
        public abstract int LineNumber { get; }
        public abstract bool isWarning { get; }

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

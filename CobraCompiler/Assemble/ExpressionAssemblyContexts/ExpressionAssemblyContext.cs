using System.Reflection;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Assemble.ExpressionAssemblyContexts
{
    class ExpressionAssemblyContext
    {
        public readonly CobraType Type;
        public readonly FieldInfo AssignToField;
        public bool AssigningToField => AssignToField != null;

        public ExpressionAssemblyContext(CobraType type)
        {
            Type = type;
            AssignToField = null;
        }

        public ExpressionAssemblyContext(CobraType type, FieldInfo assignToField)
        {
            Type = type;
            AssignToField = assignToField;
        }
    }
}

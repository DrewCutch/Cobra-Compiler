using System.Reflection;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Assemble.ExpressionAssemblyContexts
{
    class ExpressionAssemblyContext
    {
        public readonly CobraType Type;
        public readonly FieldInfo AssignToField;
        public readonly MethodInfo AssignToIndex;
        public bool AssigningToField => AssignToField != null;
        public bool AssigningToIndex => AssignToIndex != null;

        public ExpressionAssemblyContext(CobraType type)
        {
            Type = type;
            AssignToField = null;
            AssignToIndex = null;
        }

        public ExpressionAssemblyContext(CobraType type, FieldInfo assignToField)
        {
            Type = type;
            AssignToField = assignToField;
            AssignToIndex = null;
        }

        public ExpressionAssemblyContext(CobraType type, MethodInfo assignToIndex)
        {
            Type = type;
            AssignToField = null;
            AssignToIndex = assignToIndex;
        }
    }
}

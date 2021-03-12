using System.Reflection;
using System.Reflection.Emit;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Assemble.ExpressionAssemblyContexts
{
    class ExpressionAssemblyContext
    {
        public readonly CobraType Type;
        public readonly FieldInfo AssignToField;
        public readonly MethodInfo AssignToIndex;
        public Label? NullCheckEndLabel { get; protected set; }
        public bool AssigningToField => AssignToField != null;
        public bool AssigningToIndex => AssignToIndex != null;
        public bool InNullCheck => NullCheckEndLabel != null;

        public ExpressionAssemblyContext(CobraType type)
        {
            Type = type;
            AssignToField = null;
            AssignToIndex = null;
            NullCheckEndLabel = null;
        }

        public ExpressionAssemblyContext(CobraType type, FieldInfo assignToField)
        {
            Type = type;
            AssignToField = assignToField;
            AssignToIndex = null;
            NullCheckEndLabel = null;
        }

        public ExpressionAssemblyContext(CobraType type, MethodInfo assignToIndex)
        {
            Type = type;
            AssignToField = null;
            AssignToIndex = assignToIndex;
            NullCheckEndLabel = null;
        }

        public ExpressionAssemblyContext(CobraType type, Label? nullCheckEndLabel)
        {
            Type = type;
            AssignToField = null;
            AssignToIndex = null;
            NullCheckEndLabel = nullCheckEndLabel;
        }
    }
}

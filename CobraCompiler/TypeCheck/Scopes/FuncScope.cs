using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using CobraCompiler.Parse.CFG;
using CobraCompiler.Parse.Statements;
using CobraCompiler.TypeCheck.Types;

namespace CobraCompiler.Parse.Scopes
{
    class FuncScope: Scope
    {
        public readonly IReadOnlyList<(string, CobraType)> Params;
        public readonly CobraType ReturnType;
        public FuncDeclarationStatement FuncDeclaration { get; }
        public readonly CobraType FuncType;

        public readonly CFGNode CFGRoot;
        public readonly CFGNode CFGTerminal;

        public bool Returns => CFGRoot.FulfilledByChildren(node => node.Next.Count == 1 && node.Next[0] == CFGTerminal);

        public FuncScope(Scope parentScope, FuncDeclarationStatement funcDeclaration, IEnumerable<(string, CobraType)> parameters, CobraType returnType) : base(parentScope, funcDeclaration.Body)
        {
            Params = new List<(string, CobraType)>(parameters);
            ReturnType = returnType;
            FuncDeclaration = funcDeclaration;
            CFGRoot = new CFGNode(this);
            CFGTerminal = new CFGNode(this);

            List<CobraType> funcTypeArgs = Params.Select(x => x.Item2).ToList();
            funcTypeArgs.Add(returnType);

            FuncType = DotNetCobraGeneric.FuncType.CreateGenericInstance(funcTypeArgs);
        }

        public virtual int GetParamPosition(string paramName)
        {
            if (Parent is ClassScope && paramName == "this")
            {
                return 0;
            }

            for (int i = 0; i < Params.Count; i++)
            {
                if (paramName == Params[i].Item1)
                    return i + (Parent is ClassScope ? 1 : 0);
            }

            return -1;
        }

        public override CobraType GetReturnType()
        {
            return ReturnType;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.TypeCheck.CFG;
using CobraCompiler.Util;

namespace CobraCompiler.Parse.CFG
{
    class CFGNode
    {
        public readonly Scope Scope;
        public IReadOnlyList<Statement> Statements => _statements;
        protected List<Statement> _statements;

        public bool IsTerminal => Index == 0;

        public bool IsRoot => Index == 1;

        public readonly CFGraph Graph;
        internal readonly int Index;

        public IEnumerable<CFGNode> Next => Graph.GetNext(this);
        public IEnumerable<CFGNode> Previous => Graph.GetPrevious(this);

        internal CFGNode(Scope scope, CFGraph graph, int index)
        {
            Scope = scope;
            Graph = graph;
            Index = index;

            _statements = new List<Statement>();
        }

        public static CFGNode CreateDummyNode(Scope scope)
        {
            return new CFGNode(scope, null, -1);
        }

        public CFGNode CreateNext(Scope scope)
        {
            CFGNode newNode = Graph.AddNode(scope);

            Link(newNode);

            return newNode;
        }

        public void Link(CFGNode next)
        {
            Graph.AddLink(this, next);
        }

        public void SetNext(CFGNode next)
        {
            Graph.SetChild(this, next);
        }

        public void AddStatement(Statement statement)
        {
            _statements.Add(statement);
        }

        public bool FulfilledByAncestors(Func<CFGNode, bool> predicate)
        {
            if (predicate(this))
                return true;

            if (Previous.Empty())
                return false;

            return Graph.GetPrevious(this).All(previous => previous.FulfilledByAncestors(predicate));
        }

        public bool FulfilledByChildren(Func<CFGNode, bool> predicate)
        {
            if (predicate(this))
                return true;

            if (Next.Empty())
                return false;

            return Next.All(next => next.FulfilledByChildren(predicate));
        }

        public bool FulfilledByAnyChildren(Func<CFGNode, bool> predicate)
        {
            if (predicate(this))
                return true;

            if (Next.Empty())
                return false;

            return Next.Any(next => next.FulfilledByAnyChildren(predicate));
        }
    }
}

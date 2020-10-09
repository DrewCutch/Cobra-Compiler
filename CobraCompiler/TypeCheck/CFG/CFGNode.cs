using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Expressions;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;
using CobraCompiler.TypeCheck;
using CobraCompiler.TypeCheck.CFG;
using CobraCompiler.Util;

namespace CobraCompiler.Parse.CFG
{
    class CFGNode
    {
        public readonly Scope Scope;
        public IReadOnlyList<Statement> Statements => _statements;
        protected List<Statement> _statements;

        public IReadOnlyDictionary<Symbol, List<AssignExpression>> Assignments => _assignments;
        private readonly Dictionary<Symbol, List<AssignExpression>> _assignments;

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
            _assignments = new Dictionary<Symbol, List<AssignExpression>>();
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

        public void AddAssignment(Symbol symbol, AssignExpression assignExpression)
        {
            if(!_assignments.ContainsKey(symbol))
                _assignments[symbol] = new List<AssignExpression>();

            _assignments[symbol].Add(assignExpression);
        }

        public bool FulfilledByAncestors(Func<CFGNode, bool> predicate)
        {
            if (predicate(this))
                return true;

            if (Previous.Empty())
                return false;

            return Graph.GetPrevious(this).All(previous => previous.FulfilledByAncestors(predicate));
        }

        public bool FulfilledByAnyAncestors(Func<CFGNode, bool> predicate)
        {
            if (predicate(this))
                return true;

            if (Previous.Empty())
                return false;

            return Graph.GetPrevious(this).Any(previous => previous.FulfilledByAnyAncestors(predicate));
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

        private bool IsAssigned(Symbol symbol)
        {
            return Assignments.ContainsKey(symbol);
        }

        public bool IsEverAssigned(Symbol symbol)
        {
            return FulfilledByAnyAncestors(node => node.IsAssigned(symbol));
        }

        public bool IsAlwaysAssigned(Symbol symbol)
        {
            return FulfilledByAncestors(node => node.IsAssigned(symbol));
        }
    }
}

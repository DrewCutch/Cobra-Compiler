using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;

namespace CobraCompiler.Parse.CFG
{
    class CFGNode
    {
        public readonly Scope Scope;
        public IReadOnlyList<Statement> Statements => _statements;
        protected List<Statement> _statements;

        public bool IsTerminal => _next.Count == 0;
        public IReadOnlyList<CFGNode> Next => _next;
        protected List<CFGNode> _next;

        public bool IsRoot => _previous.Count == 0;
        public IReadOnlyList<CFGNode> Previous => _previous;
        protected List<CFGNode> _previous;

        public CFGNode(Scope scope)
        {
            Scope = scope;
            _next = new List<CFGNode>();
            _previous = new List<CFGNode>();
            _statements = new List<Statement>();
        }

        public static void Link(CFGNode previous, CFGNode next)
        {
            previous._next.Add(next);
            next._previous.Add(previous);
        }

        // This can be improved using a topological sort
        public static List<CFGNode> LinearNodes(CFGNode root)
        {
            Queue<CFGNode> pendingNodes = new Queue<CFGNode>();
            HashSet<CFGNode> solvedNodes = new HashSet<CFGNode>();
            List<CFGNode> linearNodes = new List<CFGNode>();

            pendingNodes.Enqueue(root);

            while (pendingNodes.Count != 0)
            {
                CFGNode next = pendingNodes.Dequeue();
                if (solvedNodes.Contains(next))
                    continue;

                if (next.Previous.All(previous => solvedNodes.Contains(previous)))
                {
                    linearNodes.Add(next);
                    solvedNodes.Add(next);

                    foreach (CFGNode node in next.Next)
                    {
                        pendingNodes.Enqueue(node);
                    }
                }
                else
                {
                    pendingNodes.Enqueue(next);
                }
            }

            return linearNodes;
        }

        public void SetNext(CFGNode next)
        {
            _next.Clear();
            _next.Add(next);
            next._previous.Add(next);
        }

        public void AddStatement(Statement statement)
        {
            _statements.Add(statement);
        }

        public bool FulfilledByAncestors(Func<CFGNode, bool> predicate)
        {
            if (predicate(this))
                return true;

            if (IsRoot)
                return false;

            return _previous.All(previous => previous.FulfilledByAncestors(predicate));
        }

        public bool FulfilledByChildren(Func<CFGNode, bool> predicate)
        {
            if (predicate(this))
                return true;

            if (IsTerminal)
                return false;

            return _next.All(next => next.FulfilledByChildren(predicate));
        }

        public bool FulfilledByAnyChildren(Func<CFGNode, bool> predicate)
        {
            if (predicate(this))
                return true;

            if (IsTerminal)
                return false;

            return _next.Any(next => next.FulfilledByAnyChildren(predicate));
        }

        public CFGNode GetRoot()
        {
            if (IsRoot)
                return this;

            return _previous[0].GetRoot();
        }

        public CFGNode GetTerminal()
        {
            if (IsTerminal)
                return this;

            return _next[0].GetTerminal();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Parse.Statements;

namespace CobraCompiler.Parse.CFG
{
    class CFGNode
    {
        public readonly Scope Scope;
        
        public IReadOnlyList<CFGNode> Next => _next;
        protected List<CFGNode> _next;

        public IReadOnlyList<CFGNode> Previous => _previous;
        protected List<CFGNode> _previous;

        public CFGNode(Scope scope)
        {
            Scope = scope;
            _next = new List<CFGNode>();
            _previous = new List<CFGNode>();
        }

        public void AddNext(CFGNode next)
        {
            _next.Add(next);
        }

        public void AddPrevious(CFGNode previous)
        {
            _previous.Add(previous);
        }

        public bool FulfilledByAncestors(Func<CFGNode, bool> predicate)
        {
            if (predicate(this))
                return true;

            if (_previous.Count == 0)
                return false;

            return _previous.All(previous => previous.FulfilledByAncestors(predicate));
        }

        public CFGNode GetRoot()
        {
            if (_previous.Count == 0)
                return this;

            return _previous[0].GetRoot();
        }

        public CFGNode GetTerminal()
        {
            if (_next.Count == 0)
                return this;

            return _next[0].GetTerminal();
        }
    }
}

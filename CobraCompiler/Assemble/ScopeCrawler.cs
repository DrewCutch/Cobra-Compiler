using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CobraCompiler.Parse.Scopes;

namespace CobraCompiler.Assemble
{
    class ScopeCrawler
    {
        public readonly Scope Root;
       
        public Scope Current { get; private set; }
        private Stack<int> _subScope;
        private int CurrentSubScope => _subScope.Peek();

        public ScopeCrawler(Scope root)
        {
            Root = root;
            Reset();
        }

        public void Reset()
        {
            Current = Root;
            _subScope = new Stack<int>();
            _subScope.Push(0);
        }

        public bool EnterScope()
        {
            if (CurrentSubScope >= Current.SubScopes.Count)
            {
                return false;
            }

            Current = Current.SubScopes[CurrentSubScope];

            int next = _subScope.Pop() + 1;
            _subScope.Push(next);

            _subScope.Push(0);
            return true;
        }

        public void ExitScope()
        {
            Current = Current.Parent;
            _subScope.Pop();
        }
    }
}


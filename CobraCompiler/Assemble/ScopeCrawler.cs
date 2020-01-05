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
        public Scope Current => _currentEnumerable.Current;
        private IEnumerator<Scope> _currentEnumerable;

        public ScopeCrawler(Scope root)
        {
            Root = root;

            Reset();
        }

        public void Reset()
        {
            _currentEnumerable = Scopes().GetEnumerator();
        }

        public bool Advance()
        {
            return _currentEnumerable.MoveNext();
        }

        private IEnumerable<Scope> Scopes()
        {
            Stack<Scope> scopes = new Stack<Scope>();
            scopes.Push(Root);

            while (scopes.Count > 0)
            {
                Scope next = scopes.Pop();
                yield return next;

                for (int i = next.SubScopes.Count - 1; i >= 0; i--)
                    scopes.Push(next.SubScopes[i]);
            }
        }
    }
}


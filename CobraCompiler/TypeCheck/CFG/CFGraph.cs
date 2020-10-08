using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.CFG;
using CobraCompiler.Parse.Scopes;

namespace CobraCompiler.TypeCheck.CFG
{
    class CFGraph
    {
        private readonly EdgeMatrix _edgeMatrix;

        public CFGNode Root => _cfgNodes[1];
        public CFGNode Terminal => _cfgNodes[0];

        public IReadOnlyList<CFGNode> CFGNodes => _cfgNodes;
        private readonly List<CFGNode> _cfgNodes;

        public CFGraph(Scope scope)
        {
            _edgeMatrix = new EdgeMatrix();
            _cfgNodes = new List<CFGNode>();

            AddNode(scope); // Terminal
            AddNode(scope); // Root
        }

        public CFGNode AddNode(Scope scope)
        {
            int nodeIndex = _edgeMatrix.AddNode();

            CFGNode newNode = new CFGNode(scope, this, nodeIndex);

            _cfgNodes.Add(newNode);

            return newNode;
        }

        public void AddLink(CFGNode node1, CFGNode node2)
        {
            _edgeMatrix.AddDirectedEdge(node1.Index, node2.Index);
        }

        public void SetChild(CFGNode node1, CFGNode node2)
        {
            _edgeMatrix.RemoveChildren(node1.Index);
            _edgeMatrix.AddDirectedEdge(node1.Index, node2.Index);
        }

        internal IEnumerable<CFGNode> GetNext(CFGNode node)
        {
            return _edgeMatrix.GetChildren(node.Index)
                .Select(nodeIndex => _cfgNodes[nodeIndex]);
        }

        internal IEnumerable<CFGNode> GetPrevious(CFGNode node)
        {
            return _edgeMatrix.GetParents(node.Index)
                .Select(nodeIndex => _cfgNodes[nodeIndex]);
        }
    }
}

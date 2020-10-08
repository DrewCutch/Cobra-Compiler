using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.TypeCheck.CFG
{
    public class EdgeMatrix
    {
        // [from, to]
        private BitMatrix _bitMatrix;
        public int NumberOfNodes { get; private set; }

        public EdgeMatrix()
        {
            _bitMatrix = new BitMatrix(8, 8);
        }

        public int AddNode()
        {
            if(NumberOfNodes == _bitMatrix.Width)
                ResizeBitMatrix();

            int nodeIndex = NumberOfNodes;

            NumberOfNodes += 1;

            return nodeIndex;
        }

        private void ResizeBitMatrix()
        {
            BitMatrix newMatrix = new BitMatrix(_bitMatrix.Width * 2, _bitMatrix.Height * 2);
            for (int i = 0; i < _bitMatrix.Height; i++)
                for (int j = 0; j < _bitMatrix.Width; j++)
                {
                    newMatrix[i, j] = _bitMatrix[i, j];
                }

            _bitMatrix = newMatrix;
        }

        public void AddBidirectionalEdge(int node1, int node2)
        {
            AddDirectedEdge(node1, node2);
            AddDirectedEdge(node2, node1);
        }

        public void AddDirectedEdge(int from, int to)
        {
            if(from >= NumberOfNodes)
                throw new ArgumentException("Node index must be less than the number of nodes", nameof(from));
            if (to >= NumberOfNodes)
                throw new ArgumentException("Node index must be less than the number of nodes", nameof(to));

            _bitMatrix[from, to] = true;
        }

        public bool EdgeExists(int from, int to)
        {
            if (from >= NumberOfNodes)
                throw new ArgumentException("Node index must be less than the number of nodes", nameof(to));
            if (from >= NumberOfNodes)
                throw new ArgumentException("Node index must be less than the number of nodes", nameof(to));

            return _bitMatrix[from, to];
        }

        public void RemoveChildren(int node)
        {
            for (int i = 0; i < _bitMatrix.Width; i++)
                _bitMatrix[node, i] = false;
        }

        public void RemoveParents(int node)
        {
            for (int i = 0; i < _bitMatrix.Width; i++)
                _bitMatrix[i, node] = false;
        }


        public IEnumerable<int> GetChildren(int node)
        {
            for (int i = 0; i < _bitMatrix.Width; i++)
                if (_bitMatrix[node, i])
                    yield return i;
        }

        public IEnumerable<int> GetParents(int node)
        {
            for (int i = 0; i < _bitMatrix.Width; i++)
                if (_bitMatrix[i, node])
                    yield return i;
        }
    }
}

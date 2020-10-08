using System;
using System.Collections.Generic;
using System.Linq;
using CobraCompiler.TypeCheck.CFG;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CobraCompilerTestFramework.TypeCheck.CFG
{
    [TestClass]
    public class EdgeMatrixTest
    {
        [TestMethod]
        public void TestAddNodes()
        {
            EdgeMatrix graph = new EdgeMatrix();

            int nodeA = graph.AddNode();
            int nodeB = graph.AddNode();
            int nodeC = graph.AddNode();

            Assert.AreEqual(3, graph.NumberOfNodes);
        }

        [TestMethod]
        public void TestEdges()
        {
            EdgeMatrix graph = new EdgeMatrix();

            int nodeA = graph.AddNode();
            int nodeB = graph.AddNode();
            int nodeC = graph.AddNode();
            
            graph.AddDirectedEdge(nodeA, nodeB);
            graph.AddDirectedEdge(nodeA, nodeC);
            graph.AddDirectedEdge(nodeB, nodeC);

            Assert.IsTrue(graph.EdgeExists(nodeA, nodeB));
            Assert.IsTrue(graph.EdgeExists(nodeA, nodeC));
            Assert.IsTrue(graph.EdgeExists(nodeB, nodeC));
        }

        [TestMethod]
        public void TestGetChildren()
        {
            EdgeMatrix graph = new EdgeMatrix();

            int nodeA = graph.AddNode();
            int nodeB = graph.AddNode();
            int nodeC = graph.AddNode();

            graph.AddDirectedEdge(nodeA, nodeB);
            graph.AddDirectedEdge(nodeA, nodeC);
            graph.AddDirectedEdge(nodeB, nodeC);

            List<int> children = graph.GetChildren(nodeA).ToList();

            Assert.AreEqual(2, children.Count);
            Assert.IsTrue(children.Contains(nodeB));
            Assert.IsTrue(children.Contains(nodeC));
        }

        [TestMethod]
        public void TestGrow()
        {
            EdgeMatrix graph = new EdgeMatrix();

            int numNodes = 100;

            for (int i = 0; i < numNodes; i++)
            {
                graph.AddNode();
            }

            Assert.AreEqual(numNodes, graph.NumberOfNodes);
        }
    }
}

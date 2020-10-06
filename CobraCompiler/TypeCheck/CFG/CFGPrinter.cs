using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CobraCompiler.Parse.CFG;
using CobraCompiler.Parse.PrettyPrint;
using CobraCompiler.Parse.Statements;

namespace CobraCompiler.TypeCheck.CFG
{
    class CFGPrinter
    {
        public void PrintCFG(CFGNode root)
        {
            List<CFGNode> allNodes = CFGNode.LinearNodes(root);

            foreach (CFGNode node in allNodes)
            {
                PrintNode(node);
            }
        }

        private void PrintNode(CFGNode node)
        {
            Console.WriteLine("\n-------------------------");
            AstPrinter astPrinter = new AstPrinter();
            astPrinter.PrintStatements(node.Statements);
            Console.WriteLine("-------------------------\n");
        }
    }
}

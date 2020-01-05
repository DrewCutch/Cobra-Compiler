using System;
using System.Text;

namespace CobraCompiler.Parse.PrettyPrint
{
    class TreePrinter
    {
        private const string Cross = " ├─";
        private const string Corner = " └─";
        private const string Vertical = " │ ";
        private const string Space = "   ";

        private StringBuilder _diagram;
        private StringBuilder _indent;

        public String Diagram => _diagram.ToString();

        public TreePrinter()
        {
            Reset();
        }

        public void Reset()
        {
            _diagram = new StringBuilder();
            _indent = new StringBuilder();
        }

        public void AddNode(string str, bool isLast)
        {
            AddLeaf(str, isLast);
            if (isLast)
                _indent.Append(Space);
            else
                _indent.Append(Vertical);
        }

        public void AddLeaf(string str, bool isLast)
        {
            _diagram.Append(_indent);

            if (isLast)
                _diagram.Append(Corner);
            else
                _diagram.Append(Cross);
            
            _diagram.AppendLine(str);
        }

        public void ExitNode()
        {
            _indent.Length -= Space.Length;
        }
    }
}

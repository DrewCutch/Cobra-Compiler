using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Parse.TypeCheck.Types
{
    class NamespaceType: CobraType
    {
        private readonly Dictionary<string, CobraType> definitions;
        private readonly string name;

        public NamespaceType(string nameSpace) : base("NamespaceLabel")
        {
            this.name = nameSpace;
            definitions = new Dictionary<string, CobraType>();
        }

        public NamespaceType AddSubNameSpace(string id)
        {
            NamespaceType subNamespace = new NamespaceType(name + "." + id);
            AddDefinition(id, subNamespace);

            return subNamespace;
        }

        public void AddDefinition(string id, CobraType type)
        {
            definitions[id] = type;
        }

        public bool HasType(string id)
        {
            return definitions.ContainsKey(id);
        }

        public string ResolveName(string id)
        {
            return name + "." + id;
        }

        public CobraType GetType(string id)
        {
            return definitions[id];
        }
    }
}

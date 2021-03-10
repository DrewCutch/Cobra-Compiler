using System.Collections.Generic;

namespace CobraCompiler.TypeCheck.Types
{
    class NamespaceType: CobraType
    {
        private readonly Dictionary<string, CobraType> definitions;
        private readonly string name;

        public NamespaceType(string nameSpace) : 
            base("NamespaceLabel", new List<CobraType>(), false, new List<CobraType>(), new List<CobraType>(), null ,-1, null)
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

        protected bool Equals(NamespaceType other)
        {
            return string.Equals(name, other.name);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((NamespaceType) obj);
        }

        public override int GetHashCode()
        {
            return (name != null ? name.GetHashCode() : 0);
        }
    }
}

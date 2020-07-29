namespace CobraCompiler.TypeCheck.Types
{
    class GenericTypeParamPlaceholder: CobraType
    {
        public readonly int Index;

        public GenericTypeParamPlaceholder(string identifier, int index) : base(identifier)
        {
            Index = index;
        }

        public override bool Equals(object obj)
        {
            if (obj is GenericTypeParamPlaceholder other)
                return Identifier == other.Identifier;

            return false;
        }

        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }
    }
}

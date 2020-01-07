namespace CobraCompiler.Parse.TypeCheck.Types
{
    abstract class CobraType
    {
        public readonly string Identifier;

        protected CobraType(string identifier)
        {
            Identifier = identifier;
        }

        public virtual bool CanImplicitCast(CobraType other)
        {
            return this.Equals(other);
        }
    }
}

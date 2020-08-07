namespace CobraCompiler.Compiler
{
    public readonly struct CompilationOptions
    {
        public readonly string FilePath;
        public readonly CompilerFlags Flags;

        public CompilationOptions(string filePath, CompilerFlags flags)
        {
            FilePath = filePath;
            Flags = flags;
        }
    }
}

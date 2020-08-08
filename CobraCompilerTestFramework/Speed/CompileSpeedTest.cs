using System;
using System.Collections.Generic;
using System.Text;
using CobraCompiler.Compiler;
using CobraCompiler.ErrorLogging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CobraCompilerTestFramework.Speed
{
    [TestClass]
    public class CompileSpeedTest
    {
        private const string basePath =
            "C:\\Users\\DrewC\\source\\repos\\CobraCompiler\\CobraCompilerTestFramework\\Speed";

        [TestMethod]
        public void CompileSpeedTest1()
        {
            string path = basePath + "\\BigProject";
            CompilationResults results = Compile(path);
            Assert.IsTrue(results.TotalDuration < 1.0);
        }

        private CompilationResults Compile(string path)
        {
            CompilationOptions options = new CompilationOptions(path, CompilerFlags.None);

            ProjectReader projectReader = new ProjectReader(options.FilePath);
            Project project = projectReader.ReadProject();

            ErrorLogger errorLogger = new ErrorLogger();

            Compiler compiler = new Compiler(options, errorLogger);

            return compiler.Compile(project);
        }
    }
}

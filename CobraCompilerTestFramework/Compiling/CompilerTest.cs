using System;
using System.Reflection;
using CobraCompiler.Compiler;
using CobraCompiler.ErrorLogging;
using CobraCompiler.Scanning;
using CobraCompilerTestFramework.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CobraCompilerTestFramework.Compiling
{
    [TestClass]
    public class CompilerTest
    {
        private const string BaseAssPath = "C:\\Users\\DrewC\\source\\repos\\CobraCompiler\\CobraCompilerTestFramework\\bin\\Debug\\";
        private const string BasePath = "C:\\Users\\DrewC\\source\\repos\\CobraCompiler\\CobraCompilerTestFramework\\Compiling\\Source\\";
        private string AssemblyPath;

        [TestMethod]
        public void TestCompilerHelloWorld()
        {
            string output = CompileAndGetOutput("HelloWorld");

            Assert.AreEqual("Hello world\r\n", output);
        }

        [TestMethod]
        public void TestCompilerFunc()
        {
            string output = CompileAndGetOutput("FuncTest");

            Assert.AreEqual("35863", output);
        }

        [TestMethod]
        public void TestCompilerFirstClassFunc()
        {
            string output = CompileAndGetOutput("FirstClassFunc");

            Assert.AreEqual("64", output);
        }

        private string CompileAndGetOutput(string fileName)
        {
            string fullPath = BasePath + fileName + ".cobra";
            ErrorLogger errorLogger = new ErrorLogger();
            Compiler compiler = new Compiler(new CompilationOptions(fullPath, CompilerFlags.HideWarnings), errorLogger);

            try
            {
                compiler.Compile(new SingleFileProject(fileName, new FileReader(fullPath)));
            }
            catch (CompilerException)
            {
                errorLogger.DisplayErrors();

                return null;
            }
            
            Assembly assembly = Assembly.LoadFile(BaseAssPath + fileName + ".exe");

            OutputCapture capture = new OutputCapture();
            Console.SetOut(capture);

            assembly.EntryPoint.Invoke(null, new object[] { });

            return capture.Captured;
        }
    }
}

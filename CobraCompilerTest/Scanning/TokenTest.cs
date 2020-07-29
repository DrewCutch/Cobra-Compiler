using System;
using System.Collections.Generic;
using System.Text;
using CobraCompiler.ErrorLogging;
using NUnit.Framework;
using CobraCompiler.Scanning;

namespace CobraCompilerTest.Scanning
{
    public class TokenTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestStringifySingleToken()
        {
            string source = "var";

            List<Token> tokens = scan(source);
            Assert.AreEqual(source + '\n', Token.Stringify(tokens.ToArray()));
        }

        [Test]
        public void TestStringifySingleLine()
        {
            string source = "var i: int";

            List<Token> tokens = scan(source);
            Assert.AreEqual(source + '\n', Token.Stringify(tokens.ToArray()));
        }

        [Test]
        public void TestStringifyMultiLine()
        {
            string source = "var i: int = 10\n" +
                            "var j: int = 5\n" +
                            "var k: int = i + j";

            List<Token> tokens = scan(source);
            Assert.AreEqual(source + '\n', Token.Stringify(tokens.ToArray()));
        }

        [Test]
        public void TestGetWholeLineSingleToken()
        {
            string source = "var";

            List<Token> tokens = scan(source);
            Token center = tokens[0];

            Assert.AreEqual(source, Token.GetWholeLine(center));
        }

        [Test]
        public void TestGetWholeLineSingleLine()
        {
            string source = "var i: int";

            List<Token> tokens = scan(source);
            Token center = tokens[3];

            Assert.AreEqual(source, Token.GetWholeLine(center));
        }

        [Test]
        public void TestGetWholeLineMultiLine()
        {
            string source = "var i: int = 10\n" +
                            "var j: int = 5\n" +
                            "var k: int = i + j";

            List<Token> tokens = scan(source);
            Token center = tokens[7];

            Assert.AreEqual("var j: int = 5", Token.GetWholeLine(center));
        }

        private List<Token> scan(string source)
        {
            string[] lines = source.Split('\n');
            Scanner scanner = new Scanner(new StringSourceReader(lines), new ErrorLogger());
            return scanner.GetTokens();
        }
    }
}

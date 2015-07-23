using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace PropertyExpression.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        [TestMethod]
        public void TestNoDiagnosticsShown()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }
        
        [TestMethod]
        public void TestCodeRewrittenToUseNameof()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class Testclass
        {
            public Testclass()
            {
                var value = PropertyUtil.GetName<Person>(x => x.Name);
                Console.WriteLine(value);
            }
        }

        class Person
        {
            public string Name { get; set; }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = PropertyExpressionAnalyzer.DiagnosticId,
                Message = String.Format("Property expression can be translated to nameof({0}.{1})", "Person", "Name"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 15, 29)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class Testclass
        {
            public Testclass()
            {
                var value = nameof(Person.Name);
                Console.WriteLine(value);
            }
        }

        class Person
        {
            public string Name { get; set; }
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new PropertyExpressionCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new PropertyExpressionAnalyzer();
        }
    }
}
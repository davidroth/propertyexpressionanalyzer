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
        public void TestRewrite_TypeQualifierUsed_IfNotWithinSameTypeScope()
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
        
        [TestMethod]
        public void TestRewrite_PartialNamespace_And_TypeQualifierIsUsed_If_Member_With_TypeName_Exists_In_AncestorType()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1.Samples
    {
        class User
        {
            public string Forename { get; set; }
        }

        class Person
        {
            public Person()
            {
                Member = PropertyUtil.GetName<User>(x => x.Forename);
            }
            public string Member { get; set; }

            public string User { get; set; }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = PropertyExpressionAnalyzer.DiagnosticId,
                Message = string.Format("Property expression can be translated to nameof({0}.{1})", "User", "Forename"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 20, 26)
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

    namespace ConsoleApplication1.Samples
    {
        class User
        {
            public string Forename { get; set; }
        }

        class Person
        {
            public Person()
            {
                Member = nameof(Samples.User.Forename);
            }
            public string Member { get; set; }

            public string User { get; set; }
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }


        [TestMethod]
        public void TestRewrite_FullNamespace_And_TypeQualifierIsUsed_If_Member_With_TypeName_Exists_In_AncestorType()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

   namespace MyOtherNamespace.Samples.Test
    {
        class User
        {
            public string Forename { get; set; }
        }
    }

    namespace ConsoleApplication1.Samples
    {
        using MyOtherNamespace.Samples.Test;
        
        class Person
        {
            public Person()
            {
                Member = PropertyUtil.GetName<User>(x => x.Forename);
            }
            public string Member { get; set; }

            public string User { get; set; }
        }
    }";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

   namespace MyOtherNamespace.Samples.Test
    {
        class User
        {
            public string Forename { get; set; }
        }
    }

    namespace ConsoleApplication1.Samples
    {
        using MyOtherNamespace.Samples.Test;
        
        class Person
        {
            public Person()
            {
                Member = nameof(MyOtherNamespace.Samples.Test.User.Forename);
            }
            public string Member { get; set; }

            public string User { get; set; }
        }
    }";
            VerifyCSharpFix(test, fixtest, allowNewCompilerDiagnostics: true);
        }

        [TestMethod]
        public void TestRewrite_FullNamespace_And_TypeQualifierIsUsed_If_Member_With_TypeName_Exists_In_ParentOf_AncestorType()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

   namespace MyOtherNamespace.Samples.Test
    {
        class User
        {
            public string Forename { get; set; }
        }
    }

    namespace ConsoleApplication1.Samples
    {
        using MyOtherNamespace.Samples.Test;

        class PersonBase
        {
            public string User { get; set; }
        }        

        class Person : PersonBase
        {
            public Person()
            {
                Member = PropertyUtil.GetName<User>(x => x.Forename);
            }
            public string Member { get; set; }
        }
    }";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

   namespace MyOtherNamespace.Samples.Test
    {
        class User
        {
            public string Forename { get; set; }
        }
    }

    namespace ConsoleApplication1.Samples
    {
        using MyOtherNamespace.Samples.Test;

        class PersonBase
        {
            public string User { get; set; }
        }        

        class Person : PersonBase
        {
            public Person()
            {
                Member = nameof(MyOtherNamespace.Samples.Test.User.Forename);
            }
            public string Member { get; set; }
        }
    }";
            VerifyCSharpFix(test, fixtest, allowNewCompilerDiagnostics: true);
        }

        [TestMethod]
        public void TestRewrite_WithPath_FullNamespace_And_TypeQualifierIsUsed_If_Member_With_TypeName_Exists_In_AncestorType()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

   namespace MyOtherNamespace.Samples.Test
    {
        class Person
        {
            public string Id { get; set; }
        }

        class User
        {
            public string Forename { get; set; }
            public Person Person { get; set; }
        }
    }

    namespace ConsoleApplication1.Samples
    {
        using MyOtherNamespace.Samples.Test;
        
        class Person
        {
            public Person()
            {
                Member = PropertyUtil.GetName<User>(x => x.Person.Id);
            }
            public string Member { get; set; }

            public string User { get; set; }
        }
    }";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

   namespace MyOtherNamespace.Samples.Test
    {
        class Person
        {
            public string Id { get; set; }
        }

        class User
        {
            public string Forename { get; set; }
            public Person Person { get; set; }
        }
    }

    namespace ConsoleApplication1.Samples
    {
        using MyOtherNamespace.Samples.Test;
        
        class Person
        {
            public Person()
            {
                Member = nameof(MyOtherNamespace.Samples.Test.User.Person.Id);
            }
            public string Member { get; set; }

            public string User { get; set; }
        }
    }";
            VerifyCSharpFix(test, fixtest, allowNewCompilerDiagnostics: true);
        }

        [TestMethod]
        public void TestRewrite_TypeQualifierIsUsed_NoTypeQualifierUsed_IfWithinSameTypeScope()
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
        class Person
        {
            var value = PropertyUtil.GetName<Person>(x => x.Name);

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
                            new DiagnosticResultLocation("Test0.cs", 13, 25)
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
        class Person
        {
            var value = nameof(Name);

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
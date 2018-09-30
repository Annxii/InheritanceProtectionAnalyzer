using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;

namespace Anx.Analyzers.InheritanceProtection.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        protected override string TestAssemblyName => "Anx.Utility";

        [TestMethod]
        public void TestMethod1()
        {
            var test = @"
using System;

namespace Anx.Utility
{
    public class InheritanceProtectionAttribute : Attribute
    {
    }

    [InheritanceProtection]
    public abstract class ViewModelBase
    {
    }

    public class ValidViewModelImpl : ViewModelBase
    {
    }
}

namespace Anx.Implementation
{
    public class InvalidViewModelImpl : Anx.Utility.ViewModelBase
    {
    }
}";

            var codeFixTest = @"
using System;

namespace Anx.Utility
{
    public class InheritanceProtectionAttribute : Attribute
    {
    }

    [InheritanceProtection]
    public abstract class ViewModelBase
    {
    }

    public class ValidViewModelImpl : ViewModelBase
    {
    }
}

namespace Anx.Implementation
{
    public class InvalidViewModelImpl : Anx.Utility.ValidViewModelImpl
    {
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "ANX1000",
                Message = $"Type name 'InvalidViewModelImpl' cannot inherit directly from type name 'ViewModelBase'",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 22, 18)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
            VerifyCSharpFix(test, codeFixTest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new InheritanceProtectionCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new InheritanceProtectionAnalyzer();
        }
    }
}

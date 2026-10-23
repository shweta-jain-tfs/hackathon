using FluentValidation.TestHelper;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Commands.ImportMethodsAndTests;

namespace Pharos.Server.Parameters.Application.Validators;

public class MethodValidatorTests
{
    private readonly MethodValidator _sut = new();

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_5948")]
    [Theory]
    [InlineData("123456789012345678901234567890", true)]
    [InlineData("", true)]
    [InlineData("1234567890123456789012345678901", false)]
    public void Validator_Method_FullMethodName(string parameter, bool shouldPass)
    {
        var methodFixture = new Method
        {
            FullMethodName = parameter
        };

        var result = _sut.TestValidate(methodFixture);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor(method => method.FullMethodName);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(method => method.FullMethodName);
        }
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_5948")]
    [Theory]
    [InlineData("1234", true)]
    [InlineData("", false)]
    [InlineData("12345", false)]
    public void Validator_Method_MethodName(string parameter, bool shouldPass)
    {
        var methodFixture = new Method
        {
            MethodName = parameter
        };

        var result = _sut.TestValidate(methodFixture);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor(method => method.MethodName);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(method => method.MethodName);
        }
    }

    [Trait("ProductRequirement", "PR_2272")]
    [Trait("ModuleRequirement", "MR_4672")]
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(99999900, true)]
    public void Validator_Method_MethodVersion(int parameter, bool shouldPass)
    {
        var methodFixture = new Method
        {
            MethodVersion = parameter
        };

        var result = _sut.TestValidate(methodFixture);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor(method => method.MethodVersion);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(method => method.MethodVersion);
        }
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_5948")]
    [Theory]
    [InlineData("", true)]
    [InlineData("12345678901234567890", true)]
    [InlineData("123456789012345678901", false)]
    public void Validator_Method_LisMethodName(string parameter, bool shouldPass)
    {
        var methodFixture = new Method
        {
            MethodNameLis = parameter
        };

        var result = _sut.TestValidate(methodFixture);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor(method => method.MethodNameLis);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(method => method.MethodNameLis);
        }
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void Validator_Method_DefaultMaxNumberOfDays(int parameter, bool shouldPass)
    {
        var methodFixture = new Method
        {
            CalibrationCurve = new MethodCalibrationCurve { DefaultMaxNumberOfDays = parameter }
        };

        var result = _sut.TestValidate(methodFixture);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor(method => method.CalibrationCurve.DefaultMaxNumberOfDays);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(method => method.CalibrationCurve.DefaultMaxNumberOfDays);
        }
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(9999, true)]
    public void Validator_Method_MaxAnalyticalRunSize(int parameter, bool shouldPass)
    {
        var methodFixture = new Method
        {
            AnalyticalRun = new MethodAnalyticalRun { DefaultMaxAnalyticalRunSize = parameter }
        };

        var result = _sut.TestValidate(methodFixture);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor(method => method.AnalyticalRun.DefaultMaxAnalyticalRunSize);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(method => method.AnalyticalRun.DefaultMaxAnalyticalRunSize);
        }
    }

    [Trait("ProductRequirement", "PR_348")]
    [Trait("ModuleRequirement", "MR_5956")]
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(99999, true)]
    [InlineData(100000, false)]
    public void Validator_Method_DefaultMinConcentrationToReport(double parameter, bool shouldPass)
    {
        var methodFixture = new Method
        {
            Reporting = new MethodReporting { DefaultMinConcentrationToReport = parameter }
        };

        var result = _sut.TestValidate(methodFixture);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor(method => method.Reporting.DefaultMinConcentrationToReport);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(method => method.Reporting.DefaultMinConcentrationToReport);
        }
    }

    //Method Dilutions


    //[Fact]
    //public void Validator_MethodCalibrators_MethodTestName()
    //{
    //    var methodtestFixture = _fixture.Create<MethodTest>() with { TestName = "ABA"};
    //    var methodCalibrationFixture = _fixture.Create<MethodCalibrators>() with { MethodTestName = "AAA"};
    //    var methodFixture = _fixture.Create<Method>() with {  MethodCalibrators = methodCalibrationFixture };

    //    var result = _sut.TestValidate(methodFixture);
    //    result.ShouldHaveValidationErrorFor(method => method.MethodCalibrators.MethodTestName);
    //}

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(6, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    public void Validator_MethodCalibrators_QuotientIndex(int quotationIndex, bool shouldPass)
    {
        var curveFixture = new MethodCalibrationCurve
        {
            QuotientIndex = quotationIndex,
            CalibrationPoints = new List<MethodCalibrationPoint>
            {
                new (),
                new (),
                new (),
                new (),
                new (),
            }
        };

        var methodFixture = new Method
        {
            CalibrationCurve = curveFixture
        };

        var error = _sut.TestValidate(methodFixture).Errors;
        var errorFound = false;
        foreach (var e in error)
        {
            if (e.ErrorMessage.Contains("The specified condition was not met for 'Quotient Index'."))
            {
                errorFound = true;
            }
        }

        if (shouldPass && errorFound)
        {
            Assert.Fail("QuotientIndex error found");
        }
        if (!shouldPass && !errorFound)
        {
            Assert.Fail("QuotientIndex error not found");
        }
    }
}

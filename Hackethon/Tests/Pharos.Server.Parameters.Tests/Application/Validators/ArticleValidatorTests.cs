using FluentValidation.TestHelper;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Parameters.Application.Commands.ImportArticles;

namespace Pharos.Server.Parameters.Application.Validators;

public class ArticleValidatorTests
{
    private readonly ArticleValidator _sut;
    private readonly PackageValidator _sutPackage;

    public ArticleValidatorTests()
    {
        _sut = new ArticleValidator();
        _sutPackage = new PackageValidator();
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Theory]
    [InlineData(36, ArticleType.StopSolutionBottle)]
    [InlineData(7, ArticleType.StopSolutionBottle)]
    [InlineData(37, ArticleType.WashConcentrateBottle)]
    [InlineData(20, ArticleType.WashConcentrateBottle)]
    [InlineData(38, ArticleType.WashAdditiveBottle)]
    [InlineData(21, ArticleType.WashAdditiveBottle)]
    [InlineData(39, ArticleType.FluoroCVial)]
    [InlineData(10, ArticleType.FluoroCVial)]
    public void Normalize_MapsArticleImportAliasesToCanonicalValues(int input, ArticleType expected)
    {
        ArticleTypeImportMapper.Normalize((ArticleType)input).Should().Be(expected);
    }

    [Fact]
    public void Normalize_LeavesUnsupportedArticleTypesForValidation()
    {
        ArticleTypeImportMapper.Normalize((ArticleType)999).Should().Be((ArticleType)999);
    }

    [Theory]
    [InlineData("AAA", true)]
    [InlineData("ZZZ", true)]
    [InlineData("001", true)]
    [InlineData("999", true)]
    [InlineData("A99", true)]
    [InlineData("Z8U", true)]
    [InlineData("000", false)]
    [InlineData("AA", false)]
    [InlineData("-111", false)]
    [InlineData("AAAA", false)]
    [InlineData("", false)]
    public void Validator_Article_ArticleBarcode(string parameter, bool shouldPass)
    {
        var articleFixture = new Article
        {
            ArticleNumber = parameter
        };

        var result = _sut.TestValidate(articleFixture);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.ArticleNumber);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.ArticleNumber);
        }
    }

    [Theory]
    [InlineData("1234567890123456789012345678901234567890123456789012345", true)]
    [InlineData("12345678901234567890123456789012345678901234567890123456", false)]
    [InlineData("", true)]
    public void Validator_Article_ArticleLongName(string parameter, bool shouldPass)
    {
        var articleFixture = new Article
        {
            FullName = parameter
        };

        var result = _sut.TestValidate(articleFixture);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.FullName);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.FullName);
        }
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(9999, true)]
    [InlineData(10000, false)]
    public void Validator_Article_Quantity(int parameter, bool shouldPass)
    {
        var articleFixture = new Article
        {
            Quantity = parameter
        };

        var result = _sut.TestValidate(articleFixture);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.Quantity);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Quantity);
        }
    }


    [Theory]
    [InlineData("1234", ArticleType.CalibratorCartridge, true)]
    [InlineData("1234", ArticleType.CurveControlCartridge, true)]
    [InlineData("12345", ArticleType.CalibratorCartridge, false)]
    [InlineData("12345", ArticleType.CurveControlCartridge, false)]
    [InlineData("", ArticleType.CalibratorCartridge, false)]
    [InlineData("", ArticleType.ConjugateCartridge, true)]
    [InlineData("1234", ArticleType.EliaWellCarrier, true)]
    public void Validator_Article_MethodName_ArticleType(string methodName, ArticleType articleType, bool shouldPass)
    {
        var articleFixture = new Article
        {
            MethodName = methodName,
            ArticleType = articleType
        };

        var result = _sut.TestValidate(articleFixture);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.MethodName);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.MethodName);
        }
    }

    [Theory]
    [InlineData("1234", ArticleType.ConjugateCartridge, true)]
    [InlineData("12345", ArticleType.ImmunoCapCarrier, true)]
    [InlineData("1234", ArticleType.QualityControlCartridge, true)]
    [InlineData("1234567890123456", ArticleType.DiluentCartridge, true)]
    [InlineData("12345", ArticleType.EliaWellCarrier, true)]
    [InlineData("12345", ArticleType.EliaCalibratorWellCarrier, true)]
    [InlineData("12345", ArticleType.DilutionWellCarrier, true)]

    [InlineData("12345", ArticleType.ConjugateCartridge, false)]
    [InlineData("123456", ArticleType.ImmunoCapCarrier, false)]
    [InlineData("12345678901234567", ArticleType.DiluentCartridge, false)]
    [InlineData("123456", ArticleType.EliaWellCarrier, false)]
    [InlineData("123456", ArticleType.EliaCalibratorWellCarrier, false)]
    [InlineData("123456", ArticleType.DilutionWellCarrier, false)]

    [InlineData("", ArticleType.ConjugateCartridge, false)]
    [InlineData("", ArticleType.ImmunoCapCarrier, false)]
    [InlineData("", ArticleType.QualityControlCartridge, false)]
    [InlineData("", ArticleType.DiluentCartridge, false)]
    [InlineData("", ArticleType.EliaWellCarrier, false)]
    [InlineData("", ArticleType.EliaCalibratorWellCarrier, false)]
    [InlineData("", ArticleType.DilutionWellCarrier, false)]

    [InlineData("", ArticleType.WashConcentrateBottle, false)]
    [InlineData("123456789012345678901234567890", ArticleType.StopSolutionBottle, true)]

    public void Validator_Article_ArticleIdentity_ArticleType(string articleIdentity, ArticleType articleType, bool shouldPass)
    {
        var articleFixture = new Article
        {
            Identity = articleIdentity,
            ArticleType = articleType
        };

        var result = _sut.TestValidate(articleFixture);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.Identity);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Identity);
        }
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(-1, false)]
    [InlineData(999, true)]
    [InlineData(1000, false)]
    public void Validator_Article_MaxDaysInInstrument(int parameter, bool shouldPass)
    {
        var articleFixture = new Article
        {
            MaxDaysInInstrument = parameter
        };

        var result = _sut.TestValidate(articleFixture);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.MaxDaysInInstrument);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.MaxDaysInInstrument);
        }
    }

    [Theory]
    [InlineData("AAA", true)]
    [InlineData("ZZZ", true)]
    [InlineData("001", true)]
    [InlineData("999", true)]
    [InlineData("A99", true)]
    [InlineData("Z8U", true)]
    [InlineData("000", false)]
    [InlineData("AA", false)]
    [InlineData("-111", false)]
    [InlineData("AAAA", false)]
    [InlineData("", false)]
    public void Validator_Package_PackageBarcode(string parameter, bool shouldPass)
    {
        var package = new Package
        {
            PackageBarcode = parameter
        };

        var result = _sutPackage.TestValidate(package);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.PackageBarcode);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.PackageBarcode);
        }
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Theory]
    [InlineData("1", true)]
    [InlineData("123456789012345", true)]
    [InlineData("1234567890123456", false)]
    [InlineData("", false)]
    public void Validator_Package_OrderNumber(string parameter, bool shouldPass)
    {
        var package = new Package
        {
            OrderingNumber = parameter
        };

        var result = _sutPackage.TestValidate(package);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.OrderingNumber);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.OrderingNumber);
        }
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("1234567890123456789012345678901234567890123456789012345", true)]
    [InlineData("12345678901234567890123456789012345678901234567890123456", false)]
    [InlineData("", true)]
    public void Validator_Package_PackageLongName(string parameter, bool shouldPass)
    {
        var package = new Package
        {
            PackageFullName = parameter
        };

        var result = _sutPackage.TestValidate(package);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.PackageFullName);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.PackageFullName);
        }
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(9999, true)]
    [InlineData(10009, false)]
    public void Validator_PackageItem_ArticleAmount(int parameter, bool shouldPass)
    {
        var packageContentList = new List<PackageItem>();
        var packageContent = new PackageItem
        {
            ArticleAmount = parameter
        };
        packageContentList.Add(packageContent);
        var package = new Package
        {
            OrderingNumber = "111",
            PackageItems = packageContentList
        };
        var result = _sutPackage.TestValidate(package);

        var propName = $"{nameof(Package.PackageItems)}[0].{nameof(packageContent.ArticleAmount)}";
        if (shouldPass)
        {
            result.Errors.Should().NotContain(err => err.PropertyName == propName);
        }
        else
        {
            result.Errors.Should().Contain(err => err.PropertyName == propName);
        }
    }
}
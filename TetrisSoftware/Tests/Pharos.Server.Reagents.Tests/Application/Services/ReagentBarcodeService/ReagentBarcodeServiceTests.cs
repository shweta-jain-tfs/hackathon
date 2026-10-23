using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Parameters.Application.Interfaces;

// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo

namespace Pharos.Server.Reagents.Application.Services.ReagentBarcodeService;

public class ReagentBarcodeServiceTests
{
    private readonly Mock<IParametersDbContext> _dbContextMock;
    private readonly ReagentBarcodeService _sut;
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly Mock<IArticlesService> _articlesServiceMock;

    public ReagentBarcodeServiceTests()
    {
        _dbContextMock = new Mock<IParametersDbContext>();
        _articlesServiceMock = new Mock<IArticlesService>();
#pragma warning disable CA2000
        var logger = new Mock<ILogger<ReagentBarcodeService>>();
        _sut = new ReagentBarcodeService(_articlesServiceMock.Object, _memoryCache, logger.Object);
#pragma warning restore CA2000
    }

    private void AddArticle(string articleNumber, ArticleType articleType, int quantity)
    {
        var results = new List<Article>
        {
            new()
            {
                ArticleNumber = "XXX",
                ArticleType = ArticleType.None,
                Quantity = -1
            },
            new()
            {
                ArticleNumber = articleNumber,
                ArticleType = articleType,
                Quantity = quantity
            }
        };
        _dbContextMock.Setup(x => x.Articles).ReturnsDbSet(results);

        // Also setup the articles service mock for the new constructor dependency
        _articlesServiceMock.Setup(x => x.GetArticle(articleNumber))
            .ReturnsAsync(results.First(r => r.ArticleNumber == articleNumber));
    }

    [Theory]

    // ImmunoCap Allergen f78 - Barcode tests
    [InlineData("CTSE2B9", null, "CTS", ArticleType.ImmunoCapCarrier, "CTSB9", null, "2022-03-31", 20)]

    // Elia Beta2-Glycoprotein I IgA Well - Barcode test - 0U in Base36 = 30, padded to 4 digits
    [InlineData("CBBEL7S0U", null, "CBB", ArticleType.EliaWellCarrier, "0030", null, "2023-10-31", 12)]

    // EliA anti-TSh-R Well - Barcode test - 0X in Base36 = 33, padded to 4 digits
    [InlineData("D14E000000000X", null, "D14", ArticleType.EliaCalibratorWellCarrier, "0033", null, "2022-01-31", 16)]

    // EliA IgM Calibrator Well - Barcode test - 1A in Base36 = 46, padded to 4 digits
    [InlineData("CB7E5FPDVCSG1A", null, "CB7", ArticleType.EliaCalibratorWellCarrier, "0046", null, "2022-06-30", 16)]

    // Fictional cartridge barcode
    [InlineData("|ABCE25B9255R", null, "ABC", ArticleType.ConjugateCartridge, "ABCB9", "255R", "2022-03-05", 255)]

    // UDI-based tests - separate from barcode tests
    [InlineData(null, "|01073330660041291722033110CTS19|991234", "CTS", ArticleType.ImmunoCapCarrier, "CTS19", "1234", "2022-03-31", 20)]
    [InlineData(null, "|01073330660041291722033110CTS19", "CTS", ArticleType.ImmunoCapCarrier, "CTS19", null, "2022-03-31", 20)]
    [InlineData(null, "|01073330660147081724063010CBB01", "CBB", ArticleType.EliaWellCarrier, "CBB01", null, "2024-06-30", 12)]
    [InlineData(null, "|01073330660147081722063010CB701", "CB7", ArticleType.EliaCalibratorWellCarrier, "CB701", null, "2022-06-30", 16)]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5534")]
    public async Task GetReagentInfo(string? barcode, string? udi, string articleNumber, ArticleType articleType, string lotNumber, string? serialNumber, string expiryDate, int quantity)
    {
        AddArticle(articleNumber, articleType, quantity);
        var reagentInfo = await _sut.GetReagentInfo(articleType, barcode, udi);
        reagentInfo.Should().NotBeNull();
        reagentInfo!.Article.ArticleNumber.Should().Be(articleNumber);
        reagentInfo.Article.ArticleType.Should().Be(articleType);
        reagentInfo.Article.Quantity.Should().Be(quantity);
        reagentInfo.SerialNumber.Should().Be(serialNumber);
        reagentInfo.Lot.LotNumber.Should().Be(lotNumber);
        reagentInfo.Lot.ExpiryDate.Should().Be(DateTimeOffset.ParseExact(expiryDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
    }

    [Theory]

    // Elia Beta2-Glycoprotein I IgA Well
    [InlineData("CBBEL7S0U", "CBB", 2.8)]
    [Trait("ProductRequirement", "PR_862")]
    [Trait("ModuleRequirement", "MR_3535")]
    public async Task GetEliaCarrierLotCorrectionFactor(string barcode, string articleNumber, double correctionFactor)
    {
        AddArticle(articleNumber, ArticleType.EliaWellCarrier, 12);
        var reagentInfo = await _sut.GetReagentInfo(ArticleType.EliaWellCarrier, barcode);
        var lot = (EliaWellLotInformation)reagentInfo!.Lot;
        lot.CorrectionFactor.Should().Be(correctionFactor);
    }

    [Theory]

    // EliA anti-TSh-R Well
    [InlineData("D14E000000000X", "D14", 0, 0, 0, true)]

    // EliA IgM Calibrator Well
    [InlineData("CB7E5FPDVCSG1A", "CB7", 5.65, 4.99, 4.60, true)]
    [InlineData("CB7E5FPDVCSD1A", "CB7", 5.65, 4.99, 4.60, false)]
    [Trait("ProductRequirement", "PR_862")]
    [Trait("ModuleRequirement", "MR_3535")]
    public async Task GetEliaCalibratorLotParameters(string barcode, string articleNumber, double alpha, double beta1, double beta2, bool checksumValid)
    {
        AddArticle(articleNumber, ArticleType.EliaCalibratorWellCarrier, 16);
        var reagentInfo = await _sut.GetReagentInfo(ArticleType.EliaCalibratorWellCarrier, barcode);
        var lot = (EliaCalibratorWellLotInformation)reagentInfo!.Lot;
        lot.Alpha.Should().Be(alpha);
        lot.Beta1.Should().Be(beta1);
        lot.Beta2.Should().Be(beta2);
        lot.IsChecksumValid.Should().Be(checksumValid);
    }

    [Fact]
    public async Task ArticleInfoCacheTest()
    {
        var barcode = "CTSE2B9";
        var articleNumber = "CTS";
        AddArticle(articleNumber, ArticleType.ImmunoCapCarrier, 20);

        var reagentInfo = await _sut.GetReagentInfo(ArticleType.ImmunoCapCarrier, barcode, null);
        reagentInfo!.Article.ArticleNumber.Should().Be(articleNumber);
        _articlesServiceMock.Verify(m => m.GetArticle(articleNumber), Times.Once);

        reagentInfo = await _sut.GetReagentInfo(ArticleType.ImmunoCapCarrier, barcode, null);
        reagentInfo!.Article.ArticleNumber.Should().Be(articleNumber);
        _articlesServiceMock.Verify(m => m.GetArticle(articleNumber), Times.Once);
    }

    [Fact]
    public async Task GetReagentInfo_ReturnsNull_WhenBothParametersEmpty()
    {
        var result = await _sut.GetReagentInfo(ArticleType.ImmunoCapCarrier, null, null);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetReagentInfo_ReturnsNull_WhenBothParametersWhitespace()
    {
        var result = await _sut.GetReagentInfo(ArticleType.ImmunoCapCarrier, "   ", "   ");
        result.Should().BeNull();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5534")]
    public async Task GetReagentInfo_ReturnsNull_WhenInvalidUdi()
    {
        var invalidUdi = "invalid-udi-format";
        AddArticle("CTS", ArticleType.ImmunoCapCarrier, 20);

        var result = await _sut.GetReagentInfo(ArticleType.ImmunoCapCarrier, null, invalidUdi);
        result.Should().BeNull();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5534")]
    public async Task GetReagentInfo_ReturnsNull_WhenArticleNotFoundForUdi()
    {
        var validUdi = "|01073330660041291722033110CTS19|991234";
        // Setup empty DbSet to simulate no articles found
        var emptyResults = new List<Article>();
        _dbContextMock.Setup(x => x.Articles).ReturnsDbSet(emptyResults);
        _articlesServiceMock.Setup(x => x.GetArticle(It.IsAny<string>())).ReturnsAsync((Article?)null);

        var result = await _sut.GetReagentInfo(ArticleType.ImmunoCapCarrier, null, validUdi);
        result.Should().BeNull();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5539")]
    public async Task GetReagentInfo_ThrowsArgumentException_WhenBottleReagentWithoutUdi()
    {
        // This is the only exception that should still be thrown
        var act = () => _sut.GetReagentInfo(ArticleType.StopSolutionBottle, "primary-barcode", null);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Scan the UDI code instead.");
    }

    [Fact]
    public void GetLotNumber_ReturnsNull_WhenBothParametersEmpty()
    {
        var result = _sut.GetReagentLotInformation(ArticleType.ImmunoCapCarrier, null, null);
        result.Should().BeNull();
    }

    [Fact]
    public void GetLotNumber_DoesNotThrow_WhenArticleTypeNotProvidedForBarcode()
    {
        var act = () => _sut.GetReagentLotInformation(ArticleType.ImmunoCapCarrier, "CTSE2B9", null);
        act.Should().NotThrow(); // This should be valid now
    }

    [Theory]
    [InlineData("CTSE2B9", ArticleType.ImmunoCapCarrier, "CTSB9")]
    [InlineData("CBBEL7S0U", ArticleType.EliaWellCarrier, "0030")]  // Updated: 0U in Base36 = 30, padded to 4 digits
    [InlineData("|ABCE25B9255R", ArticleType.ConjugateCartridge, "ABCB9")]
    public void GetLotNumber_FromBarcode_ReturnsCorrectLotNumber(string barcode, ArticleType articleType, string expectedLotNumber)
    {
        var result = _sut.GetReagentLotInformation(articleType, barcode, null);

        result!.LotNumber.Should().Be(expectedLotNumber);
    }

    [Theory]
    [InlineData("|01073330660041291722033110CTS19|991234", "CTS19")]
    [InlineData("|01073330660041291722033110ABC12", "ABC12")]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5538")]
    public void GetLotNumber_FromUdi_ReturnsCorrectLotNumber(string udi, string expectedLotNumber)
    {
        var result = _sut.GetReagentLotInformation(ArticleType.WashAdditiveBottle, null, udi);

        result!.LotNumber.Should().Be(expectedLotNumber);
    }

    [Fact]
    public void GetLotNumber_ReturnsNull_WhenInvalidUdi()
    {
        var result = _sut.GetReagentLotInformation(ArticleType.ImmunoCapCarrier, null, "invalid-udi");

        result.Should().BeNull();
    }

    [Fact]
    public void GetSerialNumber_ReturnsNull_WhenBothParametersEmpty()
    {
        var result = _sut.GetSerialNumber(ArticleType.ConjugateCartridge, null, null);
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("ABCE25B9255R0", "255R")]
    [InlineData("ABCE25B9ABCD1234", "ABCD")]  // Tests 4-character extraction from 12-character barcode
    public void GetSerialNumber_FromBarcode_ReturnsCorrectSerialNumber(string barcode, string expectedSerialNumber)
    {
        var result = _sut.GetSerialNumber(ArticleType.ConjugateCartridge, barcode, null);

        result.Should().Be(expectedSerialNumber);
    }

    [Theory]
    [InlineData("|01073330660041291722033110CTS19|991234", "1234")]
    [InlineData("|01073330660041291722033110ABC12|99ABCD5678", "ABCD5678")]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5534")]
    public void GetSerialNumber_FromUdi_ReturnsCorrectSerialNumber(string udi, string expectedSerialNumber)
    {
        var result = _sut.GetSerialNumber(ArticleType.ImmunoCapCarrier, null, udi);

        result.Should().Be(expectedSerialNumber);
    }

    [Fact]
    public void GetSerialNumber_ReturnsNull_WhenInvalidUdi()
    {
        var result = _sut.GetSerialNumber(ArticleType.ImmunoCapCarrier, null, "invalid-udi");

        result.Should().BeNull();
    }

    [Fact]
    public void GetReagentLotInformation_ReturnsNull_WhenBothParametersEmpty()
    {
        var result = _sut.GetReagentLotInformation(ArticleType.ImmunoCapCarrier, null, null);
        result.Should().BeNull();
    }

    [Fact]
    public void GetReagentLotInformation_DoesNotThrow_WhenArticleTypeNotProvidedForBarcode()
    {
        var act = () => _sut.GetReagentLotInformation(ArticleType.ImmunoCapCarrier, "CTSE2B9", null);
        act.Should().NotThrow(); // This should be valid now with the new interface
    }

    [Theory]
    [InlineData("CTSE2B9", ArticleType.ImmunoCapCarrier, "CTSB9", "2022-03-31")]
    [InlineData("CBDEL1T1P", ArticleType.EliaWellCarrier, "0061", "2023-10-31")]  // Updated: 0U in Base36 = 30, padded to 4 digits
    public void GetReagentLotInformation_FromBarcode_ReturnsCorrectLotInformation(string barcode, ArticleType articleType, string expectedLotNumber, string expectedExpiryDate)
    {
        var result = _sut.GetReagentLotInformation(articleType, barcode, null);

        result.Should().NotBeNull();
        result!.LotNumber.Should().Be(expectedLotNumber);
        result.ExpiryDate.Should().Be(DateTimeOffset.ParseExact(expectedExpiryDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
    }

    [Theory]
    [InlineData("|01073330660041291722033110CTS19|991234", "CTS19", "2022-03-31")]
    [InlineData("|01073330660041291722033110ABC12", "ABC12", "2022-03-31")]
    public void GetReagentLotInformation_FromUdi_ReturnsCorrectLotInformation(string udi, string expectedLotNumber, string expectedExpiryDate)
    {
        var result = _sut.GetReagentLotInformation(ArticleType.WashAdditiveBottle, null, udi);

        result.Should().NotBeNull();
        result!.LotNumber.Should().Be(expectedLotNumber);
        result.ExpiryDate.Should().Be(DateTimeOffset.ParseExact(expectedExpiryDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
    }

    [Fact]
    public void GetReagentLotInformation_ReturnsNull_WhenInvalidUdi()
    {
        var result = _sut.GetReagentLotInformation(ArticleType.ImmunoCapCarrier, null, "invalid-udi");

        result.Should().BeNull();
    }

    // New tests for GetCarrierContentsFromOcr method
    [Fact]
    public async Task GetCarrierContentsFromOcr_ReturnsEmptyDictionary_WhenOcrListIsEmpty()
    {
        var ocrList = new List<string>();

        var result = await _sut.GetCarrierContentsFromOcr(ocrList);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCarrierContentsFromOcr_ReturnsReagentInfoWithUnknownArticle_WhenNoMatchingArticlesFound()
    {
        var ocrList = new List<string> { "UnknownOCR" };
        var articles = new List<Article>(); // Empty list - no matches

        _articlesServiceMock.Setup(x => x.GetCarrierArticlesFromIdentities(It.IsAny<IReadOnlyList<string>>()))
            .ReturnsAsync(articles);

        var result = await _sut.GetCarrierContentsFromOcr(ocrList);

        result.Should().HaveCount(1);
        result.Should().ContainKey("UnknownOCR");
        var reagentInfo = result["UnknownOCR"];
        reagentInfo.Article.ArticleType.Should().Be(ArticleType.None);
        reagentInfo.Article.ArticleNumber.Should().BeEmpty();
        reagentInfo.Article.Identity.Should().Be("UnknownOCR");
        reagentInfo.Article.Quantity.Should().Be(0);
    }

    [Fact]
    public async Task GetCarrierContentsFromOcr_ReturnsReagentInfoWithMinQuantity_WhenSingleArticleTypeMatches()
    {
        var ocrList = new List<string> { "TestOCR" };
        var articles = new List<Article>
        {
            new()
            {
                Identity = "TestOCR",
                ArticleType = ArticleType.ImmunoCapCarrier,
                ArticleNumber = "TST",
                Quantity = 100
            },
            new()
            {
                Identity = "TestOCR",
                ArticleType = ArticleType.ImmunoCapCarrier,
                ArticleNumber = "TST",
                Quantity = 50  // This should be the minimum
            },
            new()
            {
                Identity = "TestOCR",
                ArticleType = ArticleType.ImmunoCapCarrier,
                ArticleNumber = "TST",
                Quantity = 75
            }
        };

        _articlesServiceMock.Setup(x => x.GetCarrierArticlesFromIdentities(It.IsAny<IReadOnlyList<string>>()))
            .ReturnsAsync(articles);

        var result = await _sut.GetCarrierContentsFromOcr(ocrList);

        result.Should().HaveCount(1);
        result.Should().ContainKey("TestOCR");
        var reagentInfo = result["TestOCR"];
        reagentInfo.Article.ArticleType.Should().Be(ArticleType.ImmunoCapCarrier);
        reagentInfo.Article.Quantity.Should().Be(50); // Minimum quantity
        reagentInfo.Article.Identity.Should().Be("TestOCR");
    }

    [Fact]
    public async Task GetCarrierContentsFromOcr_ReturnsUnknownArticle_WhenMultipleArticleTypesMatch()
    {
        var ocrList = new List<string> { "AmbiguousOCR" };
        var articles = new List<Article>
        {
            new()
            {
                Identity = "AmbiguousOCR",
                ArticleType = ArticleType.ImmunoCapCarrier,
                ArticleNumber = "TST1",
                Quantity = 100
            },
            new()
            {
                Identity = "AmbiguousOCR",
                ArticleType = ArticleType.EliaWellCarrier,  // Different article type
                ArticleNumber = "TST2",
                Quantity = 50
            }
        };

        _articlesServiceMock.Setup(x => x.GetCarrierArticlesFromIdentities(It.IsAny<IReadOnlyList<string>>()))
            .ReturnsAsync(articles);

        var result = await _sut.GetCarrierContentsFromOcr(ocrList);

        result.Should().HaveCount(1);
        result.Should().ContainKey("AmbiguousOCR");
        var reagentInfo = result["AmbiguousOCR"];
        reagentInfo.Article.ArticleType.Should().Be(ArticleType.None); // Ambiguous -> None
        reagentInfo.Article.ArticleNumber.Should().BeEmpty();
        reagentInfo.Article.Identity.Should().Be("AmbiguousOCR");
        reagentInfo.Article.Quantity.Should().Be(0);
    }

    [Fact]
    public async Task GetCarrierContentsFromOcr_UsesCache_WhenDataAlreadyCached()
    {
        var ocrList = new List<string> { "CachedOCR" };
        var cachedReagentInfo = new ReagentInfo
        {
            Article = new Article
            {
                Identity = "CachedOCR",
                ArticleType = ArticleType.ImmunoCapCarrier,
                ArticleNumber = "CHD",
                Quantity = 200
            }
        };

        // Pre-populate cache
        _memoryCache.Set("CachedOCR", cachedReagentInfo);

        var result = await _sut.GetCarrierContentsFromOcr(ocrList);

        result.Should().HaveCount(1);
        result.Should().ContainKey("CachedOCR");
        result["CachedOCR"].Should().BeEquivalentTo(cachedReagentInfo);

        // Verify that articles service was not called since data was cached
        _articlesServiceMock.Verify(x => x.GetCarrierArticlesFromIdentities(It.IsAny<IReadOnlyList<string>>()), Times.Never);
    }

    [Fact]
    public async Task GetCarrierContentsFromOcr_CachesResults_AfterFetching()
    {
        var ocrList = new List<string> { "NewOCR" };
        var articles = new List<Article>
        {
            new()
            {
                Identity = "NewOCR",
                ArticleType = ArticleType.ImmunoCapCarrier,
                ArticleNumber = "NEW",
                Quantity = 150
            }
        };

        _articlesServiceMock.Setup(x => x.GetCarrierArticlesFromIdentities(It.IsAny<IReadOnlyList<string>>()))
            .ReturnsAsync(articles);

        var result = await _sut.GetCarrierContentsFromOcr(ocrList);

        result.Should().HaveCount(1);
        result.Should().ContainKey("NewOCR");

        // Verify the result was cached
        _memoryCache.TryGetValue("NewOCR", out ReagentInfo? cachedValue).Should().BeTrue();
        cachedValue.Should().NotBeNull();
        cachedValue!.Article.Identity.Should().Be("NewOCR");
    }

    [Fact]
    public async Task GetCarrierContentsFromOcr_HandlesNullIdentity_InArticles()
    {
        var ocrList = new List<string> { "TestOCR" };
        var articles = new List<Article>
        {
            new()
            {
                Identity = null, // Null identity should be filtered out
                ArticleType = ArticleType.ImmunoCapCarrier,
                ArticleNumber = "TST",
                Quantity = 100
            },
            new()
            {
                Identity = "TestOCR",
                ArticleType = ArticleType.ImmunoCapCarrier,
                ArticleNumber = "TST",
                Quantity = 50
            }
        };

        _articlesServiceMock.Setup(x => x.GetCarrierArticlesFromIdentities(It.IsAny<IReadOnlyList<string>>()))
            .ReturnsAsync(articles);

        var result = await _sut.GetCarrierContentsFromOcr(ocrList);

        result.Should().HaveCount(1);
        result.Should().ContainKey("TestOCR");
        var reagentInfo = result["TestOCR"];
        reagentInfo.Article.Identity.Should().Be("TestOCR");
        reagentInfo.Article.Quantity.Should().Be(50);
    }

    [Fact]
    public async Task GetCarrierContentsFromOcr_HandlesMixedCachedAndUncachedData()
    {
        var ocrList = new List<string> { "CachedOCR", "UncachedOCR" };

        // Pre-populate cache for one item
        var cachedReagentInfo = new ReagentInfo
        {
            Article = new Article
            {
                Identity = "CachedOCR",
                ArticleType = ArticleType.ImmunoCapCarrier,
                ArticleNumber = "CHD",
                Quantity = 200
            }
        };
        _memoryCache.Set("CachedOCR", cachedReagentInfo);

        // Setup uncached data
        var articles = new List<Article>
        {
            new()
            {
                Identity = "UncachedOCR",
                ArticleType = ArticleType.EliaWellCarrier,
                ArticleNumber = "UNC",
                Quantity = 300
            }
        };

        _articlesServiceMock.Setup(x => x.GetCarrierArticlesFromIdentities(new List<string> { "UncachedOCR" }))
            .ReturnsAsync(articles);

        var result = await _sut.GetCarrierContentsFromOcr(ocrList);

        result.Should().HaveCount(2);
        result.Should().ContainKey("CachedOCR");
        result.Should().ContainKey("UncachedOCR");

        result["CachedOCR"].Should().BeEquivalentTo(cachedReagentInfo);
        result["UncachedOCR"].Article.Identity.Should().Be("UncachedOCR");
        result["UncachedOCR"].Article.Quantity.Should().Be(300);

        // Verify that articles service was called only for uncached item
        _articlesServiceMock.Verify(x => x.GetCarrierArticlesFromIdentities(
            It.Is<IReadOnlyList<string>>(list => list.Count == 1 && list[0] == "UncachedOCR")), Times.Once);
    }
}

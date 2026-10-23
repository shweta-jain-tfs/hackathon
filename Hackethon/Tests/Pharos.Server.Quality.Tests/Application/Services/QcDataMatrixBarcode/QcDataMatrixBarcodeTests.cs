using System.IO.Compression;
using System.Text;
using Pharos.Server.Application.Quality;

namespace Pharos.Server.Quality.Application.Services.QcDataMatrixBarcode;

public class QcDataMatrixBarcodeTests
{
    /// <summary>
    /// Creates a valid QC data matrix string for testing.
    /// </summary>
    private static string CreateQcDataMatrix(string articleId, string lotNumber, string version, string payload)
    {
        var compressedPayload = CompressAndEncodePayload(payload);
        var crc = Crc32.ComputeHex(compressedPayload);
        return $"|{articleId}{lotNumber}{version}{crc}{compressedPayload}";
    }

    private static string CompressAndEncodePayload(string payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(payload);
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
        {
            gzipStream.Write(bytes, 0, bytes.Length);
        }
        return Convert.ToBase64String(outputStream.ToArray());
    }

    #region Valid Format Tests

    [Fact]
    public void TryParse_ValidDataMatrix_SingleTestSingleLimit_ReturnsTrue()
    {
        var dataMatrix = CreateQcDataMatrix("QC1", "01", "1", "IGA>50");

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeTrue();
        qcData.Should().NotBeNull();
        qcData!.ArticleId.Should().Be("QC1");
        qcData.LotNumber.Should().Be("01");
        qcData.Version.Should().Be("1");
        qcData.Tests.Should().HaveCount(1);
        qcData.Tests[0].TestName.Should().Be("IGA");
        qcData.Tests[0].Limits.Should().HaveCount(1);
        qcData.Tests[0].Limits[0].LimitType.Should().Be(QcCartridgeLimitType.GreaterThan);
        qcData.Tests[0].Limits[0].LimitValue.Should().Be(50.0);
    }

    [Fact]
    public void TryParse_ValidDataMatrix_SingleTestMultipleLimits_ReturnsTestWithMultipleLimits()
    {
        // Format: TestName followed by all its limits (IGA>50<150 means IGA has two limits: >50 and <150)
        var dataMatrix = CreateQcDataMatrix("QC1", "01", "1", "IGA>50<150");

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeTrue();
        qcData.Should().NotBeNull();
        qcData!.Tests.Should().HaveCount(1);
        qcData.Tests[0].TestName.Should().Be("IGA");
        qcData.Tests[0].Limits.Should().HaveCount(2);
        qcData.Tests[0].Limits[0].LimitType.Should().Be(QcCartridgeLimitType.GreaterThan);
        qcData.Tests[0].Limits[0].LimitValue.Should().Be(50.0);
        qcData.Tests[0].Limits[1].LimitType.Should().Be(QcCartridgeLimitType.LessThan);
        qcData.Tests[0].Limits[1].LimitValue.Should().Be(150.0);
    }

    [Fact]
    public void TryParse_ValidDataMatrix_MultipleTests_ReturnsAllTests()
    {
        var dataMatrix = CreateQcDataMatrix("QC1", "01", "1", "IGA>50IGE=100IGM<200");

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeTrue();
        qcData.Should().NotBeNull();
        qcData!.Tests.Should().HaveCount(3);
        qcData.Tests.Select(t => t.TestName).Should().BeEquivalentTo(["IGA", "IGE", "IGM"]);
    }

    [Theory]
    [InlineData("<", QcCartridgeLimitType.LessThan)]
    [InlineData("(", QcCartridgeLimitType.LessThanOrEqual)]
    [InlineData(">", QcCartridgeLimitType.GreaterThan)]
    [InlineData(")", QcCartridgeLimitType.GreaterThanOrEqual)]
    [InlineData("=", QcCartridgeLimitType.Equal)]
    public void TryParse_ValidDataMatrix_AllComparisonOperators_ParsedCorrectly(string operatorChar, QcCartridgeLimitType expectedType)
    {
        var payload = $"TST{operatorChar}100";
        var dataMatrix = CreateQcDataMatrix("QC1", "01", "1", payload);

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeTrue();
        qcData.Should().NotBeNull();
        qcData!.Tests[0].Limits[0].LimitType.Should().Be(expectedType);
    }

    [Fact]
    public void TryParse_ValidDataMatrix_DecimalLimitValues_ParsedCorrectly()
    {
        var dataMatrix = CreateQcDataMatrix("QC1", "01", "1", "IGA>50.5IGE=0.12");

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeTrue();
        qcData.Should().NotBeNull();
        qcData!.Tests[0].Limits[0].LimitValue.Should().Be(50.5);
        qcData.Tests[1].Limits[0].LimitValue.Should().Be(0.12);
    }

    [Fact]
    public void TryParse_ValidDataMatrix_TestNameWithDigits_WithSeparator_ParsedCorrectly()
    {
        // Separator is required when next test name starts with a digit
        var dataMatrix = CreateQcDataMatrix("QC1", "01", "1", @"IGA>50\2TEST=100");

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeTrue();
        qcData.Should().NotBeNull();
        qcData!.Tests.Should().HaveCount(2);
        qcData.Tests[0].TestName.Should().Be("IGA");
        qcData.Tests[1].TestName.Should().Be("2TEST");
    }

    [Theory]
    [InlineData("1")]
    [InlineData("9")]
    [InlineData("A")]
    [InlineData("Z")]
    public void TryParse_ValidVersions_ParsedCorrectly(string version)
    {
        var dataMatrix = CreateQcDataMatrix("QC1", "01", version, "TST>100");

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeTrue();
        qcData!.Version.Should().Be(version);
    }

    #endregion

    #region Invalid Format Tests

    [Fact]
    public void TryParse_NullOrEmpty_ReturnsFalse()
    {
        Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(null!, out var result1).Should().BeFalse();
        result1.Should().BeNull();

        Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(string.Empty, out var result2).Should().BeFalse();
        result2.Should().BeNull();

        Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse("   ", out var result3).Should().BeFalse();
        result3.Should().BeNull();
    }

    [Fact]
    public void TryParse_MissingFnc1Character_ReturnsFalse()
    {
        var dataMatrix = "QC1011" + "00000000" + "payload";

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeFalse();
        qcData.Should().BeNull();
    }

    [Fact]
    public void TryParse_InvalidArticleId_ReturnsFalse()
    {
        // Article ID contains lowercase letter
        var dataMatrix = "|qc1011" + "00000000" + "payload";

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeFalse();
        qcData.Should().BeNull();
    }

    [Fact]
    public void TryParse_InvalidLotNumber_ReturnsFalse()
    {
        // Lot number contains special character
        var dataMatrix = "|QC1$!1" + "00000000" + "payload";

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeFalse();
        qcData.Should().BeNull();
    }

    [Fact]
    public void TryParse_InvalidVersion_ReturnsFalse()
    {
        // Version is 0 (only 1-9 and A-Z are valid)
        var dataMatrix = "|QC1010" + "00000000" + "payload";

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeFalse();
        qcData.Should().BeNull();
    }

    [Fact]
    public void TryParse_InvalidCrcFormat_ReturnsFalse()
    {
        // CRC contains non-hex character 'G'
        var dataMatrix = "|QC1011" + "0000000G" + "payload";

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeFalse();
        qcData.Should().BeNull();
    }

    [Fact]
    public void TryParse_CrcMismatch_ReturnsFalse()
    {
        var payload = CompressAndEncodePayload("IGA>50");
        // Use wrong CRC value
        var dataMatrix = $"|QC1011DEADBEEF{payload}";

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeFalse();
        qcData.Should().BeNull();
    }

    [Fact]
    public void TryParse_InvalidBase64Payload_WithValidChecksum_ReturnsFalse()
    {
        // Invalid Base64 payload with matching CRC (CRC is computed on the raw payload string)
        var invalidPayload = "!!!invalid!!!";
        var crc = Crc32.ComputeHex(invalidPayload);
        var dataMatrix = $"|QC1011{crc}{invalidPayload}";

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeFalse();
        qcData.Should().BeNull();
    }

    [Fact]
    public void TryParse_InvalidBase64Payload_WithInvalidChecksum_ReturnsFalse()
    {
        // Invalid Base64 payload with wrong CRC - checksum validation should fail first
        var dataMatrix = "|QC1011" + "00000000" + "!!!invalid!!!";

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeFalse();
        qcData.Should().BeNull();
    }

    [Fact]
    public void TryParse_TooShort_ReturnsFalse()
    {
        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse("|QC101", out var qcData);

        result.Should().BeFalse();
        qcData.Should().BeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void TryParse_MinimumLengthTestName_ParsedCorrectly()
    {
        var dataMatrix = CreateQcDataMatrix("QC1", "01", "1", "AB>50");

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeTrue();
        qcData!.Tests[0].TestName.Should().Be("AB");
    }

    [Fact]
    public void TryParse_MaximumLengthTestName_ParsedCorrectly()
    {
        var dataMatrix = CreateQcDataMatrix("QC1", "01", "1", "ABCDE>50");

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeTrue();
        qcData!.Tests[0].TestName.Should().Be("ABCDE");
    }

    [Fact]
    public void TryParse_MinimumLimitValue_ParsedCorrectly()
    {
        var dataMatrix = CreateQcDataMatrix("QC1", "01", "1", "TST>1");

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeTrue();
        qcData!.Tests[0].Limits[0].LimitValue.Should().Be(1.0);
    }

    [Fact]
    public void TryParse_MaximumLimitValue_ParsedCorrectly()
    {
        var dataMatrix = CreateQcDataMatrix("QC1", "01", "1", "TST>99999");

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeTrue();
        qcData!.Tests[0].Limits[0].LimitValue.Should().Be(99999.0);
    }

    [Fact]
    public void TryParse_EmptyPayload_ParsesWithEmptyTests()
    {
        // Create data matrix with valid header but no payload
        var crc = Crc32.ComputeHex(string.Empty);
        var dataMatrix = $"|QC1011{crc}";

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeTrue();
        qcData.Should().NotBeNull();
        qcData!.Tests.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_EmptyPayload_InvalidChecksum_ReturnsFalse()
    {
        // Create data matrix with valid header but no payload and wrong CRC
        var dataMatrix = "|QC1011DEADBEEF";

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeFalse();
        qcData.Should().BeNull();
    }

    [Fact]
    public void TryParse_LargeNumberOfTests_ParsedCorrectly()
    {
        var payloadBuilder = new StringBuilder();
        for (var i = 0; i < 20; i++)
        {
            payloadBuilder.Append($"T{i:D2}>100");
        }
        var dataMatrix = CreateQcDataMatrix("QC1", "01", "1", payloadBuilder.ToString());

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeTrue();
        qcData!.Tests.Should().HaveCount(20);
    }

    [Fact]
    public void TryParse_SingleTestWithManyLimits_ParsedCorrectly()
    {
        // Single test name followed by multiple limits: IGA>10(20)30<40=50
        var dataMatrix = CreateQcDataMatrix("QC1", "01", "1", "IGA>10(20)30<40=50");

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeTrue();
        qcData!.Tests.Should().HaveCount(1);
        qcData.Tests[0].Limits.Should().HaveCount(5);
    }

    [Fact]
    public void TryParse_MultipleTestsEachWithSingleLimit_ParsedCorrectly()
    {
        var dataMatrix = CreateQcDataMatrix("QC1", "01", "1", "AA>1BB>2CC>3DD>4EE>5");

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeTrue();
        qcData!.Tests.Should().HaveCount(5);
        foreach (var test in qcData.Tests)
        {
            test.Limits.Should().HaveCount(1);
        }
    }

    #endregion

    #region Parse Method Tests

    [Fact]
    public void Parse_ValidDataMatrix_ReturnsQcDataMatrixBarcode()
    {
        var dataMatrix = CreateQcDataMatrix("QC1", "01", "1", "IGA>50");

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.Parse(dataMatrix);

        result.Should().NotBeNull();
        result.ArticleId.Should().Be("QC1");
    }

    [Fact]
    public void Parse_InvalidDataMatrix_ThrowsArgumentException()
    {
        var act = () => Pharos.Server.Application.Quality.QcDataMatrixBarcode.Parse("invalid");

        act.Should().Throw<ArgumentException>()
            .WithMessage("Invalid QC data matrix format.*");
    }

    #endregion

    #region Article ID Validation Tests

    [Theory]
    [InlineData("ABC")]
    [InlineData("QC1")]
    [InlineData("123")]
    [InlineData("A1B")]
    public void TryParse_ValidArticleIds_ParsedCorrectly(string articleId)
    {
        var dataMatrix = CreateQcDataMatrix(articleId, "01", "1", "TST>100");

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeTrue();
        qcData!.ArticleId.Should().Be(articleId);
    }

    [Theory]
    [InlineData("abc")]  // Lowercase
    [InlineData("Ab1")]  // Mixed case
    [InlineData("A-B")]  // Special character
    public void TryParse_InvalidArticleIds_ReturnsFalse(string articleId)
    {
        var crc = Crc32.ComputeHex(string.Empty);
        var dataMatrix = $"|{articleId}011{crc}";

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeFalse();
        qcData.Should().BeNull();
    }

    #endregion

    #region Lot Number Validation Tests

    [Theory]
    [InlineData("01")]
    [InlineData("99")]
    [InlineData("AB")]
    [InlineData("A1")]
    public void TryParse_ValidLotNumbers_ParsedCorrectly(string lotNumber)
    {
        var dataMatrix = CreateQcDataMatrix("QC1", lotNumber, "1", "TST>100");

        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out var qcData);

        result.Should().BeTrue();
        qcData!.LotNumber.Should().Be(lotNumber);
    }

    #endregion

    #region CRC Validation Tests

    [Fact]
    public void TryParse_ValidHexCrc_ParsedOrValidated()
    {
        // Create a data matrix where we control the CRC
        var payload = CompressAndEncodePayload("TST>100");
        var correctCrc = Crc32.ComputeHex(payload);
        var dataMatrix = $"|QC1011{correctCrc}{payload}";

        // This should parse correctly since we're using the correct CRC
        var result = Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrix, out _);

        result.Should().BeTrue();
    }

    [Fact]
    public void TryParse_CrcIsCaseInsensitive()
    {
        var payload = CompressAndEncodePayload("TST>100");
        var correctCrc = Crc32.ComputeHex(payload);
        var upperCrc = correctCrc.ToUpperInvariant();
        var lowerCrc = correctCrc.ToUpperInvariant();

        var dataMatrixUpper = $"|QC1011{upperCrc}{payload}";
        var dataMatrixLower = $"|QC1011{lowerCrc}{payload}";

        Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrixUpper, out _).Should().BeTrue();
        Pharos.Server.Application.Quality.QcDataMatrixBarcode.TryParse(dataMatrixLower, out _).Should().BeTrue();
    }

    #endregion
}

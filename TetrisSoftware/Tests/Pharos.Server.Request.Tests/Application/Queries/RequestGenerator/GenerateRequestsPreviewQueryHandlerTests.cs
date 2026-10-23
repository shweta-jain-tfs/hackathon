using System.Linq.Expressions;
using FluentValidation;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Request.Application.Commands.RequestGenerator;
using Pharos.Server.Request.Application.Services;
using static Pharos.Server.Request.Application.Services.RequestGeneratorValidationService;

namespace Pharos.Server.Request.Application.Queries.RequestGenerator;

public class GenerateRequestsPreviewQueryHandlerTests
{
    private readonly Mock<IMethodsService> _methodsServiceMock;
    private readonly GenerateRequestsPreviewQueryHandler _handler;

    public GenerateRequestsPreviewQueryHandlerTests()
    {
        _methodsServiceMock = new Mock<IMethodsService>();

        // Default: IMethodsService returns valid methods for MTH1-MTH3 / TST1-TST3
        SetupValidMethodsService(["MTH1", "MTH2", "MTH3"], ["TST1", "TST2", "TST3"]);

        _handler = new GenerateRequestsPreviewQueryHandler(
            new RequestGeneratorValidationService(_methodsServiceMock.Object));
    }

    [Trait("ModuleRequirement", "MR_5835")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task Preview_When_ValidQuery_Should_ReturnCorrectItemCount()
    {
        // Arrange
        var query = BuildValidQuery(amountToGenerate: 5);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(5);
    }

    [Trait("ModuleRequirement", "MR_5835")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task Preview_When_ValidQuery_Should_NumberItemsSequentially()
    {
        // Arrange
        var query = BuildValidQuery(amountToGenerate: 4);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Select(r => r.Id).Should().BeEquivalentTo([1, 2, 3, 4], opts => opts.WithStrictOrdering());
    }

    [Trait("ModuleRequirement", "MR_5828")]
    [Trait("ProductRequirement", "PR_1721")]
    [Theory]
    [InlineData("S", 6, 0, "", "S000000")]
    [InlineData("S", 6, 1, "", "S000001")]
    [InlineData("", 4, 100, "X", "0100X")]
    [InlineData("AB", 3, 5, "Z", "AB005Z")]
    public async Task Preview_When_HeadingAndTrailing_Should_FormatSampleIdCorrectly(
        string heading, int digits, int startOffset, string trailing, string expectedFirstSampleId)
    {
        // Arrange
        var query = new GenerateRequestsPreviewQuery(new GenerateRequestsDto
        {
            SampleIdHeadingCharacters = heading,
            SampleIdNumberOfDigits = digits,
            SampleIdStartOffset = startOffset,
            SampleIdTrailingCharacters = trailing,
            AmountToGenerate = 1,
            PreDilutionFactor = 1,
            Tests = [new TestDetails { MethodName = "MTH1", TestName = "TST1", Replicates = 1, InstrumentDilutionFactor = 1 }]
        });

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result[0].SampleId.Should().Be(expectedFirstSampleId);
    }

    [Trait("ModuleRequirement", "MR_5832")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task Preview_When_MultipleTests_Should_FormatTestsAsCommaSeparatedTestNames()
    {
        // Arrange
        var query = BuildValidQuery(amountToGenerate: 1);
        query = query with
        {
            GenerateRequestsDto = query.GenerateRequestsDto with
            {
                Tests =
                [
                    new TestDetails { MethodName = "MTH1", TestName = "TST1", Replicates = 1, InstrumentDilutionFactor = 1 },
                    new TestDetails { MethodName = "MTH2", TestName = "TST2", Replicates = 1, InstrumentDilutionFactor = 1 }
                ]
            }
        };

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result[0].Tests.Should().Be("TST1, TST2");
    }

    [Trait("ModuleRequirement", "MR_5830")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task Preview_When_TrayConfigDisabled_Should_SetTrayFieldsToNull()
    {
        // Arrange
        var query = BuildValidQuery(amountToGenerate: 3, placeSamplesInTrays: false);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().AllSatisfy(item =>
        {
            item.Tray.Should().BeNull();
            item.TrayPosition.Should().BeNull();
        });
    }

    [Trait("ModuleRequirement", "MR_5830")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task Preview_When_TrayConfigEnabled_Should_SetTrayAndPositionForEachSample()
    {
        // Arrange
        var query = BuildValidQuery(amountToGenerate: 3, placeSamplesInTrays: true, samplesPerTray: 10, trayStartOffset: 1);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().AllSatisfy(item =>
        {
            item.Tray.Should().NotBeNull();
            item.TrayPosition.Should().NotBeNull();
        });
        result[0].Tray.Should().Be("0001");
        result[0].TrayPosition.Should().Be("A1");
    }

    [Trait("ModuleRequirement", "MR_5830")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task Preview_When_TrayConfigEnabled_Should_AdvanceTrayAfterSamplesPerTrayExceeded()
    {
        // Arrange
        var query = BuildValidQuery(amountToGenerate: 4, placeSamplesInTrays: true, samplesPerTray: 3, trayStartOffset: 1);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result[0].Tray.Should().Be("0001");
        result[1].Tray.Should().Be("0001");
        result[2].Tray.Should().Be("0001");
        result[3].Tray.Should().Be("0002"); // 4th sample overflows into next tray
    }

    [Trait("ModuleRequirement", "MR_5830")]
    [Trait("ProductRequirement", "PR_1721")]
    [Theory]
    [InlineData(1, "A1")]
    [InlineData(10, "A10")]
    [InlineData(11, "B1")]
    [InlineData(20, "B10")]
    [InlineData(21, "C1")]
    [InlineData(50, "E10")]
    public async Task Preview_When_TrayConfigEnabled_Should_ConvertPositionToGridCorrectly(
        int sampleIndex, string expectedGridPosition)
    {
        // Arrange
        var query = BuildValidQuery(
            amountToGenerate: sampleIndex,
            placeSamplesInTrays: true,
            samplesPerTray: 50,
            trayStartOffset: 0);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert � the last item corresponds to sampleIndex-th position in the tray
        result[sampleIndex - 1].TrayPosition.Should().Be(expectedGridPosition);
    }

    [Trait("ModuleRequirement", "MR_5831")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task Preview_When_InvalidMethodName_Should_ThrowValidationException()
    {
        // Arrange � methods service returns empty, so method lookup fails
        _methodsServiceMock
            .Setup(x => x.GetMethods(
                It.IsAny<Expression<Func<Method, bool>>>(),
                It.IsAny<Expression<Func<Method, MethodValidationDto>>>()))
            .ReturnsAsync(new List<MethodValidationDto>());

        var query = BuildValidQuery();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(query, TestContext.Current.CancellationToken));
        exception.Message.Should().Contain("Invalid method name specified");
    }

    [Trait("ModuleRequirement", "MR_5831")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task Preview_When_InvalidTestName_Should_ThrowValidationException()
    {
        // Arrange � method exists but has no matching test
        SetupValidMethodsService(["MTH1"], []);

        var query = BuildValidQuery();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(query, TestContext.Current.CancellationToken));
        exception.Message.Should().Contain("Invalid test name specified");
    }

    [Trait("ModuleRequirement", "MR_5831")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task Preview_When_DuplicateTestInList_Should_ThrowValidationException()
    {
        // Arrange
        SetupValidMethodsService(["MTH1"], ["TST1"]);

        var query = new GenerateRequestsPreviewQuery(new GenerateRequestsDto
        {
            SampleIdHeadingCharacters = "S",
            SampleIdNumberOfDigits = 6,
            SampleIdStartOffset = 1,
            AmountToGenerate = 1,
            PreDilutionFactor = 1,
            Tests =
            [
                new TestDetails { MethodName = "MTH1", TestName = "TST1", Replicates = 1, InstrumentDilutionFactor = 1 },
                new TestDetails { MethodName = "MTH1", TestName = "TST1", Replicates = 2, InstrumentDilutionFactor = 1 }
            ]
        });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(query, TestContext.Current.CancellationToken));
        exception.Message.Should().NotBeNullOrEmpty();
    }

    [Trait("ModuleRequirement", "MR_5834")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task Preview_When_InvalidInstrumentDilutionFactor_Should_ThrowValidationException()
    {
        // Arrange � ImmunoCap with dilution factor 3 (not in allowed set)
        SetupValidMethodsService(["MTH1"], ["TST1"], AssayTechnology.ImmunoCap);

        var query = new GenerateRequestsPreviewQuery(new GenerateRequestsDto
        {
            SampleIdHeadingCharacters = "S",
            SampleIdNumberOfDigits = 6,
            SampleIdStartOffset = 1,
            AmountToGenerate = 1,
            PreDilutionFactor = 1,
            Tests = [new TestDetails { MethodName = "MTH1", TestName = "TST1", Replicates = 1, InstrumentDilutionFactor = 3 }]
        });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(query, TestContext.Current.CancellationToken));
        exception.Message.Should().Contain("Invalid instrument dilution factor");
    }

    [Trait("ModuleRequirement", "MR_5834")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task Preview_When_TotalDilutionBelowDefault_Should_ThrowValidationException()
    {
        // Arrange � prevent flag true, default = 10, pre=1 � instrument=1 = 1 < 10
        SetupValidMethodsService(["MTH1"], ["TST1"], AssayTechnology.ImmunoCap, preventBelowDefault: true, dilutionFactor: 10);

        var query = new GenerateRequestsPreviewQuery(new GenerateRequestsDto
        {
            SampleIdHeadingCharacters = "S",
            SampleIdNumberOfDigits = 6,
            SampleIdStartOffset = 1,
            AmountToGenerate = 1,
            PreDilutionFactor = 1,
            Tests = [new TestDetails { MethodName = "MTH1", TestName = "TST1", Replicates = 1, InstrumentDilutionFactor = 1 }]
        });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(query, TestContext.Current.CancellationToken));
        exception.Message.Should().Contain("Too low total dilution factors");
    }

    [Trait("ModuleRequirement", "MR_5835")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task Preview_When_ValidationFails_Should_ReturnNoPreviewItems()
    {
        // Arrange — invalid method, so validation throws before items are built
        _methodsServiceMock
            .Setup(x => x.GetMethods(
                It.IsAny<Expression<Func<Method, bool>>>(),
                It.IsAny<Expression<Func<Method, MethodValidationDto>>>()))
            .ReturnsAsync(new List<MethodValidationDto>());

        var query = BuildValidQuery();

        // Act & Assert — exception must be thrown and no partial list returned
        await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(query, TestContext.Current.CancellationToken));
    }

    private void SetupValidMethodsService(
        string[] methodNames,
        string[] testNames,
        AssayTechnology technology = AssayTechnology.ImmunoCap,
        bool preventBelowDefault = false,
        int dilutionFactor = 1)
    {
        var results = methodNames
            .Select(mn => new MethodValidationDto
            {
                MethodName = mn,
                Technology = technology,
                PreventDilutionFactorsBelowDefault = preventBelowDefault,
                Tests = testNames.Select(tn => new MethodTestValidationDto
                {
                    Name = tn,
                    DilutionMethod = DilutionMethod.InstrumentDilution,
                    DilutionFactor = dilutionFactor
                }).ToList()
            })
            .ToList();

        _methodsServiceMock
            .Setup(x => x.GetMethods(
                It.IsAny<Expression<Func<Method, bool>>>(),
                It.IsAny<Expression<Func<Method, MethodValidationDto>>>()))
            .ReturnsAsync(results);
    }

    private static GenerateRequestsPreviewQuery BuildValidQuery(
        int amountToGenerate = 3,
        bool placeSamplesInTrays = false,
        int samplesPerTray = 10,
        int trayStartOffset = 0)
    {
        return new GenerateRequestsPreviewQuery(new GenerateRequestsDto
        {
            SampleIdHeadingCharacters = "S",
            SampleIdNumberOfDigits = 6,
            SampleIdStartOffset = 1,
            PlaceSamplesInTrays = placeSamplesInTrays,
            TraysStartOffset = trayStartOffset,
            SamplesPerTray = samplesPerTray,
            AmountToGenerate = amountToGenerate,
            PreDilutionFactor = 1,
            Tests = [new TestDetails { MethodName = "MTH1", TestName = "TST1", Replicates = 1, InstrumentDilutionFactor = 1 }]
        });
    }
}

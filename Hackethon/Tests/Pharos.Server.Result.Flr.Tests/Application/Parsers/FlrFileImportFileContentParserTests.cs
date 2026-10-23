using System.Text;
using FluentAssertions;

namespace Pharos.Server.Result.Flr.Application.Parsers;

public class FlrFileImportFileContentParserTests
{
    [Fact]
    public void Parse_ReadsAllHeaderFields_AccordingToSpec()
    {
        // Arrange
        const string headerDateTime = "2023-11-15T01:00:00+01:00";
        const string formatVersion = "1.0.2";
        const string fileName = "C:\\somepath\\FLR_231115_225541_00123.LOG";
        const string instrumentModel = "TetrisMedium";
        const string instrumentSerial = "00123";
        const string instrumentSoftwareVersion = "1.0.0";
        const string assayRunId = "11111111-2222-3333-4444-555555555555";

        var flrContent = BuildFlrContent(
            "DateTime=" + headerDateTime,
            "FormatVersion=" + formatVersion,
            "FileName=\"" + fileName + "\"",
            "InstrumentModel=" + instrumentModel,
            "InstrumentSerial=" + instrumentSerial,
            "InstrumentSoftwareVersion=" + instrumentSoftwareVersion,
            "AssayRunId=" + assayRunId);

        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(flrContent)));

        // Act
        var result = new FlrFileImportFileContentParser().Parse(reader);

        // Assert
        result.HeaderInformation.DateTime.Should().Be(headerDateTime);
        result.HeaderInformation.FormatVersion.Should().Be(formatVersion);
        result.HeaderInformation.FileName.Should().Be(fileName);
        result.HeaderInformation.InstrumentModel.Should().Be(instrumentModel);
        result.HeaderInformation.InstrumentSerial.Should().Be(instrumentSerial);
        result.HeaderInformation.InstrumentSoftwareVersion.Should().Be(instrumentSoftwareVersion);
        result.HeaderInformation.AssayRunId.Should().Be(assayRunId);
    }

    [Fact]
    public void Parse_ReadsFileName_WhenTypographicQuotesAreUsed()
    {
        // Arrange
        const string fileName = "C:\\somepath\\FLR_231115_225541_00123.LOG";
        var flrContent = BuildFlrContent(
            "DateTime=2023-11-15T01:00:00+01:00",
            "FormatVersion=1.0.2",
            "FileName=“" + fileName + "”",
            "InstrumentModel=TetrisMedium",
            "InstrumentSerial=00123",
            "InstrumentSoftwareVersion=1.0.0",
            "AssayRunId=11111111-2222-3333-4444-555555555555");

        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(flrContent)));

        // Act
        var result = new FlrFileImportFileContentParser().Parse(reader);

        // Assert
        result.HeaderInformation.FileName.Should().Be(fileName);
    }

    [Fact]
    public void Parse_ThrowsInvalidDataException_ForUnknownHeaderKey()
    {
        // Arrange
        var flrContent = BuildFlrContent(
            "DateTime=2023-11-15T01:00:00+01:00",
            "FormatVersion=1.0.2",
            "FileName=\"C:\\somepath\\FLR_231115_225541_00123.LOG\"",
            "InstrumentModel=TetrisMedium",
            "InstrumentSerial=00123",
            "InstrumentSoftwareVersion=1.0.0",
            "AssayRunId=11111111-2222-3333-4444-555555555555",
            "UnknownKey=abc");

        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(flrContent)));

        // Act
        var act = () => new FlrFileImportFileContentParser().Parse(reader);

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*Unknown FLR file header information*");
    }

    [Fact]
    public void Parse_ParsesSupportedEntries_AndSkipsUnsupportedMeasurementTypes()
    {
        // Arrange
        var assayEntry = CreateEntryLine(FlrMeasurementType.Unknown, new Dictionary<string, string>
        {
            [FlrEntryColumns.GroupId] = "grp-1",
            [FlrEntryColumns.CmrTestId] = "cmr-1",
            [FlrEntryColumns.ReplicateId] = "rep-1",
            [FlrEntryColumns.MeasurementValue] = "12.5"
        });

        var blankEntry = CreateEntryLine(FlrMeasurementType.RinseBlank, new Dictionary<string, string>
        {
            [FlrEntryColumns.GroupId] = "",
            [FlrEntryColumns.MeasurementValue] = "4.2"
        });

        var unsupportedEntry = CreateEntryLine(FlrMeasurementType.DarkBlank);

        var flrContent = BuildFlrContent(
            [
                assayEntry,
                blankEntry,
                unsupportedEntry
            ],
            "DateTime=2023-11-15T01:00:00+01:00",
            "FormatVersion=1.0.2",
            "FileName=\"C:\\somepath\\FLR_231115_225541_00123.LOG\"",
            "InstrumentModel=TetrisMedium",
            "InstrumentSerial=00123",
            "InstrumentSoftwareVersion=1.0.0",
            "AssayRunId=11111111-2222-3333-4444-555555555555");

        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(flrContent)));

        // Act
        var result = new FlrFileImportFileContentParser().Parse(reader);

        // Assert
        result.EntityList.Should().HaveCount(2);
        result.EntityList[0].Should().BeOfType<AssayResultFlrEntry>();
        result.EntityList[1].Should().BeOfType<BlankResultFlrEntry>();
    }

    [Fact]
    public void Parse_ReturnsEmptyEntityList_WhenEntriesSectionHasOnlyHeader()
    {
        // Arrange
        var flrContent = BuildFlrContent(
            [],
            "DateTime=2023-11-15T01:00:00+01:00",
            "FormatVersion=1.0.2",
            "FileName=\"C:\\somepath\\FLR_231115_225541_00123.LOG\"",
            "InstrumentModel=TetrisMedium",
            "InstrumentSerial=00123",
            "InstrumentSoftwareVersion=1.0.0",
            "AssayRunId=11111111-2222-3333-4444-555555555555");

        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(flrContent)));

        // Act
        var act = () => new FlrFileImportFileContentParser().Parse(reader);

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage("*At least one FLR entry must be parsed from the file.*");
    }

    [Fact]
    public void Parse_ThrowsFormatException_WhenFirstLineIsNotSep()
    {
        // Arrange
        var lines = new List<string>
        {
            "[HEADER]",
            "DateTime=2023-11-15T01:00:00+01:00",
            "FormatVersion=1.0.2",
            "FileName=\"C:\\somepath\\FLR_231115_225541_00123.LOG\"",
            "InstrumentModel=TetrisMedium",
            "InstrumentSerial=00123",
            "InstrumentSoftwareVersion=1.0.0",
            "AssayRunId=11111111-2222-3333-4444-555555555555",
            "[ENTRIES]",
            string.Join(";", FlrEntryColumns.All.Select(x => x.ColumnName)),
            "[END]"
        };

        var flrContent = string.Join(Environment.NewLine, lines);
        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(flrContent)));

        // Act
        var act = () => new FlrFileImportFileContentParser().Parse(reader);

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage("*first non-empty line*SEP=;*");
    }

    [Fact]
    public void Parse_ThrowsFormatException_WhenSectionsAreInWrongOrder()
    {
        // Arrange
        var lines = new List<string>
        {
            "SEP=;",
            "[ENTRIES]"
        };

        var flrContent = string.Join(Environment.NewLine, lines);
        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(flrContent)));

        // Act
        var act = () => new FlrFileImportFileContentParser().Parse(reader);

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage("*[HEADER] section expected*");
    }

    [Fact]
    public void Parse_ParsesSuccessfully_WhenEndSectionIsMissing()
    {
        // Arrange
        var lines = new List<string>
        {
            "SEP=;",
            "[HEADER]",
            "DateTime=2023-11-15T01:00:00+01:00",
            "FormatVersion=1.0.2",
            "FileName=\"C:\\somepath\\FLR_231115_225541_00123.LOG\"",
            "InstrumentModel=TetrisMedium",
            "InstrumentSerial=00123",
            "InstrumentSoftwareVersion=1.0.0",
            "AssayRunId=11111111-2222-3333-4444-555555555555",
            "[ENTRIES]",
            string.Join(";", FlrEntryColumns.All.Select(x => x.ColumnName)),
            CreateEntryLine(FlrMeasurementType.Unknown)
        };

        var flrContent = string.Join(Environment.NewLine, lines);
        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(flrContent)));

        // Act
        var result = new FlrFileImportFileContentParser().Parse(reader);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void Parse_ThrowsFormatException_WhenContentExistsAfterEndSection()
    {
        // Arrange
        var lines = new List<string>
        {
            "SEP=;",
            "[HEADER]",
            "DateTime=2023-11-15T01:00:00+01:00",
            "FormatVersion=1.0.2",
            "FileName=\"C:\\somepath\\FLR_231115_225541_00123.LOG\"",
            "InstrumentModel=TetrisMedium",
            "InstrumentSerial=00123",
            "InstrumentSoftwareVersion=1.0.0",
            "AssayRunId=11111111-2222-3333-4444-555555555555",
            "[ENTRIES]",
            string.Join(";", FlrEntryColumns.All.Select(x => x.ColumnName)),
            "[END]",
            "trailing-text"
        };

        var flrContent = string.Join(Environment.NewLine, lines);
        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(flrContent)));

        // Act
        var act = () => new FlrFileImportFileContentParser().Parse(reader);

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage("*Unexpected content after [END] section*");
    }

    [Fact]
    public void Parse_ParsesAllAssayResultFlrEntryProperties_ExceptRuntimeCounters()
    {
        // Arrange
        var entryLine = CreateEntryLine(FlrMeasurementType.Unknown, new Dictionary<string, string>
        {
            [FlrEntryColumns.GroupId] = "group-77",
            [FlrEntryColumns.CmrTestId] = "cmr-77",
            [FlrEntryColumns.ReplicateId] = "rep-77",
            [FlrEntryColumns.MethodName] = "method-77",
            [FlrEntryColumns.Technology] = ((int)FlrTechnology.EliA).ToString(),
            [FlrEntryColumns.SampleId] = "sample-77",
            [FlrEntryColumns.SampleBarcode] = "sample-barcode-77",
            [FlrEntryColumns.TubeId] = "tube-77",
            [FlrEntryColumns.SampleType] = ((int)FlrSampleType.QualityControl).ToString(),
            [FlrEntryColumns.SampleTubeType] = "3",
            [FlrEntryColumns.SampleHighPriority] = "1",
            [FlrEntryColumns.TestName] = "test-name-77",
            [FlrEntryColumns.TestBarcode] = "test-barcode-77",
            [FlrEntryColumns.TestUdi] = "udi-77",
            [FlrEntryColumns.TestExpired] = "TRUE",
            [FlrEntryColumns.TestPositionIrw] = "IRW_C49",
            [FlrEntryColumns.TestPositionErw] = "ERW_B88",
            [FlrEntryColumns.AnalyticalRunId] = "analytical-77",
            [FlrEntryColumns.CustomRunId] = "custom-77",
            [FlrEntryColumns.ConjugateBarcode] = "conjugate-77",
            [FlrEntryColumns.ConjugateLocation] = "FS_A9, C7_8",
            [FlrEntryColumns.ConjugateExpired] = "true",
            [FlrEntryColumns.DevelopmentBarcode] = "development-77",
            [FlrEntryColumns.DevelopmentLocation] = "C1_9",
            [FlrEntryColumns.DevelopmentExpired] = "1",
            [FlrEntryColumns.StopBarcode] = "stop-77",
            [FlrEntryColumns.StopExpired] = "0",
            [FlrEntryColumns.WashConcentrateBarcode] = "wash-conc-77",
            [FlrEntryColumns.WashAddBarcode] = "wash-add-77",
            [FlrEntryColumns.MeasurementType] = ((int)FlrMeasurementType.Unknown).ToString(),
            [FlrEntryColumns.MeasurementValue] = "45.6",
            [FlrEntryColumns.MeasurementDateTime] = "2024-01-01T10:00:00+02:00",
            [FlrEntryColumns.FluorometerStatus] = "0",
            [FlrEntryColumns.PreDilution] = "4",
            [FlrEntryColumns.InstrumentDilution] = "5",
            [FlrEntryColumns.DilutionPlateBarcode] = "plate-77",
            [FlrEntryColumns.Dilstep1Barcode] = "diluent1-77",
            [FlrEntryColumns.Dilstep2Barcode] = "diluent2-77",
            [FlrEntryColumns.Dilstep3Barcode] = "diluent3-77",
            [FlrEntryColumns.Dilstep1DiluentPos] = "C1_11",
            [FlrEntryColumns.Dilstep2DiluentPos] = "C1_12",
            [FlrEntryColumns.Dilstep3DiluentPos] = "C1_13",
            [FlrEntryColumns.Dilstep1DiluentExpired] = "1",
            [FlrEntryColumns.Dilstep2DiluentExpired] = "0",
            [FlrEntryColumns.Dilstep3DiluentExpired] = "true",
        });

        var flrContent = BuildFlrContent(
            [entryLine],
            "DateTime=2023-11-15T01:00:00+01:00",
            "FormatVersion=1.0.2",
            "FileName=\"C:\\somepath\\FLR_231115_225541_00123.LOG\"",
            "InstrumentModel=TetrisMedium",
            "InstrumentSerial=00123",
            "InstrumentSoftwareVersion=1.0.0",
            "AssayRunId=11111111-2222-3333-4444-555555555555");

        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(flrContent)));

        // Act
        var result = new FlrFileImportFileContentParser().Parse(reader);
        var entry = result.EntityList.Should().ContainSingle().Subject.Should().BeOfType<AssayResultFlrEntry>().Subject;

        // Assert
        entry.GroupId.Should().Be("group-77");
        entry.CmrTestId.Should().Be("cmr-77");
        entry.ReplicateId.Should().Be("rep-77");
        entry.MethodName.Should().Be("method-77");
        entry.SampleId.Should().Be("sample-77");
        entry.SampleBarcode.Should().Be("sample-barcode-77");
        entry.TubeId.Should().Be("tube-77");
        entry.SampleType.Should().Be(FlrSampleType.QualityControl);
        entry.SampleTubeType.Should().Be(3);
        entry.SampleHighPriority.Should().BeTrue();
        entry.TestName.Should().Be("test-name-77");
        entry.TestBarcode.Should().Be("test-barcode-77");
        entry.TestUdi.Should().Be("udi-77");
        entry.TestExpired.Should().BeTrue();
        entry.IrwRow.Should().Be(3);
        entry.IrwSpoke.Should().Be(49);
        entry.ErwRow.Should().Be(2);
        entry.ErwSpoke.Should().Be(88);
        entry.AnalyticalRunId.Should().Be("analytical-77");
        entry.CustomRunId.Should().Be("custom-77");
        entry.ConjugateBarcode.Should().Be("conjugate-77");
        entry.ConjugateWell.Should().Be(8);
        entry.ConjugateExpired.Should().BeTrue();
        entry.DevelopmentBarcode.Should().Be("development-77");
        entry.DevelopmentWell.Should().Be(9);
        entry.DevelopmentExpired.Should().BeTrue();
        entry.StopBarcode.Should().Be("stop-77");
        entry.StopExpired.Should().BeFalse();
        entry.WashConcentrationBarcode.Should().Be("wash-conc-77");
        entry.WashAddBarcode.Should().Be("wash-add-77");
        entry.MeasurementType.Should().Be(FlrMeasurementType.Unknown);
        entry.MeasurementValue.Should().Be(45.6);
        entry.MeasurementDateAndTime.ToDateTimeOffset().Should().Be(new DateTimeOffset(2024, 1, 1, 8, 0, 0, TimeSpan.Zero));
        entry.PreDilution.Should().Be(4);
        entry.InstrumentDilution.Should().Be(5);
        entry.DilutionPlateBarcode.Should().Be("plate-77");
        entry.DiluentBarcode1.Should().Be("diluent1-77");
        entry.DiluentBarcode2.Should().Be("diluent2-77");
        entry.DiluentBarcode3.Should().Be("diluent3-77");
        entry.DiluentWell1.Should().Be(11);
        entry.DiluentWell2.Should().Be(12);
        entry.DiluentWell3.Should().Be(13);
        entry.DiluentExpired1.Should().BeTrue();
        entry.DiluentExpired2.Should().BeFalse();
        entry.DiluentExpired3.Should().BeTrue();
        entry.IsAborted.Should().BeFalse();
    }

    private static string BuildFlrContent(string[] entryLines, params string[] headerLines)
    {
        var lines = new List<string>
        {
            "SEP=;",
            "[HEADER]",
        };

        lines.AddRange(headerLines);

        lines.Add("[ENTRIES]");
        lines.Add(string.Join(";", FlrEntryColumns.All.Select(x => x.ColumnName)));
        lines.AddRange(entryLines);
        lines.Add("[END]");

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildFlrContent(params string[] headerLines)
    {
        return BuildFlrContent([CreateEntryLine(FlrMeasurementType.Unknown)], headerLines);
    }

    private static string CreateEntryLine(FlrMeasurementType measurementType, IReadOnlyDictionary<string, string>? overrides = null)
    {
        var values = FlrEntryColumns.All.ToDictionary(c => c.ColumnName, _ => string.Empty);

        values[FlrEntryColumns.GroupId] = "group-1";
        values[FlrEntryColumns.CmrTestId] = "cmr-1";
        values[FlrEntryColumns.ReplicateId] = "rep-1";
        values[FlrEntryColumns.MethodName] = "method-1";
        values[FlrEntryColumns.Technology] = ((int)FlrTechnology.ImmunoCap).ToString();
        values[FlrEntryColumns.SampleId] = "sample-1";
        values[FlrEntryColumns.SampleType] = ((int)FlrSampleType.Sample).ToString();
        values[FlrEntryColumns.TestBarcode] = "test-1";
        values[FlrEntryColumns.DevelopmentBarcode] = "dev-1";
        values[FlrEntryColumns.DevelopmentLocation] = "C1_1";
        values[FlrEntryColumns.MeasurementType] = ((int)measurementType).ToString();
        values[FlrEntryColumns.MeasurementValue] = "1.5";
        values[FlrEntryColumns.MeasurementDateTime] = "2023-11-15T01:00:00+01:00";
        values[FlrEntryColumns.FluorometerStatus] = "0";
        values[FlrEntryColumns.TestPositionIrw] = "IRW_A1";
        values[FlrEntryColumns.TestPositionErw] = "ERW_A1";

        if (overrides != null)
        {
            foreach (var (key, value) in overrides)
            {
                values[key] = value;
            }
        }

        return string.Join(";", FlrEntryColumns.All.Select(c => values[c.ColumnName]));
    }
}

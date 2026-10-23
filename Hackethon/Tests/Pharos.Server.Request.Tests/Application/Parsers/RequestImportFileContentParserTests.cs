namespace Pharos.Server.Request.Application.Parsers;

public class RequestImportFileContentParserTests
{
    [Fact]
    public void Parse_When_ContentIsValid_ShouldReturnImportedRequests()
    {
        // Arrange
        var parser = new RequestImportFileContentParser();
        var csv = "SampleId,MethodName,TestName\r\nS1,M1,T1";

        // Act
        var result = parser.Parse(csv, "requests.csv");

        // Assert
        result.Samples.Should().HaveCount(1);
        result.Samples.Single().SampleId.Should().Be("S1");
        result.Samples.Single().Tests.Should().HaveCount(1);
        result.Samples.Single().Tests.Single().MethodName.Should().Be("M1");
        result.Samples.Single().Tests.Single().TestName.Should().Be("T1");
    }

    [Fact]
    public void Parse_When_HeaderContainsUnknownColumn_ShouldThrowArgumentExceptionWithHeaderParam()
    {
        // Arrange
        var parser = new RequestImportFileContentParser();
        var csv = "SampleId,MethodName,TestName,UnknownColumn\r\nS1,M1,T1,X";

        // Act
        var act = () => parser.Parse(csv, "requests.csv");

        // Assert
        var exception = act.Should().Throw<ArgumentException>().Which;
        exception.ParamName.Should().Be("headerRow");
        exception.Message.Should().Contain("unknown column");
        exception.Message.Should().Contain("UnknownColumn");
        exception.Message.Should().Contain("headerColumnText");
    }

    [Fact]
    public void Parse_When_MandatoryColumnMissing_ShouldThrowArgumentExceptionWithMandatoryParam()
    {
        // Arrange
        var parser = new RequestImportFileContentParser();
        var csv = "SampleId,MethodName\r\nS1,M1";

        // Act
        var act = () => parser.Parse(csv, "requests.csv");

        // Assert
        var exception = act.Should().Throw<ArgumentException>().Which;
        exception.ParamName.Should().Be("headerRow");
        exception.Message.Should().Contain("missing mandatory column");
        exception.Message.Should().Contain("TestName");
        exception.Message.Should().Contain("mandatoryColumnName");
    }

    [Fact]
    public void Parse_When_RowHasWrongColumnCount_ShouldThrowArgumentExceptionWithRequestRowParam()
    {
        // Arrange
        var parser = new RequestImportFileContentParser();
        var csv = "SampleId,MethodName,TestName\r\nS1,M1";

        // Act
        var act = () => parser.Parse(csv, "requests.csv");

        // Assert
        var exception = act.Should().Throw<ArgumentException>().Which;
        exception.ParamName.Should().Be("requestRows");
        exception.Message.Should().Contain("row 1");
        exception.Message.Should().Contain("expected 3");
        exception.Message.Should().Contain("requestRow");
    }

    [Fact]
    public void Parse_When_RowHasMoreColumnsThanHeader_ShouldThrowArgumentExceptionWithRequestRowParam()
    {
        // Arrange
        var parser = new RequestImportFileContentParser();
        var csv = "SampleId,MethodName,TestName\r\nS1,M1,T1,Extra";

        // Act
        var act = () => parser.Parse(csv, "requests.csv");

        // Assert
        var exception = act.Should().Throw<ArgumentException>().Which;
        exception.ParamName.Should().Be("requestRows");
        exception.Message.Should().Contain("row 1");
        exception.Message.Should().Contain("has 4 column(s), expected 3");
        exception.Message.Should().Contain("requestRow");
    }

    [Fact]
    public void Parse_When_RowContainsImpossibleSampleDate_ShouldThrowArgumentExceptionWithColumnContext()
    {
        // Arrange
        var parser = new RequestImportFileContentParser();
        var csv = "SampleId,MethodName,TestName,SampleDate\r\nS1,M1,T1,1900-01-00";

        // Act
        var act = () => parser.Parse(csv, "requests.csv");

        // Assert
        var exception = act.Should().Throw<ArgumentException>().Which;
        exception.ParamName.Should().Be("requestRows");
        exception.Message.Should().Contain("row 1");
        exception.Message.Should().Contain("column 4");
        exception.Message.Should().Contain("SampleDate");
        exception.InnerException.Should().BeOfType<FormatException>();
    }
}

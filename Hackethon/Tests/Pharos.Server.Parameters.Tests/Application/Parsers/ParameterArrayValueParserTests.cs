using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;

namespace Pharos.Server.Parameters.Application.Parsers;

public class ParameterArrayValueParserTests
{
    private readonly Method _method;
    private readonly IntParser _intParser;
    private readonly StringParser _stringParser;
    private readonly int _index;

    public ParameterArrayValueParserTests()
    {
        _index = 1;
        _method = new Method
        {
            ClassTableBelows = new List<MethodClassTableBelow>(),
        };

        _intParser = new IntParser();
        _stringParser = new StringParser();
    }
    //test for ParameterArrayValueParser

    [Fact]
    public void ParameterArrayValueParser_HasToExistTrue_InRange_ParserDefaultTrue_ShouldReturnGivenInputValue_Int()
    {
        string[] stringArray = { "1", "2" };
        var sut = new ParameterArrayValueParser<Method, int>(1, true, new ParserDefault<int>(99, true), (model, value) => model.MethodVersion = value, _intParser);

        sut.Parse(_method, stringArray);
        _method.MethodVersion.Should().Be(2);
    }

    [Fact]
    public void ParameterArrayValueParser_ShouldThrow_FailInt()
    {
        string[] stringArray = { "a" };
        var sut = new ParameterArrayValueParser<Method, int>(0, true, new ParserDefault<int>(99, true), (model, value) => model.MethodVersion = value, _intParser);

        Assert.Throws<FormatException>(() => sut.Parse(_method, stringArray));
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistTrue_OutOfRange_ParserDefaultTrue_ShouldReturnGivenDefaultValue_Int()
    {
        string[] stringArray = { "1" };
        var sut = new ParameterArrayValueParser<Method, int>(1, true, new ParserDefault<int>(99, true), (model, value) => model.MethodVersion = value, _intParser);
        sut.Parse(_method, stringArray);

        _method.MethodVersion.Should().Be(99);
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistFalse_LastValueMissing_ParserDefaultTrue_ShouldReturnGivenDefaultValue_Int()
    {
        var s = "1;";
        var sArray = s.Split(";");
        var sut = new ParameterArrayValueParser<Method, int>(_index, false, new ParserDefault<int>(99, true), (model, value) => model.MethodVersion = value, _intParser);

        sut.Parse(_method, sArray);
        _method.MethodVersion.Should().Be(99);
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistTrue_InRange_NoParserDefault_ShouldReturnGivenInputValue_Int()
    {
        string[] stringArray = { "1", "2" };
        var sut = new ParameterArrayValueParser<Method, int>(_index, true, (model, value) => model.MethodVersion = value, _intParser);

        sut.Parse(_method, stringArray);
        _method.MethodVersion.Should().Be(2);
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistTrue_InRange_ParserDefaultFalse_ShouldReturnGivenInputValue_Int()
    {
        string[] stringArray = { "1", "2" };
        var sut = new ParameterArrayValueParser<Method, int>(_index, true, new ParserDefault<int>(99, true), (model, value) => model.MethodVersion = value, _intParser);

        sut.Parse(_method, stringArray);
        _method.MethodVersion.Should().Be(2);
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistTrue_OutOfRange_NoParserDefault_ShouldThrow_Int()
    {
        string[] stringArray = { "1" };

        var sut = new ParameterArrayValueParser<Method, int>(_index, true, (model, value) => model.MethodVersion = value, _intParser);

        var ex = Assert.Throws<InvalidDataException>(() => sut.Parse(_method, stringArray));
        Assert.Contains("Missing Value, HasToExist is true and ParserDefault False or not specified, or fail to parse", ex.Message);
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistTrue_OutOfRange_ParserDefaultFalse_ShouldThrow_Int()
    {
        string[] stringArray = { "1" };
        var sut = new ParameterArrayValueParser<Method, int>(_index, true, new ParserDefault<int>(99, false), (model, value) => model.MethodVersion = value, _intParser);

        var ex = Assert.Throws<InvalidDataException>(() => sut.Parse(_method, stringArray));
        Assert.Contains("Missing Value, HasToExist is true and ParserDefault False or not specified, or fail to parse", ex.Message);
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistFalse_InRange_ParserDefaultTrue_ShouldReturnGivenInputValue_Int()
    {
        string[] stringArray = { "1", "2" };
        var sut = new ParameterArrayValueParser<Method, int>(_index, false, new ParserDefault<int>(99, true), (model, value) => model.MethodVersion = value, _intParser);

        sut.Parse(_method, stringArray);
        _method.MethodVersion.Should().Be(2);
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistFalse_OutOfRange_ParserDefaultTrue_ShouldReturnGivenDefaultValue_Int()
    {
        var s = "1";
        var sArray = s.Split(";");
        var sut = new ParameterArrayValueParser<Method, int>(_index, false, new ParserDefault<int>(99, true), (model, value) => model.MethodVersion = value, _intParser);

        sut.Parse(_method, sArray);
        _method.MethodVersion.Should().Be(99);
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistFalse_OutOfRange_ParserDefaultFalse_ShouldReturnDefaultValue_Int()
    {
        string[] stringArray = { "1" };
        var sut = new ParameterArrayValueParser<Method, int>(_index, false, new ParserDefault<int>(99, false), (model, value) => model.MethodVersion = value, _intParser);

        sut.Parse(_method, stringArray);
        _method.MethodVersion.Should().Be(0);
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistFalse_OutOfRange_NoParserDefault_ShouldReturnDefaultValue_Int()
    {
        string[] stringArray = { "1" };
        var sut = new ParameterArrayValueParser<Method, int>(_index, false, (model, value) => model.MethodVersion = value, _intParser);

        sut.Parse(_method, stringArray);
        _method.MethodVersion.Should().Be(0);
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistFalse_MiddleValueMissing_NoParserDefault_ShouldReturnDefaultValue_Int()
    {
        string[] stringArray = { "1", "", "2" };
        var sut = new ParameterArrayValueParser<Method, int>(_index, false, (model, value) => model.MethodVersion = value, _intParser);

        sut.Parse(_method, stringArray);
        _method.MethodVersion.Should().Be(0);
    }

    //for string

    [Fact]
    public void ParameterArrayValueParser_HasToExistFalse_LastValueMissing_ParserDefaultTrue_ShouldReturnGivenDefaultValue_String()
    {
        var s = "1;";
        var sArray = s.Split(";");
        string[] stringArray = { "1", "" };
        var sut = new ParameterArrayValueParser<Method, string>(_index, false, new ParserDefault<string>("99", true), (model, value) => model.FullMethodName = value, _stringParser);

        sut.Parse(_method, sArray);
        _method.FullMethodName.Should().Be("99");
        sArray.Should().Equal(stringArray);
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistFalse_FirstValueMissing_ParserDefaultTrue_ShouldReturnGivenDefaultValue_String()
    {
        var s = ";1";
        var sArray = s.Split(";");
        var sut = new ParameterArrayValueParser<Method, string>(0, false, new ParserDefault<string>("99", true), (model, value) => model.FullMethodName = value, _stringParser);

        sut.Parse(_method, sArray);
        _method.FullMethodName.Should().Be("99");
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistFalse_MiddleValueMissing_ParserDefaultTrue_ShouldReturnGivenDefaultValue_String()
    {
        string[] stringArray = { "", "1", "", "5" };
        var sut = new ParameterArrayValueParser<Method, string>(2, false, new ParserDefault<string>("99", true), (model, value) => model.FullMethodName = value, _stringParser);

        sut.Parse(_method, stringArray);
        _method.FullMethodName.Should().Be("99");
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistTrue_InRange_ParserDefaultTrue_ShouldReturnGivenInputValue_String()
    {
        string[] stringArray = { "1", "2" };
        var sut = new ParameterArrayValueParser<Method, string>(_index, true, new ParserDefault<string>("99", true), (model, value) => model.FullMethodName = value, _stringParser);

        sut.Parse(_method, stringArray);
        _method.FullMethodName.Should().Be("2");
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistTrue_InRange_NoParserDefault_ShouldReturnGivenInputValue_String()
    {
        string[] stringArray = { "1", "2" };
        var sut = new ParameterArrayValueParser<Method, string>(_index, true, new ParserDefault<string>("99", true), (model, value) => model.FullMethodName = value, _stringParser);

        sut.Parse(_method, stringArray);
        _method.FullMethodName.Should().Be("2");
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistTrue_OutOfRange_NoParserDefault_ShouldThrow_String()
    {
        string[] stringArray = { "1" };
        var sut = new ParameterArrayValueParser<Method, string>(_index, true, (model, value) => model.FullMethodName = value, _stringParser);

        var ex = Assert.Throws<InvalidDataException>(() => sut.Parse(_method, stringArray));
        Assert.Contains("Missing Value, HasToExist is true and ParserDefault False or not specified, or fail to parse", ex.Message);
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistTrue_OutOfRange_ParserDefaultTrue_ShouldReturnGivenDefaultValue_String()
    {
        string[] stringArray = { "1" };
        var sut = new ParameterArrayValueParser<Method, string>(_index, true, new ParserDefault<string>("99", true), (model, value) => model.FullMethodName = value, _stringParser);

        sut.Parse(_method, stringArray);
        _method.FullMethodName.Should().Be("99");
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistFalse_InRange_ParserDefaultTrue_ShouldReturnGivenInputValue_String()
    {
        string[] stringArray = { "1", "2" };
        var sut = new ParameterArrayValueParser<Method, string>(_index, true, new ParserDefault<string>("99", true), (model, value) => model.FullMethodName = value, _stringParser);

        sut.Parse(_method, stringArray);
        _method.FullMethodName.Should().Be("2");
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistFalse_OutOfRange_ParserDefaultTrue_ShouldReturnGivenDefaultValue_String()
    {
        string[] stringArray = { "1" };
        var sut = new ParameterArrayValueParser<Method, string>(_index, false, new ParserDefault<string>("99", true), (model, value) => model.FullMethodName = value, _stringParser);

        sut.Parse(_method, stringArray);
        _method.FullMethodName.Should().Be("99");
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistFalse_OutOfRange_ParserDefaultFalse_ShouldReturnDefaultValue_String()
    {
        string[] stringArray = { "1" };
        var sut = new ParameterArrayValueParser<Method, string>(_index, false, new ParserDefault<string>("99", false), (model, value) => model.FullMethodName = value, _stringParser);

        sut.Parse(_method, stringArray);
        _method.FullMethodName.Should().BeNull();
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistFalse_OutOfRange_NoParserDefault_ShouldReturnDefaultValue_String()
    {
        string[] stringArray = { "1" };
        var sut = new ParameterArrayValueParser<Method, string>(_index, false, (model, value) => model.FullMethodName = value, _stringParser);

        sut.Parse(_method, stringArray);
        _method.FullMethodName.Should().BeNull();
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistTrue_OutOfRange_ParserDefaultFalse_ShouldThrow_String()
    {
        string[] stringArray = { "1" };
        var sut = new ParameterArrayValueParser<Method, string>(_index, true, new ParserDefault<string>("99", false), (model, value) => model.FullMethodName = value, _stringParser);

        var ex = Assert.Throws<InvalidDataException>(() => sut.Parse(_method, stringArray));
        Assert.Contains("Missing Value, HasToExist is true and ParserDefault False or not specified, or fail to parse", ex.Message);
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistTrue_InRange_ParserDefaultFalse_ShouldReturnGivenInputValue_String()
    {
        string[] stringArray = { "1", "2" };
        var sut = new ParameterArrayValueParser<Method, string>(_index, true, new ParserDefault<string>("99", false), (model, value) => model.FullMethodName = value, _stringParser);

        sut.Parse(_method, stringArray);
        _method.FullMethodName.Should().Be("2");
    }

    [Fact]
    public void ParameterArrayValueParser_HasToExistTrue_null_NoParserDefault_ShouldThrow_String()
    {
        var stringArray = Array.Empty<string>();
        var sut = new ParameterArrayValueParser<Method, string>(_index, true, (model, value) => model.FullMethodName = value, _stringParser);

        var ex = Assert.Throws<InvalidDataException>(() => sut.Parse(_method, stringArray));
        Assert.Contains("Missing Value, HasToExist is true and ParserDefault False or not specified, or fail to parse", ex.Message);
    }
}
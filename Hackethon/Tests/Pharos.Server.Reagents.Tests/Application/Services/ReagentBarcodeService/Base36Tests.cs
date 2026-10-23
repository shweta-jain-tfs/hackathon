namespace Pharos.Server.Reagents.Application.Services.ReagentBarcodeService;

public class Base36Tests
{
    [Theory]
    [InlineData("", 0)]
    [InlineData("0", 0)]
    [InlineData("A", 10)]
    [InlineData("Z", 35)]
    [InlineData("C5", 437)]
    [InlineData("AC5", 13397)]
    public void DecodeIntFromString(string input, int output)
    {
        var number = Base36.DecodeInt(input);
        number.Should().Be(output);
    }

    [Theory]
    [InlineData('0', 0)]
    [InlineData('5', 5)]
    [InlineData('A', 10)]
    [InlineData('Z', 35)]
    public void DecodeIntFromChar(char input, int output)
    {
        var number = Base36.DecodeInt(input);
        number.Should().Be(output);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("+")]
    [InlineData(" ")]
    public void DecodeWrongStringFormat(string input)
    {
        Assert.Throws<ArgumentException>(() => Base36.DecodeInt(input));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1295, "ZZ")]
    [InlineData(1296, "ZZ")]
    [InlineData(9, "9")]
    [InlineData(10, "A")]
    [InlineData(361, "A1")]
    public void EncodeInt(int input, string output)
    {
        var number = Base36.EncodeInt(input);
        number.Should().Be(output);
    }
}

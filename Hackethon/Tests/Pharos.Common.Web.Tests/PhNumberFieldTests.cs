using AngleSharp.Dom;
using Bunit;
using Pharos.Common.Web.Components.FormElements;

namespace Pharos.Common.Web.Tests;

public class PhNumberFieldTests : BunitContext
{
    public PhNumberFieldTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<PhNumberField<T>> RenderNumberField<T>(
        Action<T> onChanged,
        T? initialValue = default,
        string? culture = "en-US",
        int maxDigits = 0,
        bool valueChangedOnInput = false,
        int debounce = 50,
        bool allowNegativeNumbers = false,
        int? maxDecimals = null)
    {
        return Render<PhNumberField<T>>(parameters => parameters
            .Add(p => p.Value, initialValue)
            .Add(p => p.Culture, culture)
            .Add(p => p.MaxNumberOfDigits, maxDigits)
            .Add(p => p.ValueChangedOnInput, valueChangedOnInput)
            .Add(p => p.Debounce, debounce)
            .Add(p => p.AllowNegativeNumbers, allowNegativeNumbers)
            .Add(p => p.MaxDecimals, maxDecimals)
            .Add(p => p.ValueChanged, onChanged)
        );
    }

    private static void TypeAndCommit(IElement input, string value)
    {
        // oninput updates TextValue, onchange reads TextValue and raises ValueChanged
        input.Input(value);
        input.Change(value);
    }

    [Fact]
    public void Double_OnChange_ReturnsEnteredValue()
    {
        double? capturedValue = null;

        var cut = RenderNumberField<double>(
            onChanged: v => capturedValue = v
        );

        var input = cut.Find("input");
        var text = "123.45";

        TypeAndCommit(input, text);

        capturedValue.Should().Be(123.45);
        input.GetAttribute("value").Should().Be(text);
    }

    [Fact]
    public async Task Double_OnInput_RaisesValueAfterDebounce()
    {
        double? capturedValue = null;
        const int debounceMs = 50;

        var cut = RenderNumberField<double>(
            onChanged: v => capturedValue = v,
            valueChangedOnInput: true,
            debounce: debounceMs
        );

        var input = cut.Find("input");
        var text = "123.45";

        await input.InputAsync(text);
        await Task.Delay(debounceMs * 10, Xunit.TestContext.Current.CancellationToken);

        capturedValue.Should().Be(123.45);
        input.GetAttribute("value").Should().Be(text);
    }

    [Theory]
    [InlineData("123", 123d)]
    [InlineData("123abc", 123d)]
    [InlineData("-42", 42d)]     // minus stripped: AllowNegativeNumbers defaults to false
    [InlineData("-42.5", 42.5d)] // minus stripped: AllowNegativeNumbers defaults to false
    [InlineData("1,23", 1.23d)] // accept comma as decimal separator
    public void Double_Input_IsParsedCorrectly(string inputText, double expected)
    {
        double? capturedValue = null;

        var cut = RenderNumberField<double>(
            onChanged: v => capturedValue = v
        );

        var input = cut.Find("input");
        TypeAndCommit(input, inputText);

        capturedValue.Should().Be(expected);
    }

    [Theory]
    [InlineData("en-US", "123,45", '.', 123.45)]
    [InlineData("sv-SE", "123.45", ',', 123.45)]
    public void Culture_UsesCorrectDecimalSeparator(
        string culture,
        string typedText,
        char expectedSeparator,
        double expectedValue)
    {
        double? capturedValue = null;

        var cut = RenderNumberField<double>(
            onChanged: v => capturedValue = v,
            culture: culture
        );

        var input = cut.Find("input");

        TypeAndCommit(input, typedText);

        // numeric value is culture-agnostic
        Assert.Equal(expectedValue, capturedValue);

        var rendered = input.GetAttribute("value") ?? string.Empty;
        rendered.Should().Contain(expectedSeparator.ToString());
    }

    [Fact]
    public void Integer_RejectsDecimalInput()
    {
        var value = 0;
        var cut = Render<PhNumberField<int>>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.ValueChanged, v => value = v));

        var input = cut.Find("input");
        TypeAndCommit(input, "123.45");

        value.Should().Be(123);
        input.GetAttribute("value").Should().Be("123");
    }

    [Fact]
    public void NullableDouble_InitialNull_ShowsEmpty()
    {
        var cut = RenderNumberField<double?>(
            onChanged: _ => { },
            initialValue: null
        );

        var input = cut.Find("input");

        input.GetAttribute("value").Should().Be(string.Empty);
    }

    [Fact]
    public void NullableDouble_Cleared_CommitsNull()
    {
        double? capturedValue = null;

        var cut = RenderNumberField<double?>(
            onChanged: v => capturedValue = v,
            initialValue: 42d
        );

        var input = cut.Find("input");

        input.GetAttribute("value").Should().Contain("42");

        TypeAndCommit(input, string.Empty);

        capturedValue.Should().BeNull();
        input.GetAttribute("value").Should().Be(string.Empty);
    }

    [Fact]
    public void NonNullableDouble_Cleared_CommitsZero()
    {
        double? capturedValue = null;

        var cut = RenderNumberField<double>(
            onChanged: v => capturedValue = v,
            initialValue: 5d
        );

        var input = cut.Find("input");

        TypeAndCommit(input, string.Empty);

        capturedValue.Should().Be(0d);
    }

    [Fact]
    public void MaxDigits_TruncatesIntegerPart()
    {
        double? capturedValue = null;

        var cut = RenderNumberField<double>(
            onChanged: v => capturedValue = v,
            maxDigits: 3
        );

        var input = cut.Find("input");

        TypeAndCommit(input, "12345");

        capturedValue.Should().Be(123d);
    }

    [Fact]
    public void NonNullableDouble_Cleared_StaysEmptyUntilCommit()
    {
        double? capturedValue = null;

        var cut = RenderNumberField<double>(
            onChanged: v => capturedValue = v,
            initialValue: 5d
        );

        var input = cut.Find("input");

        input.GetAttribute("value").Should().Be("5");

        input.Input(string.Empty);

        input.GetAttribute("value").Should().Be(string.Empty);
        capturedValue.Should().BeNull();

        input.Change(string.Empty);

        capturedValue.Should().Be(0d);
        input.GetAttribute("value").Should().Be("0");
    }

    // -----------------------------------------------------------------------
    // AllowNegativeNumbers
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("-1", 1d)]
    [InlineData("-42", 42d)]
    [InlineData("-42.5", 42.5d)]
    public void AllowNegativeNumbers_WhenFalse_StripsMinusSign(string input, double expected)
    {
        double? capturedValue = null;

        var cut = RenderNumberField<double>(onChanged: v => capturedValue = v); // default: false

        TypeAndCommit(cut.Find("input"), input);

        capturedValue.Should().Be(expected);
        cut.Find("input").GetAttribute("value").Should().NotStartWith("-");
    }

    [Theory]
    [InlineData("-1", -1d)]
    [InlineData("-42", -42d)]
    [InlineData("-42.5", -42.5d)]
    public void AllowNegativeNumbers_WhenTrue_AcceptsNegativeNumbers(string input, double expected)
    {
        double? capturedValue = null;

        var cut = RenderNumberField<double>(
            onChanged: v => capturedValue = v,
            allowNegativeNumbers: true
        );

        TypeAndCommit(cut.Find("input"), input);

        capturedValue.Should().Be(expected);
        cut.Find("input").GetAttribute("value").Should().StartWith("-");
    }

    // -----------------------------------------------------------------------
    // MaxDecimals
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("1.23456", 1, 1.2d)]
    [InlineData("1.23456", 2, 1.23d)]
    [InlineData("1.23456", 4, 1.2345d)]
    public void MaxDecimals_TruncatesDecimalPart(string input, int maxDecimals, double expected)
    {
        double? capturedValue = null;

        var cut = RenderNumberField<double>(
            onChanged: v => capturedValue = v,
            maxDecimals: maxDecimals
        );

        TypeAndCommit(cut.Find("input"), input);

        capturedValue.Should().Be(expected);
    }

    [Fact]
    public void MaxDecimals_Zero_TruncatesAllDecimalDigits()
    {
        double? capturedValue = null;

        var cut = RenderNumberField<double>(
            onChanged: v => capturedValue = v,
            maxDecimals: 0
        );

        TypeAndCommit(cut.Find("input"), "1.23456");

        capturedValue.Should().Be(1d);
        cut.Find("input").GetAttribute("value").Should().Be("1");
    }

    [Fact]
    public void MaxDecimals_IsIgnoredForIntegerType()
    {
        var capturedValue = 0;

        var cut = Render<PhNumberField<int>>(parameters => parameters
            .Add(p => p.MaxDecimals, 2)
            .Add(p => p.ValueChanged, (int v) => capturedValue = v));

        TypeAndCommit(cut.Find("input"), "123");

        capturedValue.Should().Be(123);
        cut.Find("input").GetAttribute("value").Should().Be("123");
    }
}

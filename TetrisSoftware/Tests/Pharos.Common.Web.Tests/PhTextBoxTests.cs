using Bunit;
using Pharos.Common.Web.Components.FormElements;

namespace Pharos.Common.Web.Tests;

public class PhTextboxTests : BunitContext
{
    public PhTextboxTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void TrimsWhitespace_OnChange()
    {
        string? value = null;
        var cut = Render<PhTextBox>(parameters => parameters
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value)
            .Add(p => p.ValueChanged, v => value = v));

        var input = cut.Find("input");
        input.Input("  test  ");
        input.Change("  test  ");

        value.Should().Be("test");
    }
}

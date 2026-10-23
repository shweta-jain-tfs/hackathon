namespace Pharos.Common.Web.Utilities;

public class CssBuilder
{
    private string _cssBuilder;

    public CssBuilder(string value)
    {
        _cssBuilder = value;
    }

    private CssBuilder AddValue(string value)
    {
        _cssBuilder += value;
        return this;
    }

    public CssBuilder AddClass(string value)
    {
        return AddValue(" " + value);
    }

    public CssBuilder AddClass(string value, bool when = true)
    {
        return when ? AddClass(value) : this;
    }

    public CssBuilder AddClass(string value, bool? when = true)
    {
        return when == true ? AddClass(value) : this;
    }

    public string Build()
    {
        // String buffer finalization code
        return _cssBuilder.Trim();
    }

    // ToString should only and always call Build to finalize the rendered string.
    public override string ToString()
    {
        return Build();
    }
}
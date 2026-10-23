using FluentAssertions;
using Pharos.Client.Common.App.HelpContent;
using Xunit;

namespace Pharos.Client.Common.App.Tests;

public class HelpContentUrlRewriterTests
{
    [Theory]
    [InlineData("""<a href="#chapter-1">Chapter</a>""")]
    [InlineData("""<a href="{{HelpLink-chapter-1}}">Chapter</a>""")]
    [InlineData("""<a href="/help/#chapter-1">Chapter</a>""")]
    [InlineData("""<a href='{{HelpLink-chapter-1}}'>Chapter</a>""")]
    public void RewriteUrls_WithSupportedHelpLink_ReturnsCanonicalLink(string input)
    {
        HelpContentUrlRewriter.RewriteUrls(input).Should().Be(
            """<a href="/help/#chapter-1">Chapter</a>""");
    }

    [Fact]
    public void RewriteUrls_WithUnsupportedHref_RemovesHrefAttribute()
    {
        var result = HelpContentUrlRewriter.RewriteUrls(
            """<a href="https://example.com">External link</a>""");

        result.Should().Be("<a>External link</a>");
    }

    [Fact]
    public void RewriteUrls_WithImagePath_RewritesImageSource()
    {
        var result = HelpContentUrlRewriter.RewriteUrls(
            """<img src="images/example.png">""");

        result.Should().Be(
            """<img src="/_content/Pharos.Client.App.Help/manuals/images/example.png">""");
    }

    [Fact]
    public void RewriteUrls_RewritesMultipleUrlsWithoutChangingOtherContent()
    {
        var result = HelpContentUrlRewriter.RewriteUrls(
            """<p>Hello world</p><a href="#chapter-1"><img src="images/example.png"></a>""");

        result.Should().Be(
            """<p>Hello world</p><a href="/help/#chapter-1"><img src="/_content/Pharos.Client.App.Help/manuals/images/example.png"></a>""");
    }
}

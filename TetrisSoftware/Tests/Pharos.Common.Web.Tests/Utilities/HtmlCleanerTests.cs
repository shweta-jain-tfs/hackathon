using Microsoft.AspNetCore.Components;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Tests.Utilities;

public class HtmlCleanerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n\t  ")]
    public void CleanHtml_WithEmptyInput_ReturnsEmptyString(string? input)
    {
        // Act
        var result = HtmlCleaner.CleanHtml(input);

        // Assert
        result.Should().Be("");
    }

    [Theory]
    [InlineData("<h1>Title</h1><p>Paragraph</p>")]
    [InlineData("<ul><li>Item</li></ul>")]
    [InlineData("<strong>Bold</strong> and <em>italic</em> text")]
    public void CleanHtml_WithAllowedTags_PreservesContent(string input)
    {
        // Act
        var result = HtmlCleaner.CleanHtml(input);

        // Assert
        result.Should().Be(input);
    }

    [Theory]
    [InlineData("<a href=\"/test\">Link</a>")]
    [InlineData("<img src=\"image.jpg\" alt=\"Image\" class=\"photo\">")]
    [InlineData("<th colspan=\"2\">Header</th>")]
    [InlineData("<div class=\"placeholder\">Content</div>")]
    [InlineData("<img src=\"image.jpg\" style=\"--img-width: 50%\">")]
    public void CleanHtml_WithAllowedAttributes_PreservesAttributes(string input)
    {
        // Act
        var result = HtmlCleaner.CleanHtml(input);

        // Assert
        result.Should().Be(input);
    }

    [Theory]
    [InlineData("<a href=\"/test\" onclick=\"bad()\" target=\"_blank\">Link</a>", "<a href=\"/test\">Link</a>")]
    [InlineData("<img src=\"image.jpg\" alt=\"Image\" onclick=\"hack()\" style=\"--img-width: 50%\">", "<img src=\"image.jpg\" alt=\"Image\" style=\"--img-width: 50%\">")]
    [InlineData("<p class=\"bad\" onclick=\"alert('hack')\">Text</p>", "<p>Text</p>")]
    [InlineData("<th colspan=\"2\" onclick=\"bad()\" style=\"color:red\">Header</th>", "<th colspan=\"2\">Header</th>")]
    [InlineData("<div class=\"placeholder\" onclick=\"bad()\" style=\"color:red\">Content</div>", "<div class=\"placeholder\">Content</div>")]
    public void CleanHtml_WithMixedAttributes_KeepsAllowedRemovesDisallowed(string input, string expected)
    {
        // Act
        var result = HtmlCleaner.CleanHtml(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("<script>alert('hack')</script>Safe", "alert('hack')Safe")]
    [InlineData("<div onclick=\"bad()\">Content</div>", "<div>Content</div>")]
    [InlineData("<span onclick=\"bad()\">Text</span>", "Text")]
    public void CleanHtml_WithDisallowedContent_RemovesDisallowedParts(string input, string expected)
    {
        // Act
        var result = HtmlCleaner.CleanHtml(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("<script></script>")]
    [InlineData("<style></style>")]
    [InlineData("<iframe></iframe>")]
    public void CleanHtml_WithEmptyDisallowedTags_StripsCompletely(string input)
    {
        // Act
        var result = HtmlCleaner.CleanHtml(input);

        // Assert
        result.Should().Be("");
    }

    [Theory]
    [InlineData("Plain text")]
    [InlineData("Text with & special chars")]
    public void CleanHtml_WithPlainText_PreservesText(string input)
    {
        // Act
        var result = HtmlCleaner.CleanHtml(input);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void CleanHtml_WithDebugMode_EncodesRejectedTags()
    {
        // Arrange
        var input = "<script>alert('hack')</script><p>Safe</p>";

        // Act
        var result = HtmlCleaner.CleanHtml(input, preserveRejectedTags: true);

        // Assert
        result.Should().Contain("&lt;script&gt;");
        result.Should().Contain("<p>Safe</p>");
    }

    [Theory]
    [InlineData("<img src=\"photo.jpg\">", "<img src=\"photo.jpg\" loading=\"lazy\">")]
    [InlineData("<img src=\"image.jpg\" alt=\"Photo\">", "<img src=\"image.jpg\" alt=\"Photo\" loading=\"lazy\">")]
    [InlineData("<img src=\"test.jpg\" width=\"100\" height=\"200\">", "<img src=\"test.jpg\" width=\"100\" height=\"200\" loading=\"lazy\">")]
    public void AddLazyLoadingToImages_WithImages_AddsLazyLoading(string input, string expected)
    {
        // Act
        var result = HtmlCleaner.AddLazyLoadingToImages(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("<p>No images here</p>")]
    [InlineData("<div>img</div>")]
    [InlineData("")]
    public void AddLazyLoadingToImages_WithoutImages_ReturnsUnchanged(string input)
    {
        // Act
        var result = HtmlCleaner.AddLazyLoadingToImages(input);

        // Assert
        result.Should().Be(input);
    }

    [Theory]
    [InlineData("<p disallowed=\"remove\">Text with <img src=\"photo.jpg\"> image</p>", "<p>Text with <img src=\"photo.jpg\" loading=\"lazy\"> image</p>")]
    [InlineData("<img src=\"1.jpg\"><img src=\"2.jpg\">", "<img src=\"1.jpg\" loading=\"lazy\"><img src=\"2.jpg\" loading=\"lazy\">")]
    [InlineData("<img src=\"photo.jpg\" onclick=\"hack()\" loading=\"eager\"><p>Safe content</p>", "<img src=\"photo.jpg\" loading=\"lazy\"><p>Safe content</p>")]
    public void ToCleanRichText_WithImages_CleansAndAddsLazyLoading(string input, string expectedValue)
    {
        // Act
        var result = HtmlCleaner.ToCleanRichText(input);

        // Assert
        result.Should().BeOfType<MarkupString>();
        result.Value.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("<p>First paragraph</p><p>Second paragraph</p>", "First paragraph Second paragraph")]
    [InlineData("Hello <b>World</b>", "Hello World")]
    [InlineData("<div>Alpha</div> <div>Beta</div>", "Alpha Beta")]
    [InlineData("<p>Hello&nbsp;World</p>", "Hello World")]
    public void StripHtml_DoesNotConcatenateWords_WhenRemovingTags(string input, string expected)
    {
        var result = HtmlCleaner.StripHtml(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StripHtml_WithNullOrWhitespace_ReturnsEmpty(string? input)
    {
        var result = HtmlCleaner.StripHtml(input);
        result.Should().Be("");
    }

}
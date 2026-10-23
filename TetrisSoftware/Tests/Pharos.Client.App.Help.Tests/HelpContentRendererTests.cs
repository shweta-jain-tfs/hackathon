using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using Pharos.Client.App.Help.Utilities;
using Xunit;

namespace Pharos.Client.App.Help.Tests;

public class HelpContentRendererTests : BunitContext
{
    private const string _marker = HelpContentRenderer.ContentMarker;

    // Fake templates: only used to verify that a substitution happened
    private static readonly Dictionary<HelpPlaceholderType, string> _mockTemplates = new()
    {
        [HelpPlaceholderType.NoteHighlight] = $"<mock-note>{_marker}</mock-note>",
        [HelpPlaceholderType.CautionHighlight] = $"<mock-caution>{_marker}</mock-caution>",
        [HelpPlaceholderType.WarningHighlight] = $"<mock-warning>{_marker}</mock-warning>",
    };

    [Fact]
    public void RenderChapterContent_WithNullInput_ReturnsEmptyFragment()
    {
        // Act
        var html = HelpContentRenderer.RenderChapterContent(null, _mockTemplates).Value;

        // Assert
        html.Should().BeNullOrEmpty();
    }

    [Fact]
    public void RenderChapterContent_WithHighlightPlaceholder_SubstitutesTemplate()
    {
        // Arrange
        var input = "<div class=\"note\">This is important information</div>";

        // Act
        var html = HelpContentRenderer.RenderChapterContent(input, _mockTemplates).Value;

        // Assert
        html.Should().Be("<mock-note>This is important information</mock-note>");
    }

    [Theory]
    [InlineData("<div class=\"note\">Note content</div>", "ph-highlighted-content--info")]
    [InlineData("<div class=\"caution\">Caution content</div>", "ph-highlighted-content--warning")]
    [InlineData("<div class=\"warning\">Warning content</div>", "ph-highlighted-content--error")]
    public async Task RenderChapterContent_WithHighlightPlaceholder_MapsToCorrectType(string input, string expectedCssClass)
    {
        // Arrange: live templates, since we check real component output
        await using var htmlRenderer = new HtmlRenderer(Services, NullLoggerFactory.Instance);
        var templates = await HelpContentRenderer.CreateTemplatesAsync(htmlRenderer);

        // Act
        var html = HelpContentRenderer.RenderChapterContent(input, templates).Value;

        // Assert
        html.Should().Contain("ph-highlighted-content");
        html.Should().Contain(expectedCssClass);
    }

    [Fact]
    public void RenderChapterContent_WithMissingTemplate_KeepsOriginalElement()
    {
        // Arrange
        var input = "<div class=\"caution\">Caution content</div>";

        // Act
        var html = HelpContentRenderer.RenderChapterContent(input, new Dictionary<HelpPlaceholderType, string>()).Value;

        // Assert
        html.Should().Be(input);
    }

    [Fact]
    public void RenderChapterContent_WithPlainContent_RendersAsMarkupString()
    {
        // Arrange
        var input = "<p>Simple paragraph content</p>";

        // Act
        var html = HelpContentRenderer.RenderChapterContent(input, _mockTemplates).Value;

        // Assert
        html.Should().Be("<p>Simple paragraph content</p>");
    }

    // Tests for supported styling classes
    [Theory]
    [InlineData("<figure class=\"image-left\"><img src=\"test.jpg\" alt=\"Test\"></figure>", "image-left")]
    [InlineData("<figure class=\"image-right\"><img src=\"test.jpg\" alt=\"Test\"></figure>", "image-right")]
    [InlineData("<img src=\"test.jpg\" alt=\"Test\" class=\"image-left\">", "image-left")]
    [InlineData("<img src=\"test.jpg\" alt=\"Test\" class=\"image-right\">", "image-right")]
    public void RenderChapterContent_WithSupportedStylingClasses_PreservesClasses(string input, string expectedClass)
    {
        // Act
        var html = HelpContentRenderer.RenderChapterContent(input, _mockTemplates).Value;

        // Assert
        html.Should().Contain(expectedClass);
    }

    [Fact]
    public void RenderChapterContent_WithMixedContent_ProcessesBoth()
    {
        // Arrange
        var input = "<figure class=\"image-left\"><img src=\"test.jpg\" alt=\"Test\"></figure><div class=\"warning\">Important warning</div>";

        // Act
        var html = HelpContentRenderer.RenderChapterContent(input, _mockTemplates).Value;

        // Assert
        // Styling classes preserved
        html.Should().Contain("image-left");
        // Placeholder processed
        html.Should().Contain("<mock-warning>Important warning</mock-warning>");
    }
}
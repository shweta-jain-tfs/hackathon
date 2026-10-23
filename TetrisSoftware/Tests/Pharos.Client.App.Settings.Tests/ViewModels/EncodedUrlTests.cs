using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Settings.ViewModels;

public class EncodedUrlTests
{
    private readonly PageViewModel _viewModel;

    public EncodedUrlTests()
    {
        var mockLocalizer = new Mock<IStringLocalizer<Translations>>();
        _viewModel = new PageViewModel(mockLocalizer.Object);
    }

    private void SetUrl(string url)
    {
        _viewModel.Model = new Page { Url = url };
    }

    [Theory]
    [InlineData("LIS/LIS_@#!@#$%^&&*()", "LIS/LIS_%40%23%21%40%23%24%25%5E%26%26%2A%28%29")]
    [InlineData("LIS/LIS_test&name", "LIS/LIS_test%26name")]
    [InlineData("LIS/LIS_test#name", "LIS/LIS_test%23name")]
    [InlineData("LIS/LIS_test name", "LIS/LIS_test%20name")]
    [InlineData("LIS/LIS_test%name", "LIS/LIS_test%25name")]
    public void EncodedUrl_SpecialCharactersInName_EncodesEachSegment(string rawUrl, string expectedEncodedUrl)
    {
        SetUrl(rawUrl);

        _viewModel.EncodedUrl.Should().Be(expectedEncodedUrl);
    }

    [Theory]
    [InlineData("LIS/SomeEntry")]
    [InlineData("Root/Sub/Entry")]
    public void EncodedUrl_NoSpecialCharacters_RemainsUnchanged(string url)
    {
        SetUrl(url);

        _viewModel.EncodedUrl.Should().Be(url);
    }

    [Fact]
    public void EncodedUrl_SingleSegment_EncodesCorrectly()
    {
        SetUrl("entry@#!");

        _viewModel.EncodedUrl.Should().Be("entry%40%23%21");
    }

    [Fact]
    public void EncodedUrl_PreservesSlashDelimiters()
    {
        SetUrl("A/B/C");

        _viewModel.EncodedUrl.Should().Be("A/B/C");
        _viewModel.EncodedUrl.Count(c => c == '/').Should().Be(_viewModel.Url.Count(c => c == '/'));
    }

    [Fact]
    public void EncodedUrl_SpecialCharsOnlyInLastSegment_OnlyLastSegmentEncoded()
    {
        SetUrl("Root/Sub/Entry&#!");

        var segments = _viewModel.EncodedUrl.Split('/');

        segments[0].Should().Be("Root");
        segments[1].Should().Be("Sub");
        segments[2].Should().Be("Entry%26%23%21");
    }
}

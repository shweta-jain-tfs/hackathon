using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using Pharos.Client.App.Parameters.LoadListConfiguration.ViewModels;
using Pharos.Client.Common.Localization;
using Xunit;

namespace Pharos.Client.App.Parameters.Tests.ViewModels;

public class LoadListConfigurationValidatorTests
{
    private readonly LoadListConfigurationValidator _sut;

    public LoadListConfigurationValidatorTests()
    {
        var localizer = new Mock<IStringLocalizer<Translations>>();
        localizer.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        _sut = new LoadListConfigurationValidator(localizer.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(1001)]
    [InlineData(99999)]
    [InlineData(100000)]
    public void Validate_ShouldAcceptEachLevelIndependently_WhenWithinRange(int? level)
    {
        _sut.Validate(new ConfigurationItem { TriggerLevel = level }).IsValid.Should().BeTrue();
        _sut.Validate(new ConfigurationItem { RefillLevel = level }).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100001)]
    [InlineData(int.MaxValue)]
    public void Validate_ShouldRejectEachLevel_WithCorrectMessage_WhenOutsideRange(int level)
    {
        var triggerResult = _sut.Validate(new ConfigurationItem { TriggerLevel = level });
        triggerResult.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("LoadListConfiguration_TriggerLevel_ValidationText");

        var refillResult = _sut.Validate(new ConfigurationItem { RefillLevel = level });
        refillResult.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("LoadListConfiguration_RefillLevel_ValidationText");
    }

    [Theory]
    [InlineData(99999, 100000, true)]
    [InlineData(100000, 100000, false)]
    [InlineData(100000, 99999, false)]
    public void Validate_ShouldPreserveOrderingAtNewMaximum(int triggerLevel, int refillLevel, bool expectedValid)
    {
        var result = _sut.Validate(new ConfigurationItem
        {
            TriggerLevel = triggerLevel,
            RefillLevel = refillLevel
        });

        result.IsValid.Should().Be(expectedValid);
    }
}

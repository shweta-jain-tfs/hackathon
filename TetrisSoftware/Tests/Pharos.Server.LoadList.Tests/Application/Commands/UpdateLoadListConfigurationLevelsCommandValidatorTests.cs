using Pharos.Server.LoadList.Application.Commands.UpdateConfiguration;

namespace Pharos.Server.LoadList.Tests.Application.Commands;

public class UpdateLoadListConfigurationLevelsCommandValidatorTests
{
    private readonly UpdateLoadListConfigurationLevelsCommandValidator _sut = new();

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(1001)]
    [InlineData(99999)]
    [InlineData(100000)]
    public void Validate_ShouldAcceptEachLevelIndependently_WhenWithinRange(int? level)
    {
        _sut.Validate(new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [1],
            TriggerLevel = level
        }).IsValid.Should().BeTrue();

        _sut.Validate(new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [1],
            RefillLevel = level
        }).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100001)]
    [InlineData(int.MaxValue)]
    public void Validate_ShouldRejectEachLevelIndependently_WhenOutsideRange(int level)
    {
        var triggerResult = _sut.Validate(new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [1],
            TriggerLevel = level
        });
        triggerResult.Errors.Should().Contain(error => error.PropertyName == "TriggerLevel");

        var refillResult = _sut.Validate(new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [1],
            RefillLevel = level
        });
        refillResult.Errors.Should().Contain(error => error.PropertyName == "RefillLevel");
    }

    [Theory]
    [InlineData(99999, 100000, true)]
    [InlineData(100000, 100000, false)]
    [InlineData(100000, 99999, false)]
    public void Validate_ShouldPreserveOrderingAtNewMaximum(int triggerLevel, int refillLevel, bool expectedValid)
    {
        var result = _sut.Validate(new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [1],
            TriggerLevel = triggerLevel,
            RefillLevel = refillLevel
        });

        result.IsValid.Should().Be(expectedValid);
    }
}
